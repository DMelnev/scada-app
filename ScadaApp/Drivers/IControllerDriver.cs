using ScadaApp.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaApp.Drivers;

/// <summary>
/// Результат чтения одного тега с контроллера.
/// Возвращается методом ReadTagAsync() и передаётся в событие TagValueChanged.
/// </summary>
public class TagReadResult
{
    /// <summary>
    /// Успешно ли выполнено чтение.
    /// true  — значение прочитано без ошибок
    /// false — произошла ошибка (см. ErrorMessage)
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Прочитанное значение. object? — может быть любого типа или null.
    /// В зависимости от типа тега это может быть bool, int, float, string и т.д.
    /// Вызывающий код обычно делает .ToString() для отображения.
    /// </summary>
    public object? Value { get; set; }

    /// <summary>
    /// Качество чтения:
    /// "Good"      — чтение прошло успешно, значение достоверно
    /// "Bad"       — ошибка чтения (нет связи, неверный адрес и т.д.)
    /// "Uncertain" — значение получено, но его достоверность сомнительна
    /// </summary>
    public string Quality { get; set; } = "Good";

    /// <summary>
    /// Текст ошибки при неудачном чтении. Null если Success == true.
    /// Записывается в лог при Quality == "Bad".
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Абстракция (интерфейс) драйвера промышленного контроллера.
/// Интерфейс определяет контракт — набор методов, которые должен реализовать
/// каждый конкретный драйвер (Siemens, OPC UA и т.д.).
///
/// Наследуется от IDisposable — все драйверы должны реализовать Dispose()
/// для корректного освобождения ресурсов (сетевых соединений).
///
/// Зачем нужен интерфейс?
///   DeviceManager работает с IControllerDriver, не зная конкретного типа драйвера.
///   Это позволяет легко добавлять поддержку новых протоколов без изменения DeviceManager.
/// </summary>
public interface IControllerDriver : IDisposable
{
    /// <summary>
    /// Уникальный идентификатор устройства (GUID).
    /// Используется DeviceManager для хранения драйверов в словаре.
    /// </summary>
    string DeviceId { get; }

    /// <summary>
    /// Признак активного подключения к устройству.
    /// true  — подключение установлено, можно читать теги
    /// false — нет подключения (устройство недоступно или не подключались)
    /// </summary>
    bool IsConnected { get; }

    /// <summary>
    /// Асинхронно подключается к устройству.
    /// Принимает конфигурацию (IP, Rack, Slot и т.д.) и токен отмены.
    /// Возвращает true при успехе, false при ошибке.
    ///
    /// CancellationToken ct = default — необязательный параметр.
    /// default = CancellationToken.None (без возможности отмены).
    /// </summary>
    Task<bool> ConnectAsync(DeviceConfig config, CancellationToken ct = default);

    /// <summary>
    /// Асинхронно отключается от устройства.
    /// Закрывает TCP-соединение и освобождает связанные ресурсы.
    /// </summary>
    Task DisconnectAsync();

    /// <summary>
    /// Асинхронно читает значение одного тега.
    /// Возвращает TagReadResult с значением и качеством чтения.
    /// При ошибке возвращает Success=false, Quality="Bad", ErrorMessage=...
    /// </summary>
    Task<TagReadResult> ReadTagAsync(TagConfig tag, CancellationToken ct = default);

    /// <summary>
    /// Асинхронно записывает значение в тег контроллера.
    /// object value — значение любого типа (приводится к нужному типу внутри драйвера).
    /// Возвращает true при успешной записи.
    /// </summary>
    Task<bool> WriteTagAsync(TagConfig tag, object value, CancellationToken ct = default);
}
