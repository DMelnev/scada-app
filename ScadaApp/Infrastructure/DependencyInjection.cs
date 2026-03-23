using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScadaApp.Data.Repositories;
using ScadaApp.Infrastructure.Logging;
using ScadaApp.Models;
using ScadaApp.Services;
using ScadaApp.ViewModels;
using ScadaApp.Views;
using Serilog;
using Serilog.Extensions.Logging;
using System.Windows;
using System.Windows.Threading;

namespace ScadaApp.Infrastructure;

/// <summary>Регистрация всех сервисов приложения в DI-контейнере.</summary>
public static class DependencyInjection
{
    public static IServiceCollection AddScadaServices(
        this IServiceCollection services,
        ObservableLogSink logSink)
    {
        // Serilog
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File("logs/scada.log", rollingInterval: RollingInterval.Day)
            .WriteTo.Sink(logSink)
            .CreateLogger();

        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(
            new SerilogLoggerFactory(serilogLogger, true));

        services.AddLogging(b => b.ClearProviders().AddSerilog(serilogLogger));

        // Config (placeholder — will be replaced after load)
        services.AddSingleton<AppConfig>();
        services.AddSingleton(sp => sp.GetRequiredService<AppConfig>().Database);
        services.AddSingleton(sp => sp.GetRequiredService<AppConfig>().Visualization);

        // Infrastructure
        services.AddSingleton(logSink);

        // Services
        services.AddSingleton<IConfigurationService, ConfigurationService>();
        services.AddSingleton<DeviceManager>();
        services.AddSingleton<PollingService>();
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<IDatabaseService>(sp => sp.GetRequiredService<DatabaseService>());
        services.AddSingleton<I3DVisualizationService, Dummy3DVisualizationService>();

        // Repositories
        services.AddTransient<ITagValueRepository, TagValueRepository>();

        // ViewModels
        services.AddTransient<MainViewModel>();
        services.AddTransient<DeviceSettingsViewModel>();
        services.AddTransient<TagSettingsViewModel>();
        services.AddTransient<DatabaseSettingsViewModel>();
        services.AddTransient<VisualizationSettingsViewModel>();

        // Views
        services.AddTransient<MainWindow>();
        services.AddTransient<DeviceSettingsWindow>();
        services.AddTransient<TagSettingsWindow>();
        services.AddTransient<DatabaseSettingsWindow>();
        services.AddTransient<VisualizationSettingsWindow>();

        return services;
    }
}
