using ScadaApp.Models;
using System.Threading.Tasks;

namespace ScadaApp.Services;

/// <summary>Интерфейс сервиса 3D-визуализации.</summary>
public interface I3DVisualizationService
{
    /// <summary>Включён ли сервис.</summary>
    bool IsEnabled { get; }

    /// <summary>Инициализировать сервис по конфигурации.</summary>
    Task InitializeAsync(VisualizationConfig config);

    /// <summary>Обновить позицию объекта.</summary>
    Task UpdatePositionAsync(string objectName, double x, double y, double z);

    /// <summary>Обновить поворот объекта.</summary>
    Task UpdateRotationAsync(string objectName, double rx, double ry, double rz);

    /// <summary>Установить состояние объекта.</summary>
    Task SetStateAsync(string objectName, string state, object value);

    /// <summary>Остановить сервис.</summary>
    Task ShutdownAsync();
}
