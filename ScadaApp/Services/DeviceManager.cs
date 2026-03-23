using Microsoft.Extensions.Logging;
using ScadaApp.Drivers;
using ScadaApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ScadaApp.Services;

/// <summary>
/// Менеджер устройств — создаёт, хранит и управляет экземплярами драйверов контроллеров.
///
/// Роль класса: DeviceManager отвечает за создание правильного типа драйвера
/// (фабричный паттерн) и хранит словарь "ID устройства → драйвер".
///
/// PollingService использует DeviceManager для получения драйвера по ID устройства
/// и вызова на нём операций чтения/подключения.
/// </summary>
public class DeviceManager
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly ILogger<DeviceManager> _logger;

    /// <summary>
    /// Словарь: ключ = ID устройства (GUID), значение = экземпляр драйвера.
    /// Dictionary&lt;TKey, TValue&gt; — коллекция "ключ-значение" с быстрым поиском по ключу.
    /// </summary>
    private readonly Dictionary<string, IControllerDriver> _drivers = new();

    public DeviceManager(ILoggerFactory loggerFactory)
    {
        _loggerFactory = loggerFactory;
        _logger = loggerFactory.CreateLogger<DeviceManager>();
    }

    /// <summary>
    /// Инициализирует драйверы по конфигурации устройств.
    /// Сначала отключает и удаляет все существующие драйверы,
    /// затем создаёт новые для каждого устройства из списка.
    ///
    /// IEnumerable&lt;DeviceConfig&gt; — принимает любую коллекцию (List, массив и т.д.).
    /// </summary>
    public async Task InitializeAsync(IEnumerable<DeviceConfig> devices)
    {
        // Отключаем и освобождаем все существующие драйверы.
        foreach (var driver in _drivers.Values)
        {
            await driver.DisconnectAsync();
            driver.Dispose(); // Освобождаем ресурсы (соединения, порты и т.д.)
        }
        _drivers.Clear();

        // Создаём новый драйвер для каждого устройства и добавляем в словарь.
        foreach (var device in devices)
        {
            var driver = CreateDriver(device);
            _drivers[device.Id] = driver; // device.Id — ключ словаря
        }
    }

    /// <summary>
    /// Подключается ко всем устройствам в списке конфигураций.
    /// </summary>
    public async Task ConnectAllAsync(IEnumerable<DeviceConfig> configs)
    {
        foreach (var config in configs)
        {
            var connected = await ConnectDeviceAsync(config);
            if (!connected)
                _logger.LogWarning("Не удалось подключиться к устройству {Name}", config.Name);
        }
    }

    /// <summary>
    /// Подключается к одному устройству по его конфигурации.
    /// Ищет драйвер по ID устройства в словаре и вызывает ConnectAsync().
    ///
    /// TryGetValue — безопасный поиск в словаре: возвращает false если ключ не найден
    /// (вместо исключения при обычном обращении _drivers[key]).
    /// </summary>
    public async Task<bool> ConnectDeviceAsync(DeviceConfig config)
    {
        if (_drivers.TryGetValue(config.Id, out var driver))
        {
            return await driver.ConnectAsync(config);
        }
        return false; // Драйвер не найден
    }

    /// <summary>
    /// Отключается от всех устройств.
    /// Вызывается при StopAsync() в PollingService.
    /// </summary>
    public async Task DisconnectAllAsync()
    {
        foreach (var driver in _drivers.Values)
        {
            await driver.DisconnectAsync();
        }
    }

    /// <summary>
    /// Возвращает драйвер устройства по его ID.
    /// Возвращает null, если устройство не найдено.
    /// Используется в цикле опроса PollingService.
    /// </summary>
    public IControllerDriver? GetDriver(string deviceId)
    {
        _drivers.TryGetValue(deviceId, out var driver);
        return driver;
    }

    /// <summary>
    /// Возвращает словарь всех драйверов.
    /// IReadOnlyDictionary — только для чтения (нельзя добавить/удалить элементы).
    /// </summary>
    public IReadOnlyDictionary<string, IControllerDriver> GetAllDrivers() => _drivers;

    /// <summary>
    /// Фабричный метод: создаёт нужный тип драйвера по конфигурации устройства.
    /// switch expression (C# 8+) — компактная запись switch-case.
    ///
    /// Сейчас поддерживается только Siemens. OpcUa и другие — на будущее.
    /// </summary>
    private IControllerDriver CreateDriver(DeviceConfig device)
    {
        return device.DriverType switch
        {
            "Siemens" => new SiemensDriver(device.Id, _loggerFactory.CreateLogger<SiemensDriver>()),
            // Для всех остальных типов тоже используем SiemensDriver (заглушка)
            _ => new SiemensDriver(device.Id, _loggerFactory.CreateLogger<SiemensDriver>())
        };
    }
}
