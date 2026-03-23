using Microsoft.Extensions.Logging;
using ScadaApp.Models;
using System.Threading.Tasks;

namespace ScadaApp.Services;

/// <summary>
/// Заглушка (stub / dummy) сервиса 3D-визуализации.
/// Реализует интерфейс I3DVisualizationService, но вместо реальной работы
/// только записывает все вызовы методов в лог.
///
/// Зачем нужна заглушка?
///   - Позволяет тестировать и запускать приложение без реального 3D-движка
///   - Показывает в логе, какие команды должны отправляться в 3D-систему
///   - В будущем будет заменена на реальный WebSocket-клиент
///
/// В DI-контейнере зарегистрирована как:
///   services.AddSingleton&lt;I3DVisualizationService, Dummy3DVisualizationService&gt;()
/// То есть при запросе I3DVisualizationService всегда возвращается этот объект.
/// </summary>
public class Dummy3DVisualizationService : I3DVisualizationService
{
    private readonly ILogger<Dummy3DVisualizationService> _logger;

    public Dummy3DVisualizationService(ILogger<Dummy3DVisualizationService> logger)
    {
        _logger = logger;
    }

    /// <summary>
    /// Флаг активности сервиса.
    /// private set — свойство можно изменить только внутри класса.
    /// </summary>
    public bool IsEnabled { get; private set; }

    /// <summary>
    /// Инициализирует сервис на основе конфигурации.
    /// В заглушке — только сохраняет флаг Enabled и пишет в лог.
    /// Task.CompletedTask — возвращаем уже завершённую задачу (ничего не делаем асинхронно).
    /// </summary>
    public Task InitializeAsync(VisualizationConfig config)
    {
        IsEnabled = config.Enabled;
        _logger.LogInformation("3D визуализация инициализирована (URL: {Url}, Enabled: {Enabled})",
            config.ServiceUrl, config.Enabled);
        return Task.CompletedTask;
    }

    /// <summary>Обновить позицию объекта в 3D-сцене. В заглушке — только лог.</summary>
    public Task UpdatePositionAsync(string objectName, double x, double y, double z)
    {
        _logger.LogDebug("[3D] UpdatePosition: {Object} X={X} Y={Y} Z={Z}", objectName, x, y, z);
        return Task.CompletedTask;
    }

    /// <summary>Обновить поворот объекта в 3D-сцене. В заглушке — только лог.</summary>
    public Task UpdateRotationAsync(string objectName, double rx, double ry, double rz)
    {
        _logger.LogDebug("[3D] UpdateRotation: {Object} RX={Rx} RY={Ry} RZ={Rz}", objectName, rx, ry, rz);
        return Task.CompletedTask;
    }

    /// <summary>Установить состояние объекта в 3D-сцене. В заглушке — только лог.</summary>
    public Task SetStateAsync(string objectName, string state, object value)
    {
        _logger.LogDebug("[3D] SetState: {Object} {State}={Value}", objectName, state, value);
        return Task.CompletedTask;
    }

    /// <summary>Остановить сервис визуализации. В заглушке — только лог и сброс флага.</summary>
    public Task ShutdownAsync()
    {
        _logger.LogInformation("3D визуализация остановлена");
        IsEnabled = false;
        return Task.CompletedTask;
    }
}
