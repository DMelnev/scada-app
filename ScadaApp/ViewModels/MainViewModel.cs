using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScadaApp.Infrastructure.Logging;
using ScadaApp.Models;
using ScadaApp.Services;
using ScadaApp.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ScadaApp.ViewModels;

/// <summary>
/// Модель отображения одного тега в таблице главного окна.
/// Содержит данные о теге и его текущем значении для отображения в DataGrid.
///
/// Наследуется от ObservableObject (из CommunityToolkit.Mvvm).
/// ObservableObject реализует интерфейс INotifyPropertyChanged — это стандартный
/// механизм WPF, который автоматически обновляет UI при изменении свойств.
///
/// Атрибут [ObservableProperty] над private-полем автоматически генерирует
/// публичное свойство с нужным именем и уведомлением об изменении.
/// Например, _deviceName → публичное свойство DeviceName.
/// </summary>
public partial class TagDisplayItem : ObservableObject
{
    /// <summary>Название устройства (ПЛК), к которому принадлежит тег.</summary>
    [ObservableProperty] private string _deviceName = "";

    /// <summary>Имя тега (как задано пользователем в настройках).</summary>
    [ObservableProperty] private string _tagName = "";

    /// <summary>
    /// Текущее значение тега в виде строки.
    /// "—" означает, что значение ещё не было прочитано.
    /// </summary>
    [ObservableProperty] private string _value = "—";

    /// <summary>
    /// Качество последнего прочитанного значения.
    /// "Good" — чтение успешно, "Bad" — ошибка, "Uncertain" — неопределённо.
    /// "—" означает, что тег ещё не был прочитан.
    /// </summary>
    [ObservableProperty] private string _quality = "—";

    /// <summary>Время последнего успешного обновления значения тега.</summary>
    [ObservableProperty] private DateTime _lastUpdate = DateTime.MinValue;

    /// <summary>
    /// Флаг наличия связи с устройством.
    /// true  — последнее чтение тега было успешным (отображается галочкой в UI)
    /// false — устройство недоступно или опрос не запущен
    /// </summary>
    [ObservableProperty] private bool _isConnected;

    /// <summary>
    /// Идентификатор устройства (GUID). Используется для поиска нужной строки
    /// в таблице при получении нового значения от сервиса опроса.
    /// Не привязывается к UI напрямую.
    /// </summary>
    public string DeviceId { get; set; } = "";

    /// <summary>Идентификатор тега (GUID). Не привязывается к UI напрямую.</summary>
    public string TagId { get; set; } = "";
}

/// <summary>
/// ViewModel главного окна приложения.
/// Реализует логику кнопок, управляет таблицей тегов и журналом событий.
///
/// В паттерне MVVM:
///   Model      — данные (AppConfig, TagDisplayItem)
///   View       — MainWindow.xaml (разметка UI)
///   ViewModel  — этот класс (логика и состояние UI)
///
/// ViewModel не знает ничего о конкретном окне — это позволяет тестировать
/// логику без запуска UI.
///
/// Атрибут [partial] нужен, так как CommunityToolkit.Mvvm генерирует
/// дополнительный код в отдельном partial-классе.
/// </summary>
public partial class MainViewModel : ObservableObject
{
    // Зависимости, получаемые через DI (Dependency Injection).
    // readonly — значит, они устанавливаются один раз в конструкторе и не меняются.
    private readonly IServiceProvider _serviceProvider;    // DI-контейнер для создания окон
    private readonly IConfigurationService _configService; // Сервис загрузки/сохранения конфига
    private readonly PollingService _pollingService;       // Сервис опроса ПЛК
    private readonly ObservableLogSink _logSink;           // Источник записей журнала для UI
    private readonly ILogger<MainViewModel> _logger;       // Логгер для записи событий

    /// <summary>
    /// Корневая конфигурация приложения.
    /// [ObservableProperty] генерирует свойство Config с уведомлением об изменении.
    /// Привязывается к элементам управления в XAML через {Binding Config}.
    /// </summary>
    [ObservableProperty] private AppConfig _config = new();

