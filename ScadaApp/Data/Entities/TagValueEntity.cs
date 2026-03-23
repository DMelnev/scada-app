using System;

namespace ScadaApp.Data.Entities;

/// <summary>Сущность для хранения значений тегов в базе данных.</summary>
public class TagValueEntity
{
    public long Id { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }

    /// <summary>Качество значения: "Good", "Bad", "Uncertain"</summary>
    public string Quality { get; set; } = "Good";

    public string DeviceName { get; set; } = "";
}