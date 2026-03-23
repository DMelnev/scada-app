using Microsoft.Extensions.Logging;
using S7.Net;
using ScadaApp.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaApp.Drivers;

/// <summary>Драйвер для контроллеров Siemens S7 через библиотеку S7netplus.</summary>
public class SiemensDriver : IControllerDriver
{
    private readonly ILogger<SiemensDriver> _logger;
    private Plc? _plc;
    private DeviceConfig? _config;

    public SiemensDriver(string deviceId, ILogger<SiemensDriver> logger)
    {
        DeviceId = deviceId;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string DeviceId { get; }

    /// <inheritdoc/>
    public bool IsConnected => _plc?.IsConnected ?? false;

    /// <inheritdoc/>
    public async Task<bool> ConnectAsync(DeviceConfig config, CancellationToken ct = default)
    {
        _config = config;
        try
        {
            var cpuType = ParseCpuType(config.CpuType);
            _plc = new Plc(cpuType, config.IpAddress, (short)config.Rack, (short)config.Slot);
            await _plc.OpenAsync(ct);
            _logger.LogInformation("Подключено к устройству {Name} ({Ip})", config.Name, config.IpAddress);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка подключения к устройству {Name} ({Ip})", config.Name, config.IpAddress);
            return false;
        }
    }

    /// <inheritdoc/>
    public async Task DisconnectAsync()
    {
        try
        {
            _plc?.Close();
            _logger.LogInformation("Отключено от устройства {DeviceId}", DeviceId);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Ошибка при отключении от устройства {DeviceId}", DeviceId);
        }
        await Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task<TagReadResult> ReadTagAsync(TagConfig tag, CancellationToken ct = default)
    {
        if (_plc == null || !_plc.IsConnected)
        {
            return new TagReadResult { Success = false, Quality = "Bad", ErrorMessage = "Нет подключения" };
        }

        try
        {
            var value = await Task.Run(() => _plc.Read(tag.Address), ct);
            return new TagReadResult { Success = true, Value = value, Quality = "Good" };
        }
        catch (PlcException ex)
        {
            _logger.LogWarning(ex, "Ошибка чтения тега {TagName} с устройства {DeviceId}", tag.Name, DeviceId);
            return new TagReadResult { Success = false, Quality = "Bad", ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Неожиданная ошибка чтения тега {TagName}", tag.Name);
            return new TagReadResult { Success = false, Quality = "Bad", ErrorMessage = ex.Message };
        }
    }

    /// <inheritdoc/>
    public async Task<bool> WriteTagAsync(TagConfig tag, object value, CancellationToken ct = default)
    {
        if (_plc == null || !_plc.IsConnected)
        {
            _logger.LogWarning("Попытка записи тега {TagName} без подключения", tag.Name);
            return false;
        }

        try
        {
            await Task.Run(() => _plc.Write(tag.Address, value), ct);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка записи тега {TagName} на устройство {DeviceId}", tag.Name, DeviceId);
            return false;
        }
    }

    /// <summary>Преобразует строку типа CPU в перечисление S7.Net.CpuType.</summary>
    private static CpuType ParseCpuType(string cpuTypeStr) =>
        cpuTypeStr.ToUpperInvariant() switch
        {
            "S71200" => CpuType.S71200,
            "S71500" => CpuType.S71500,
            "S7300"  => CpuType.S7300,
            "S7400"  => CpuType.S7400,
            "S7200"  => CpuType.S7200,
            _        => CpuType.S71500
        };

    public void Dispose()
    {
        _plc?.Close();
        _plc = null;
    }
}
