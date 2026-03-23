using Microsoft.Extensions.Logging;
using ScadaApp.Drivers;
using ScadaApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScadaApp.Services;

/// <summary>Менеджер устройств — создаёт и хранит экземпляры драйверов контроллеров.</summary>
public class DeviceManager
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DeviceManager> _logger;
    private readonly Dictionary<string, IControllerDriver> _drivers = new();

    public DeviceManager(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DeviceManager>();
    }

    /// <summary>Инициализировать драйверы по конфигурации устройств.</summary>
    public async Task InitializeAsync(IEnumerable<DeviceConfig> devices)
    {
        foreach (var driver in _drivers.Values)
        {
            await driver.DisconnectAsync();
            driver.Dispose();
        }
        _drivers.Clear();

        foreach (var device in devices)
        {
            var driver = CreateDriver(device);
            _drivers[device.Id] = driver;
        }
    }

    /// <summary>Подключиться ко всем устройствам.</summary>
    public async Task ConnectAllAsync(IEnumerable<DeviceConfig> configs)
    {
        foreach (var config in configs)
        {
            var connected = await ConnectDeviceAsync(config);
            if (!connected)
                _logger.LogWarning("Не удалось подключиться к устройству {Name}", config.Name);
        }
    }

    /// <summary>Подключиться к устройству по конфигурации.</summary>
    public async Task<bool> ConnectDeviceAsync(DeviceConfig config)
    {
        if (_drivers.TryGetValue(config.Id, out var driver))
        {
            return await driver.ConnectAsync(config);
        }
        return false;
    }

    /// <summary>Отключиться от всех устройств.</summary>
    public async Task DisconnectAllAsync()
    {
        foreach (var driver in _drivers.Values)
        {
            await driver.DisconnectAsync();
        }
    }

    /// <summary>Получить драйвер по идентификатору устройства.</summary>
    public IControllerDriver? GetDriver(string deviceId)
    {
        _drivers.TryGetValue(deviceId, out var driver);
        return driver;
    }

    /// <summary>Получить все устройства и их статусы подключения.</summary>
    public IReadOnlyDictionary<string, IControllerDriver> GetAllDrivers() => _drivers;

    private IControllerDriver CreateDriver(DeviceConfig device)
    {
        return device.DriverType switch
        {
            "Siemens" => new SiemensDriver(device.Id, _loggerFactory.CreateLogger<SiemensDriver>()),
            _ => new SiemensDriver(device.Id, _loggerFactory.CreateLogger<SiemensDriver>())
        };
    }
}
