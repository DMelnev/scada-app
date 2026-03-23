using Microsoft.Extensions.Logging;
using S7.Net;
using ScadaApp.Models;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaApp.Drivers;

/// <summary>
/// Драйвер для контроллеров Siemens S7 серии S7-1200, S7-1500, S7-300, S7-400, S7-200.
/// Использует библиотеку S7.Net (S7netplus) для низкоуровневой коммуникации
/// по протоколу ISO on TCP (промышленный Ethernet для Siemens).
///
/// Протокол ISO on TCP:
///   - Транспортный уровень: TCP (порт 102 по умолчанию)
///   - Прикладной уровень: S7Comm — фирменный протокол Siemens
///   - Адресация: IP + Rack + Slot (физическое расположение CPU в стойке)
///
/// Библиотека S7.Net (s7netplus на NuGet):
///   Плюс — готовое решение, не нужно реализовывать протокол с нуля.
///   Класс Plc — основной класс для работы с контроллером.
/// </summary>
public class SiemensDriver : IControllerDriver
{
    private readonly ILogger<SiemensDriver> _logger;

    /// <summary>
    /// Экземпляр класса Plc из библиотеки S7.Net.
    /// Содержит TCP-соединение с контроллером и методы чтения/записи.
    /// Знак "?" — может быть null (до вызова ConnectAsync).
    /// </summary>
    private Plc? _plc;

    /// <summary>Последняя использованная конфигурация устройства (для повторного подключения).</summary>
    private DeviceConfig? _config;

    public SiemensDriver(string deviceId, ILogger<SiemensDriver> logger)
    {
        DeviceId = deviceId;
        _logger = logger;
    }

    /// <summary>Идентификатор устройства (GUID). Задаётся при создании драйвера.</summary>
    public string DeviceId { get; }

    /// <summary>
    /// Возвращает true если контроллер подключён.
    /// _plc?.IsConnected — если _plc == null, возвращает false (null-conditional operator).
    /// ?? false — если результат null, возвращаем false.
    /// </summary>
    public bool IsConnected => _plc?.IsConnected ?? false;

    /// <summary>
    /// Асинхронно подключается к контроллеру Siemens S7.
    /// Создаёт объект Plc с параметрами из конфигурации и открывает соединение.
    /// </summary>
    public async Task<bool> ConnectAsync(DeviceConfig config, CancellationToken ct = default)
    {
        _config = config;
        try
        {
            // Преобразуем строку типа CPU ("S71500") в перечисление CpuType.S71500.
            var cpuType = ParseCpuType(config.CpuType);

            // Создаём объект контроллера: тип CPU, IP-адрес, стойка (rack), слот (slot).
            // (short) — явное приведение типа int → short (S7.Net ожидает short).
            _plc = new Plc(cpuType, config.IpAddress, (short)config.Rack, (short)config.Slot);

            // Открываем TCP-соединение асинхронно.
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

    /// <summary>
    /// Закрывает TCP-соединение с контроллером.
    /// _plc?.Close() — безопасный вызов: если _plc == null, Close() не вызывается.
    /// </summary>
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
        await Task.CompletedTask; // Формально async, но операция синхронная
    }

    /// <summary>
    /// Асинхронно читает значение тега по его адресу.
    ///
    /// S7.Net читает значение синхронно (_plc.Read()), поэтому оборачиваем в Task.Run()
    /// чтобы не блокировать поток (выполняем в пуле потоков).
    ///
    /// Исключения:
    ///   PlcException — специфичное исключение S7.Net (неверный адрес, таймаут и т.д.)
    ///   Exception    — любые другие ошибки
    /// </summary>
    public async Task<TagReadResult> ReadTagAsync(TagConfig tag, CancellationToken ct = default)
    {
        // Проверяем подключение перед чтением.
        if (_plc == null || !_plc.IsConnected)
        {
            return new TagReadResult { Success = false, Quality = "Bad", ErrorMessage = "Нет подключения" };
        }

        try
        {
            // Task.Run(() => ...) — выполняем синхронный метод в пуле потоков асинхронно.
            // tag.Address — адрес тега в формате "DB1.DBD0", "M0.0" и т.д.
            var value = await Task.Run(() => _plc.Read(tag.Address), ct);

            return new TagReadResult { Success = true, Value = value, Quality = "Good" };
        }
        catch (PlcException ex)
        {
            // Ошибка на уровне протокола S7 (неверный адрес, область недоступна).
            _logger.LogWarning(ex, "Ошибка чтения тега {TagName} с устройства {DeviceId}", tag.Name, DeviceId);
            return new TagReadResult { Success = false, Quality = "Bad", ErrorMessage = ex.Message };
        }
        catch (Exception ex)
        {
            // Неожиданная ошибка (сетевая, timeout и т.д.).
            _logger.LogError(ex, "Неожиданная ошибка чтения тега {TagName}", tag.Name);
            return new TagReadResult { Success = false, Quality = "Bad", ErrorMessage = ex.Message };
        }
    }

    /// <summary>
    /// Асинхронно записывает значение в тег контроллера.
    /// _plc.Write(адрес, значение) — отправляет команду записи по протоколу S7.
    /// </summary>
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

    /// <summary>
    /// Преобразует строку с именем типа CPU в перечисление S7.Net.CpuType.
    ///
    /// switch expression (C# 8+) — компактная форма switch-case.
    /// ToUpperInvariant() — приводит к верхнему регистру независимо от локали.
    /// Символ "_" — default case (для любого другого значения возвращаем S71500).
    /// </summary>
    private static CpuType ParseCpuType(string cpuTypeStr) =>
        cpuTypeStr.ToUpperInvariant() switch
        {
            "S71200" => CpuType.S71200,
            "S71500" => CpuType.S71500,
            "S7300"  => CpuType.S7300,
            "S7400"  => CpuType.S7400,
            "S7200"  => CpuType.S7200,
            _        => CpuType.S71500  // По умолчанию — S71500
        };

    /// <summary>
    /// Освобождает ресурсы (IDisposable).
    /// Закрывает TCP-соединение и освобождает объект Plc.
    /// </summary>
    public void Dispose()
    {
        _plc?.Close();
        _plc = null;
    }
}
