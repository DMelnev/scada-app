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

/// <summary>
/// Статический класс, содержащий метод-расширение для регистрации
/// всех сервисов приложения в DI-контейнере (Dependency Injection Container).
///
/// Dependency Injection (DI) — паттерн, при котором объекты не создают свои
/// зависимости сами, а получают их снаружи (через конструктор).
/// DI-контейнер автоматически создаёт нужные объекты и передаёт их при запросе.
///
/// Метод-расширение (Extension Method) — специальный статический метод,
/// который можно вызывать как обычный метод на объекте другого типа.
/// Здесь: services.AddScadaServices() вызывается на IServiceCollection.
/// "this IServiceCollection services" — ключевое слово this делает его расширением.
///
/// Жизненный цикл сервисов (Lifetime):
///   Singleton  — один экземпляр на всё приложение
///   Transient  — новый экземпляр при каждом запросе
///   Scoped     — один экземпляр на "область" (в WPF не используется)
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddScadaServices(
        this IServiceCollection services,
        ObservableLogSink logSink)
    {
        // ── LOGGING (Serilog) ────────────────────────────────────────────────
        // Serilog — популярная библиотека структурированного логирования.
        // Настраиваем два "стока" (sink): файл на диске + ObservableLogSink для UI.
        var serilogLogger = new LoggerConfiguration()
            .MinimumLevel.Debug() // Записываем все сообщения от уровня Debug и выше
            .WriteTo.File("logs/scada.log", rollingInterval: RollingInterval.Day) // Файл, новый файл каждый день
            .WriteTo.Sink(logSink) // ObservableLogSink — отображает логи в таблице UI
            .CreateLogger();

        // Регистрируем ILoggerFactory (нужен для создания ILogger<T>).
        services.AddSingleton<Microsoft.Extensions.Logging.ILoggerFactory>(
            new SerilogLoggerFactory(serilogLogger, true));

        // Регистрируем стандартный механизм логирования через Serilog.
        services.AddLogging(b => b.ClearProviders().AddSerilog(serilogLogger));

        // ── КОНФИГУРАЦИЯ ─────────────────────────────────────────────────────
        // AppConfig — корневой объект конфигурации, singleton (один на всё приложение).
        // Содержимое будет заполнено из config.json в App.xaml.cs после запуска хоста.
        services.AddSingleton<AppConfig>();

        // DatabaseConfig и VisualizationConfig — подобъекты AppConfig.
        // Регистрируем их как отдельные singleton'ы, получая через AppConfig.
        // sp — ServiceProvider (контейнер сервисов), используется для получения
        // уже зарегистрированных сервисов.
        services.AddSingleton(sp => sp.GetRequiredService<AppConfig>().Database);
        services.AddSingleton(sp => sp.GetRequiredService<AppConfig>().Visualization);

        // ── ИНФРАСТРУКТУРА ───────────────────────────────────────────────────
        // ObservableLogSink — уже создан в App.xaml.cs и передан сюда параметром.
        // Регистрируем его как singleton, чтобы все сервисы получали один и тот же объект.
        services.AddSingleton(logSink);

        // ── СЕРВИСЫ ──────────────────────────────────────────────────────────
        // IConfigurationService → ConfigurationService:
        // При запросе IConfigurationService DI вернёт экземпляр ConfigurationService.
        services.AddSingleton<IConfigurationService, ConfigurationService>();

        // DeviceManager — управляет драйверами устройств, singleton.
        services.AddSingleton<DeviceManager>();

        // PollingService — сервис опроса, singleton (один на всё приложение).
        services.AddSingleton<PollingService>();

        // DatabaseService — сервис записи в БД.
        // Регистрируем дважды: как конкретный тип и как интерфейс IDatabaseService.
        // Оба возвращают ОДИН И ТОТ ЖЕ экземпляр (sp.GetRequiredService<DatabaseService>()).
        services.AddSingleton<DatabaseService>();
        services.AddSingleton<IDatabaseService>(sp => sp.GetRequiredService<DatabaseService>());

        // I3DVisualizationService → Dummy3DVisualizationService:
        // Заглушка сервиса 3D-визуализации.
        services.AddSingleton<I3DVisualizationService, Dummy3DVisualizationService>();

        // ── РЕПОЗИТОРИИ ──────────────────────────────────────────────────────
        // Transient — новый экземпляр при каждом запросе (репозиторий не хранит состояние).
        services.AddTransient<ITagValueRepository, TagValueRepository>();

        // ── VIEWMODELS ───────────────────────────────────────────────────────
        // Transient — каждый раз при открытии окна создаётся новый ViewModel.
        services.AddTransient<MainViewModel>();
        services.AddTransient<DeviceSettingsViewModel>();
        services.AddTransient<TagSettingsViewModel>();
        services.AddTransient<DatabaseSettingsViewModel>();
        services.AddTransient<VisualizationSettingsViewModel>();

        // ── VIEWS (ОКНА) ─────────────────────────────────────────────────────
        // Transient — каждый раз при запросе создаётся новый экземпляр окна.
        // WPF-окна создаются через DI, чтобы их конструкторы могли получать ViewModel.
        services.AddTransient<MainWindow>();
        services.AddTransient<DeviceSettingsWindow>();
        services.AddTransient<TagSettingsWindow>();
        services.AddTransient<DatabaseSettingsWindow>();
        services.AddTransient<VisualizationSettingsWindow>();

        return services; // Возвращаем коллекцию для поддержки method chaining
    }
}
