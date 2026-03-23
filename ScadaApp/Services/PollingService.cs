using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScadaApp.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaApp.Services;

/// <summary>
/// Аргументы события изменения значения тега.
/// Передаются вместе с событием TagValueChanged в PollingService.
/// Содержат всю необходимую информацию о прочитанном значении.
///
/// EventArgs — базовый класс для аргументов событий в C#.
/// Ключевое слово "init" у свойств — можно задать только в инициализаторе объекта.
/// </summary>
public class TagValueChangedEventArgs : EventArgs
{
    /// <summary>Идентификатор устройства (GUID), от которого получено значение.</summary>
    public string DeviceId { get; init; } = "";

    /// <summary>Имя устройства для отображения.</summary>
    public string DeviceName { get; init; } = "";

    /// <summary>Имя тега, значение которого было прочитано.</summary>
    public string TagName { get; init; } = "";

    /// <summary>
    /// Прочитанное значение. object? — может быть любого типа или null.
    /// В зависимости от типа тега это может быть bool, int, float и т.д.
    /// </summary>
    public object? Value { get; init; }

    /// <summary>
    /// Качество чтения: "Good" (успешно), "Bad" (ошибка), "Uncertain" (сомнительно).
    /// </summary>
    public string Quality { get; init; } = "Good";

    /// <summary>
    /// Временная метка чтения в UTC (Coordinated Universal Time — всемирное координированное время).
    /// В UI переводится в локальное время: e.Timestamp.ToLocalTime().
    /// </summary>
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>
/// Сервис периодического опроса тегов промышленных контроллеров.
///
/// Реализует два интерфейса:
///   IHostedService — позволяет запускать и останавливать сервис через Generic Host.
///   IDisposable    — освобождение неуправляемых ресурсов (CancellationTokenSource).
///
/// Принцип работы:
///   1. При StartAsync() — создаёт драйверы, подключается к устройствам,
///      для каждого устройства запускает отдельную задачу (Task) опроса.
///   2. Каждая задача опроса в цикле читает все теги устройства и поднимает
///      событие TagValueChanged с новым значением.
///   3. При StopAsync() — отменяет все задачи и отключается от устройств.
/// </summary>
public class PollingService : IHostedService, IDisposable
{
    private readonly DeviceManager _deviceManager;
    private readonly ILogger<PollingService> _logger;

    /// <summary>
    /// Список конфигураций устройств для опроса.
    /// Задаётся методом Configure() перед запуском.
    /// </summary>
    private readonly List<DeviceConfig> _deviceConfigs = new();

    /// <summary>
    /// CancellationTokenSource — источник токена отмены.
    /// При вызове _cts.Cancel() все задачи, использующие _cts.Token,
    /// получают сигнал об отмене и должны завершиться.
    /// </summary>
    private CancellationTokenSource? _cts;

    /// <summary>
    /// Список запущенных задач опроса (по одной на каждое устройство).
    /// Используется для ожидания их завершения при StopAsync().
    /// </summary>
    private readonly List<Task> _pollingTasks = new();

    /// <summary>
    /// Событие, которое возникает при каждом новом прочитанном значении тега.
    /// EventHandler&lt;T&gt; — стандартный делегат события в C#.
    /// Знак "?" — на событие может никто не подписываться (null).
    ///
    /// Подписчики (MainViewModel, DatabaseService) получают уведомление
    /// и обновляют UI / записывают в БД.
    /// </summary>
    public event EventHandler<TagValueChangedEventArgs>? TagValueChanged;

    /// <summary>
    /// Флаг активности опроса. Используется в UI для отображения состояния.
    /// </summary>
    public bool IsPolling { get; private set; }

    public PollingService(DeviceManager deviceManager, ILogger<PollingService> logger)
    {
        _deviceManager = deviceManager;
        _logger = logger;
    }

    /// <summary>
    /// Устанавливает список устройств для опроса.
    /// Вызывается из MainViewModel перед StartAsync().
    /// IEnumerable&lt;T&gt; — принимает любую коллекцию DeviceConfig.
    /// </summary>
    public void Configure(IEnumerable<DeviceConfig> devices)
    {
        _deviceConfigs.Clear();
        _deviceConfigs.AddRange(devices);
    }

    /// <summary>
    /// Запускает сервис опроса (IHostedService.StartAsync).
    /// CancellationToken cancellationToken — токен отмены от хоста (при остановке приложения).
    /// </summary>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запуск сервиса опроса тегов");
        IsPolling = true;

