using System.Collections.Generic;

namespace ScadaApp.Models;

/// <summary>Корневой конфиг приложения, сериализуется в config.json.</summary>
public class AppConfig
{
    public List<DeviceConfig> Devices { get; set; } = new();
    public DatabaseConfig Database { get; set; } = new();
    public VisualizationConfig Visualization { get; set; } = new();
}
