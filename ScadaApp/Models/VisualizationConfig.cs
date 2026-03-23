using System.Collections.Generic;

namespace ScadaApp.Models;

/// <summary>Включено/выкл, URL, маппинг тег→объект/свойство.</summary>
public class VisualizationConfig
{
    public bool Enabled { get; set; } = false;
    public string ServiceUrl { get; set; } = "ws://localhost:8080";
    public List<TagVisualizationMapping> Mappings { get; set; } = new();
}

/// <summary>Маппинг тега на объект и свойство в 3D-сцене.</summary>
public class TagVisualizationMapping
{
    public string TagName { get; set; } = "";
    public string ObjectName { get; set; } = "";

    /// <summary>Свойство: "PositionX", "PositionY", "PositionZ", "RotationX", "State" и др.</summary>
    public string Property { get; set; } = "";
}
