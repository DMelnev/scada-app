using Microsoft.Extensions.Logging;
using ScadaApp.Models;
using System.Threading.Tasks;

namespace ScadaApp.Services;

/// <summary>Заглушка сервиса 3D-визуализации — логирует все вызовы.</summary>
public class Dummy3DVisualizationService : I3DVisualizationService
{
    private readonly ILogger<Dummy3DVisualizationService> _logger;

    public Dummy3DVisualizationService(ILogger<Dummy3DVisualizationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public bool IsEnabled { get; private set; }

    /// <inheritdoc/>
    public Task InitializeAsync(VisualizationConfig config)
    {
        IsEnabled = config.Enabled;
        _logger.LogInformation("3D визуализация инициализирована (URL: {Url}, Enabled: {Enabled})",
            config.ServiceUrl, config.Enabled);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpdatePositionAsync(string objectName, double x, double y, double z)
    {
        _logger.LogDebug("[3D] UpdatePosition: {Object} X={X} Y={Y} Z={Z}", objectName, x, y, z);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task UpdateRotationAsync(string objectName, double rx, double ry, double rz)
    {
        _logger.LogDebug("[3D] UpdateRotation: {Object} RX={Rx} RY={Ry} RZ={Rz}", objectName, rx, ry, rz);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task SetStateAsync(string objectName, string state, object value)
    {
        _logger.LogDebug("[3D] SetState: {Object} {State}={Value}", objectName, state, value);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public Task ShutdownAsync()
    {
        _logger.LogInformation("3D визуализация остановлена");
        IsEnabled = false;
        return Task.CompletedTask;
    }
}