        // Создаём новый источник токена отмены, связанный с внешним токеном.
        // Linked — если внешний токен отменён, наш тоже отменяется.
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        // Инициализируем драйверы для всех устройств.
        await _deviceManager.InitializeAsync(_deviceConfigs);

        // Подключаемся к каждому устройству и запускаем задачу опроса.
        foreach (var device in _deviceConfigs)
        {
            var connected = await _deviceManager.ConnectDeviceAsync(device);
            if (!connected)
            {
                _logger.LogWarning("Не удалось подключиться к устройству {Name}", device.Name);
            }

            // Task.Run здесь не используется — PollDeviceAsync сама является async,
            // поэтому запускается параллельно без блокировки.
            var task = PollDeviceAsync(device, _cts.Token);
            _pollingTasks.Add(task);
        }
    }

    /// <summary>
    /// Останавливает сервис опроса (IHostedService.StopAsync).
    /// Отменяет все задачи и дожидается их завершения.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Остановка сервиса опроса тегов");
        IsPolling = false;

        // Сигнализируем всем задачам об отмене.
        _cts?.Cancel();

        try
        {
            // Ждём завершения всех задач опроса.
            // Task.WhenAll — ждёт, пока все переданные задачи завершатся.
            await Task.WhenAll(_pollingTasks);
        }
        catch (OperationCanceledException)
        {
            // OperationCanceledException — нормальное поведение при отмене задачи.
            // Игнорируем это исключение.
        }

        _pollingTasks.Clear();

        // Отключаемся от всех устройств.
        await _deviceManager.DisconnectAllAsync();
    }

    /// <summary>
    /// Цикл опроса одного устройства. Выполняется в фоне всё время, пока опрос активен.
    ///
    /// private async Task — асинхронный метод, который выполняется в отдельной "нити".
    /// while (!ct.IsCancellationRequested) — цикл продолжается, пока не запрошена отмена.
    /// </summary>
    private async Task PollDeviceAsync(DeviceConfig device, CancellationToken ct)
    {
        _logger.LogInformation("Начало опроса устройства {Name}", device.Name);

        while (!ct.IsCancellationRequested)
        {
            // Получаем драйвер для этого устройства.
            var driver = _deviceManager.GetDriver(device.Id);
            if (driver == null) break; // Драйвер не найден — выходим из цикла

            // Если устройство не подключено — пробуем переподключиться.
            if (!driver.IsConnected)
            {
                _logger.LogDebug("Устройство {Name} недоступно, попытка переподключения...", device.Name);
                await _deviceManager.ConnectDeviceAsync(device);

                // Ждём интервал и повторяем попытку со следующей итерации.
                await Task.Delay(device.PollingIntervalMs, ct).ConfigureAwait(false);
                continue;
            }

            // Перебираем все теги устройства и читаем каждый.
            foreach (var tag in device.Tags)
            {
                // Пропускаем выключенные теги и проверяем отмену.
                if (!tag.Enabled || ct.IsCancellationRequested) continue;

                try
                {
                    // Читаем значение тега через драйвер.
                    var result = await driver.ReadTagAsync(tag, ct);

                    // Создаём аргументы события с данными о прочитанном значении.
                    var args = new TagValueChangedEventArgs
                    {
                        DeviceId = device.Id,
                        DeviceName = device.Name,
                        TagName = tag.Name,
                        Value = result.Value,
                        Quality = result.Quality,
                        Timestamp = DateTime.UtcNow
                    };

                    // Вызываем событие — все подписчики (MainViewModel, DatabaseService)
                    // получат уведомление.
                    // "?" — проверка на null (никто не подписан → просто ничего не делаем).
                    TagValueChanged?.Invoke(this, args);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    // "when" — фильтр исключения: перехватываем только если отмена не запрошена.
                    _logger.LogError(ex, "Ошибка опроса тега {Tag} устройства {Device}", tag.Name, device.Name);
                }
            }

            try
            {
                // Пауза между итерациями опроса.
                // ConfigureAwait(false) — продолжаем работу не обязательно в UI-потоке.
                await Task.Delay(device.PollingIntervalMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                // Отмена во время паузы — нормальная ситуация, выходим из цикла.
                break;
            }
        }
    }

    /// <summary>
    /// Освобождает ресурсы (IDisposable).
    /// Вызывается автоматически при using или при завершении хоста.
    /// Отменяем и освобождаем CancellationTokenSource.
    /// </summary>
    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
