using System;

namespace ScadaApp.Models;

/// <summary>Имя тега, адрес S7, тип данных, интервал опроса.</summary>
public class TagConfig
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; } = "";

    /// <summary>Адрес тега, например "DB1.DBD0" для S7.</summary>
    public string Address { get; set; } = "";

    /// <summary>Тип данных: Bool, Int, DInt, Real, Word и др.</summary>
    public string DataType { get; set; } = "Real";

    public string Description { get; set; } = "";
    public bool Enabled { get; set; } = true;
}