    /// <summary>
    /// Флаг активности опроса устройств.
    /// true  — опрос запущен, кнопка "Стоп" активна, "Старт" заблокирована.
    /// false — опрос остановлен, кнопка "Старт" активна, "Стоп" заблокирована.
    /// </summary>
    [ObservableProperty] private bool _isPolling;

    /// <summary>
    /// Текст в строке состояния главного окна.
    /// Отображает текущее состояние приложения: "Готов", "Опрос запущен" и т.д.
    /// </summary>
    [ObservableProperty] private string _statusText = "Готов";

    /// <summary>
    /// Коллекция строк таблицы тегов. Каждый элемент — одна строка в DataGrid.
    /// ObservableCollection автоматически уведомляет UI при добавлении/удалении элементов.
    /// </summary>
    public ObservableCollection<TagDisplayItem> Tags { get; } = new();

    /// <summary>
    /// Коллекция записей журнала событий.
    /// Берётся напрямую из ObservableLogSink — тот же объект, в который
    /// Serilog добавляет новые записи.
    /// </summary>
    public ObservableCollection<LogEntry> LogEntries { get; }

    /// <summary>
    /// Конструктор получает все зависимости через DI-контейнер (внедрение зависимостей).
    /// DI автоматически создаёт и передаёт нужные объекты при создании ViewModel.
    /// </summary>
    public MainViewModel(
        IServiceProvider serviceProvider,
        IConfigurationService configService,
        PollingService pollingService,
        ObservableLogSink logSink,
        ILogger<MainViewModel> logger)
    {
        _serviceProvider = serviceProvider;
        _configService = configService;
        _pollingService = pollingService;
        _logSink = logSink;
        _logger = logger;

        // Получаем коллекцию логов напрямую из sink.
        LogEntries = logSink.LogEntries;

        // Подписываемся на событие изменения тега.
        // "+=" добавляет обработчик OnTagValueChanged к событию TagValueChanged.
        // Теперь при каждом новом прочитанном значении будет вызываться наш метод.
        _pollingService.TagValueChanged += OnTagValueChanged;
    }

    /// <summary>
    /// Загружает конфигурацию из файла и обновляет таблицу тегов.
    /// Вызывается при старте приложения из App.xaml.cs.
    /// Task — тип возвращаемого значения async-метода (аналог "обещание результата").
    /// </summary>
    public async Task LoadConfigAsync()
    {
        Config = await _configService.LoadAsync();
        RefreshTagList();
        _logger.LogInformation("Конфигурация загружена ({DeviceCount} устройств)", Config.Devices.Count);
    }

    /// <summary>
    /// Перестраивает таблицу тегов на основе текущей конфигурации.
    /// Вызывается после загрузки конфига и после сохранения настроек устройств/тегов.
    /// </summary>
    private void RefreshTagList()
    {
        // Очищаем все строки таблицы.
        Tags.Clear();

        // Перебираем все устройства и все их теги, создавая строки таблицы.
        foreach (var device in Config.Devices)
        {
            foreach (var tag in device.Tags)
            {
                Tags.Add(new TagDisplayItem
                {
                    DeviceId = device.Id,
                    DeviceName = device.Name,
                    TagId = tag.Id,
                    TagName = tag.Name,
                    // Value и Quality остаются "—" до первого опроса
                });
            }
        }

        // Обновляем строку состояния с количеством устройств и тегов.
        StatusText = $"Устройств: {Config.Devices.Count}, Тегов: {Tags.Count}";
    }

