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

/// <summary>Точка входа WPF-приложения с DI и Microsoft.Extensions.Hosting.</summary>
public partial class App : Application
{
    private IHost? _host;
    private ObservableLogSink? _logSink;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _logSink = new ObservableLogSink(Dispatcher.CurrentDispatcher);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddScadaServices(_logSink);
            })
            .Build();

        await _host.StartAsync();

        // Загрузить конфигурацию
        var configService = _host.Services.GetRequiredService<IConfigurationService>();
        var config = await configService.LoadAsync();

        // Переписать singleton AppConfig содержимым из файла
        var appConfig = _host.Services.GetRequiredService<Models.AppConfig>();
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

        // Инициализировать 3D-сервис
        var vis3d = _host.Services.GetRequiredService<I3DVisualizationService>();
        await vis3d.InitializeAsync(appConfig.Visualization);

        // Открыть главное окно
        var mainWindow = _host.Services.GetRequiredService<MainWindow>();
        var mainVm = _host.Services.GetRequiredService<ViewModels.MainViewModel>();
        await mainVm.LoadConfigAsync();
        mainWindow.Show();

        DispatcherUnhandledException += OnUnhandledException;
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host != null)
        {
            // Остановить все hosted services
            await _host.StopAsync(TimeSpan.FromSeconds(5));
            _host.Dispose();
        }
        base.OnExit(e);
    }

    private void OnUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        MessageBox.Show($"Необработанная ошибка:\n{e.Exception.Message}",
            "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
        e.Handled = true;
    }
}
