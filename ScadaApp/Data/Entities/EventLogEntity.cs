using System;

namespace ScadaApp.Data.Entities;

/// <summary>Сущность записи журнала событий.</summary>
public class EventLogEntity
{
    public long Id { get; set; }
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "Info";
    public string Message { get; set; } = "";
    public string Source { get; set; } = "";
}
