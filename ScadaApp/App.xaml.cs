using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ScadaApp.Infrastructure;
using ScadaApp.Infrastructure.Logging;
using ScadaApp.Services;
using ScadaApp.Views;
using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace ScadaApp;

/// <summary>
/// Главный класс WPF-приложения. Это точка входа — код, который выполняется первым
/// при запуске программы.
///
/// Наследуется от класса Application (базовый класс всех WPF-приложений).
/// Ключевое слово "partial" означает, что класс разделён на два файла:
///   App.xaml     — ресурсы (конвертеры значений для привязки данных в XAML)
///   App.xaml.cs  — логика запуска (этот файл)
///
/// Использует паттерн "Generic Host" от Microsoft.Extensions.Hosting:
/// это стандартный способ организации .NET-приложений с DI-контейнером,
/// логированием и hosted services (фоновыми сервисами).
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// Хост приложения — контейнер для всех сервисов, логирования и конфигурации.
    /// IHost — интерфейс из Microsoft.Extensions.Hosting.
    /// Знак "?" означает, что переменная может быть null (nullable reference type).
    /// </summary>
    private IHost? _host;

    /// <summary>
    /// Специальный объект, который перехватывает сообщения логирования (Serilog)
    /// и добавляет их в коллекцию для отображения в таблице журнала главного окна.
    /// </summary>
    private ObservableLogSink? _logSink;

    /// <summary>
    /// Вызывается автоматически при запуске приложения (до появления окна).
    /// Здесь выполняется вся инициализация: DI, загрузка конфига, создание окна.
    ///
    /// Метод помечен как async void — это допустимо только для обработчиков событий.
    /// async позволяет использовать await внутри метода для асинхронных операций
    /// (операций, которые выполняются в фоне, не блокируя UI).
    /// </summary>
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Создаём sink для логов — он будет отображать логи в UI.
        // Dispatcher.CurrentDispatcher — это планировщик задач UI-потока.
        // Все изменения UI в WPF должны происходить только в UI-потоке.
        _logSink = new ObservableLogSink(Dispatcher.CurrentDispatcher);

        // Создаём Generic Host — стандартный контейнер .NET-приложения.
        // Host.CreateDefaultBuilder() создаёт хост с базовыми настройками.
        // ConfigureServices — регистрируем все наши сервисы в DI-контейнере.
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                // AddScadaServices — наш метод-расширение (Extension Method) из DependencyInjection.cs
                // Регистрирует все сервисы, ViewModels и Views приложения.
                services.AddScadaServices(_logSink);
            })
            .Build();

        // Запускаем хост — это запускает все IHostedService (DatabaseService и т.д.)
        await _host.StartAsync();

        // Получаем сервис конфигурации из DI-контейнера.
        // GetRequiredService<T>() — получить сервис типа T. Выбросит исключение,
        // если сервис не зарегистрирован.
        var configService = _host.Services.GetRequiredService<IConfigurationService>();

        // Загружаем конфигурацию из файла config.json асинхронно.
        var config = await configService.LoadAsync();

        // Получаем singleton-экземпляр AppConfig (уже зарегистрированный в DI).
        // Singleton — один экземпляр на всё приложение. Все части кода получают
        // один и тот же объект.
        var appConfig = _host.Services.GetRequiredService<Models.AppConfig>();

        // Переносим данные из загруженного конфига в singleton AppConfig.
        // Мы не можем просто присвоить appConfig = config, потому что DI уже
        // раздал ссылку на старый appConfig другим сервисам. Поэтому обновляем
        // содержимое существующего объекта.
        appConfig.Devices.Clear();
        appConfig.Devices.AddRange(config.Devices);
        appConfig.Database.Enabled = config.Database.Enabled;
        appConfig.Database.DatabaseType = config.Database.DatabaseType;
        appConfig.Database.ConnectionString = config.Database.ConnectionString;
        appConfig.Database.SaveIntervalMs = config.Database.SaveIntervalMs;
        appConfig.Database.BufferSize = config.Database.BufferSize;
        appConfig.Visualization.Enabled = config.Visualization.Enabled;
        appConfig.Visualization.ServiceUrl = config.Visualization.ServiceUrl;
        appConfig.Visualization.Mappings.Clear();
        appConfig.Visualization.Mappings.AddRange(config.Visualization.Mappings);

        // Инициализируем сервис 3D-визуализации с загруженными настройками.
        var vis3d = _host.Services.GetRequiredService<I3DVisualizationService>();
        await vis3d.InitializeAsync(appConfig.Visualization);

        // Получаем главное окно и его ViewModel из DI и открываем окно.
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        var mainVm = _host.Services.GetRequiredService<ViewModels.MainViewModel>();

        // Загружаем конфигурацию в ViewModel (обновляет таблицу тегов).
        await mainVm.LoadConfigAsync();

        // Показываем главное окно приложения.
        mainWindow.Show();

        // Подписываемся на обработчик необработанных исключений.
        // Если в UI-потоке возникнет неперехваченное исключение — оно попадёт сюда.
        DispatcherUnhandledException += OnUnhandledException;
    }

    /// <summary>
    /// Вызывается при закрытии приложения (пользователь закрыл все окна
    /// или вызван Application.Shutdown()).
    ///
    /// Метод async void — допустимо для обработчиков событий.
    /// Здесь мы корректно останавливаем все сервисы хоста.
    /// </summary>
    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            // Даём сервисам 5 секунд на корректное завершение работы.
            // Например, DatabaseService успеет сохранить буфер в БД.
            await _host.StopAsync(TimeSpan.FromSeconds(5));

            // Освобождаем ресурсы хоста (закрываем соединения, файлы и т.д.).
            _host.Dispose();
        }
        base.OnExit(e);
    }

    /// <summary>
    /// Обработчик необработанных исключений в UI-потоке.
    /// Если в коде ViewModel или обработчике события произошла ошибка,
    /// которую никто не поймал — она попадёт сюда.
    ///
    /// Параметр e.Handled = true предотвращает аварийное завершение приложения.
    /// </summary>
    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        // Показываем диалоговое окно с сообщением об ошибке.
        MessageBox.Show($"Необработанная ошибка:\n{e.Exception.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);

        // Отмечаем исключение как обработанное — приложение продолжит работу.
        e.Handled = true;
    }
}
