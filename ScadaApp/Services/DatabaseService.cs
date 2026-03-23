using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using ScadaApp.Data;
using ScadaApp.Data.Entities;
using ScadaApp.Models;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaApp.Services;

/// <summary>Сервис буферизованного сохранения значений тегов в базу данных.</summary>
public class DatabaseService : IHostedService, IDatabaseService, IDisposable
{
    private readonly PollingService _pollingService;
    private readonly DatabaseConfig _dbConfig;
    private readonly ILogger<DatabaseService> _logger;
    private readonly ConcurrentQueue<TagValueEntity> _tagQueue = new();
    private readonly ConcurrentQueue<EventLogEntity> _eventQueue = new();
    private Timer? _flushTimer;
    private CancellationToken _cancellationToken;

    public DatabaseService(
        PollingService pollingService,
        DatabaseConfig dbConfig,
        ILogger<DatabaseService> logger)
    {
        _pollingService = pollingService;
        _dbConfig = dbConfig;
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Enqueue(TagValueEntity value)
    {
        _tagQueue.Enqueue(value);
    }

    /// <inheritdoc/>
    public void EnqueueEvent(EventLogEntity eventLog)
    {
        _eventQueue.Enqueue(eventLog);
    }

    /// <inheritdoc/>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_dbConfig.Enabled)
        {
            _logger.LogInformation("Сохранение в БД отключено");
            return Task.CompletedTask;
        }

        _cancellationToken = cancellationToken;
        _pollingService.TagValueChanged += OnTagValueChanged;

        _flushTimer = new Timer(
            async _ => await FlushBufferAsync(),
            null,
            TimeSpan.FromMilliseconds(_dbConfig.SaveIntervalMs),
            TimeSpan.FromMilliseconds(_dbConfig.SaveIntervalMs));

        _logger.LogInformation("Сервис БД запущен (интервал: {Interval}ms)", _dbConfig.SaveIntervalMs);
        return Task.CompletedTask;
    }

    /// <inheritdoc/>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _pollingService.TagValueChanged -= OnTagValueChanged;
        _flushTimer?.Change(Timeout.Infinite, 0);
        await FlushBufferAsync();
        _logger.LogInformation("Сервис БД остановлен");
    }

    private void OnTagValueChanged(object? sender, TagValueChangedEventArgs e)
    {
        var entity = new TagValueEntity
        {
            TagName = e.TagName,
            Value = e.Value?.ToString() ?? "",
            Timestamp = e.Timestamp,
            Quality = e.Quality,
            DeviceName = e.DeviceName
        };
        Enqueue(entity);

        if (_tagQueue.Count >= _dbConfig.BufferSize)
        {
            _ = FlushBufferAsync();
        }
    }

    private async Task FlushBufferAsync()
    {
        var tagBatch = new List<TagValueEntity>();
        while (_tagQueue.TryDequeue(out var item))
            tagBatch.Add(item);

        var eventBatch = new List<EventLogEntity>();
        while (_eventQueue.TryDequeue(out var ev))
            eventBatch.Add(ev);

        if (tagBatch.Count == 0 && eventBatch.Count == 0) return;

        try
        {
            await using var context = new ScadaDbContext(_dbConfig);
            await context.EnsureCreatedAsync();

            if (tagBatch.Count > 0)
                await context.TagValues.AddRangeAsync(tagBatch, _cancellationToken);

            if (eventBatch.Count > 0)
                await context.EventLogs.AddRangeAsync(eventBatch, _cancellationToken);

            await context.SaveChangesAsync(_cancellationToken);
            _logger.LogDebug("Сохранено {TagCount} тегов и {EventCount} событий в БД",
                tagBatch.Count, eventBatch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении данных в БД");
        }
    }

    public void Dispose()
    {
        _flushTimer?.Dispose();
    }
}
