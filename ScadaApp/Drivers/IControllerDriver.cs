using ScadaApp.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaApp.Drivers;

/// <summary>Результат чтения тега с контроллера.</summary>
public class TagReadResult
{
    public bool Success { get; set; }
    public object? Value { get; set; }
    public string Quality { get; set; } = "Good";
    public string? ErrorMessage { get; set; }
}

/// <summary>Абстракция драйвера контроллера.</summary>
public interface IControllerDriver : IDisposable
{
    /// <summary>Идентификатор устройства.</summary>
    string DeviceId { get; }

    /// <summary>Признак активного подключения.</summary>
    bool IsConnected { get; }

    /// <summary>Подключиться к устройству.</summary>
    Task<bool> ConnectAsync(DeviceConfig config, CancellationToken ct = default);

    /// <summary>Отключиться от устройства.</summary>
    Task DisconnectAsync();

    /// <summary>Прочитать значение тега.</summary>
    Task<TagReadResult> ReadTagAsync(TagConfig tag, CancellationToken ct = default);

    /// <summary>Записать значение тега.</summary>
    Task<bool> WriteTagAsync(TagConfig tag, object value, CancellationToken ct = default);
}