    /// <summary>
    /// Обработчик события TagValueChanged от PollingService.
    /// Вызывается из фонового потока при каждом новом прочитанном значении тега.
    ///
    /// ВАЖНО: обновление UI всегда должно происходить в UI-потоке!
    /// Поэтому используем Application.Current.Dispatcher.Invoke().
    /// Dispatcher.Invoke() "перекидывает" выполнение кода в UI-поток.
    /// </summary>
    private void OnTagValueChanged(object? sender, TagValueChangedEventArgs e)
    {
        // Ищем строку в таблице, соответствующую этому тегу этого устройства.
        // FirstOrDefault возвращает первый подходящий элемент или null.
        var item = Tags.FirstOrDefault(t => t.DeviceId == e.DeviceId && t.TagName == e.TagName);

        if (item != null)
        {
            // Обновляем UI строго в UI-потоке.
            Application.Current.Dispatcher.Invoke(() =>
            {
                // Преобразуем значение в строку для отображения.
                item.Value = e.Value?.ToString() ?? "null";
                item.Quality = e.Quality;
                // ToLocalTime() конвертирует UTC-время в локальное время компьютера.
                item.LastUpdate = e.Timestamp.ToLocalTime();
                item.IsConnected = true; // Раз значение прочитано — связь есть
            });
        }
    }

    /// <summary>
    /// Команда запуска опроса устройств.
    /// [RelayCommand] генерирует свойство StartPollingCommand типа IRelayCommand.
    /// Кнопка в XAML привязывается: Command="{Binding StartPollingCommand}"
    ///
    /// CanExecute = nameof(CanStartPolling) означает: кнопка активна только
    /// когда метод CanStartPolling() возвращает true (то есть когда !IsPolling).
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStartPolling))]
    private async Task StartPolling()
    {
        // Передаём список устройств в сервис опроса.
        _pollingService.Configure(Config.Devices);

        // Запускаем опрос. default = CancellationToken.None (без возможности отмены извне).
        await _pollingService.StartAsync(default);

        IsPolling = true;
        StatusText = "Опрос запущен";
        _logger.LogInformation("Опрос запущен");
    }

    /// <summary>
    /// Определяет, когда кнопка "Старт" активна.
    /// Команда доступна только когда опрос НЕ запущен.
    /// </summary>
    private bool CanStartPolling() => !IsPolling;

    /// <summary>
    /// Команда остановки опроса устройств.
    /// Аналогично StartPollingCommand, но для остановки.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanStopPolling))]
    private async Task StopPolling()
    {
        await _pollingService.StopAsync(default);
        IsPolling = false;
        StatusText = "Опрос остановлен";
        _logger.LogInformation("Опрос остановлен");
    }

    /// <summary>
    /// Кнопка "Стоп" активна только когда опрос запущен.
    /// </summary>
    private bool CanStopPolling() => IsPolling;

    /// <summary>
    /// Открывает окно настройки устройств.
    /// _serviceProvider.GetRequiredService&lt;T&gt;() создаёт новый экземпляр окна через DI.
    /// ShowDialog() открывает окно как модальное (блокирует главное окно до закрытия).
    /// Возвращает true, если пользователь нажал "Сохранить".
    /// </summary>
    [RelayCommand]
    private void OpenDeviceSettings()
    {
        var window = _serviceProvider.GetRequiredService<DeviceSettingsWindow>();
        window.Owner = Application.Current.MainWindow; // Привязываем к главному окну
        if (window.ShowDialog() == true)
        {
            // Пользователь нажал "Сохранить" — сохраняем конфиг и обновляем таблицу.
            // "_" — дискардирование Task (fire-and-forget, мы не ждём завершения).
            _ = _configService.SaveAsync(Config);
            RefreshTagList();
        }
    }

    /// <summary>Открывает окно настройки тегов.</summary>
    [RelayCommand]
    private void OpenTagSettings()
    {
        var window = _serviceProvider.GetRequiredService<TagSettingsWindow>();
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            _ = _configService.SaveAsync(Config);
            RefreshTagList();
        }
    }

    /// <summary>Открывает окно настройки базы данных.</summary>
    [RelayCommand]
    private void OpenDatabaseSettings()
    {
        var window = _serviceProvider.GetRequiredService<DatabaseSettingsWindow>();
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            _ = _configService.SaveAsync(Config);
        }
    }

    /// <summary>Открывает окно настройки 3D-визуализации.</summary>
    [RelayCommand]
    private void OpenVisualizationSettings()
    {
        var window = _serviceProvider.GetRequiredService<VisualizationSettingsWindow>();
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            _ = _configService.SaveAsync(Config);
        }
    }
}
