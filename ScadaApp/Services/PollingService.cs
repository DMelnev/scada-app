using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScadaApp.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaApp.Services;

/// <summary>Аргументы события изменения значения тега.</summary>
public class TagValueChangedEventArgs : EventArgs
{
    public string DeviceId { get; init; } = "";
    public string DeviceName { get; init; } = "";
    public string TagName { get; init; } = "";
    public object? Value { get; init; }
    public string Quality { get; init; } = "Good";
    public DateTime Timestamp { get; init; } = DateTime.UtcNow;
}

/// <summary>Сервис периодического опроса тегов контроллеров.</summary>
public class PollingService : IHostedService, IDisposable
{
    private readonly DeviceManager _deviceManager;
    private readonly ILogger<PollingService> _logger;
    private readonly List<DeviceConfig> _deviceConfigs = new();
    private CancellationTokenSource? _cts;
    private readonly List<Task> _pollingTasks = new();

    /// <summary>Событие изменения значения тега.</summary>
    public event EventHandler<TagValueChangedEventArgs>? TagValueChanged;

    public bool IsPolling { get; private set; }

    public PollingService(DeviceManager deviceManager, ILogger<PollingService> logger)
    {
        _deviceManager = deviceManager;
        _logger = logger;
    }

    /// <summary>Установить конфигурацию устройств для опроса.</summary>
    public void Configure(IEnumerable<DeviceConfig> devices)
    {
        _deviceConfigs.Clear();
        _deviceConfigs.AddRange(devices);
    }

    /// <inheritdoc/>
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Запуск сервиса опроса тегов");
        IsPolling = true;
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

        await _deviceManager.InitializeAsync(_deviceConfigs);

        foreach (var device in _deviceConfigs)
        {
            var connected = await _deviceManager.ConnectDeviceAsync(device);
            if (!connected)
            {
                _logger.LogWarning("Не удалось подключиться к устройству {Name}", device.Name);
            }

            var task = PollDeviceAsync(device, _cts.Token);
            _pollingTasks.Add(task);
        }
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _logger.LogInformation("Остановка сервиса опроса тегов");
        IsPolling = false;
        _cts?.Cancel();

        try
        {
            await Task.WhenAll(_pollingTasks);
        }
        catch (OperationCanceledException)
        {
            // ожидаемо
        }
        _pollingTasks.Clear();

        await _deviceManager.DisconnectAllAsync();
    }

    private async Task PollDeviceAsync(DeviceConfig device, CancellationToken ct)
    {
        _logger.LogInformation("Начало опроса устройства {Name}", device.Name);

        while (!ct.IsCancellationRequested)
        {
            var driver = _deviceManager.GetDriver(device.Id);
            if (driver == null) break;

            if (!driver.IsConnected)
            {
                _logger.LogDebug("Устройство {Name} недоступно, попытка переподключения...", device.Name);
                await _deviceManager.ConnectDeviceAsync(device);
                await Task.Delay(device.PollingIntervalMs, ct).ConfigureAwait(false);
                continue;
            }

            foreach (var tag in device.Tags)
            {
                if (!tag.Enabled || ct.IsCancellationRequested) continue;

                try
                {
                    var result = await driver.ReadTagAsync(tag, ct);
                    var args = new TagValueChangedEventArgs
                    {
                        DeviceId = device.Id,
                        DeviceName = device.Name,
                        TagName = tag.Name,
                        Value = result.Value,
                        Quality = result.Quality,
                        Timestamp = DateTime.UtcNow
                    };
                    TagValueChanged?.Invoke(this, args);
                }
                catch (Exception ex) when (!ct.IsCancellationRequested)
                {
                    _logger.LogError(ex, "Ошибка опроса тега {Tag} устройства {Device}", tag.Name, device.Name);
                }
            }

            try
            {
                await Task.Delay(device.PollingIntervalMs, ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    public void Dispose()
    {
        _cts?.Cancel();
        _cts?.Dispose();
    }
}
