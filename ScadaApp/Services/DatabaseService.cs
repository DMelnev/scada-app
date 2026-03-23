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

/// <summary>
/// Сервис буферизованного сохранения значений тегов в базу данных.
///
/// Реализует три интерфейса:
///   IHostedService  — управление жизненным циклом (запуск/остановка)
///   IDatabaseService — публичный интерфейс для добавления записей в очередь
///   IDisposable      — освобождение ресурсов (таймера)
///
/// Принцип работы:
///   1. Подписывается на событие TagValueChanged от PollingService
///   2. При каждом новом значении — добавляет его в очередь (ConcurrentQueue)
///   3. Таймер каждые SaveIntervalMs миллисекунд — сбрасывает очередь в БД
///   4. Если очередь превысила BufferSize — сброс происходит немедленно
///
/// Буферизация нужна для производительности: запись каждого значения по
/// отдельности была бы очень медленной. Пакетная запись намного эффективнее.
/// </summary>
public class DatabaseService : IHostedService, IDatabaseService, IDisposable
{
    private readonly PollingService _pollingService;
    private readonly DatabaseConfig _dbConfig;
    private readonly ILogger<DatabaseService> _logger;

    /// <summary>
    /// Потокобезопасная очередь значений тегов ожидающих записи в БД.
    /// ConcurrentQueue — аналог обычной Queue, но безопасен для использования
    /// из нескольких потоков одновременно без блокировок.
    /// </summary>
    private readonly ConcurrentQueue<TagValueEntity> _tagQueue = new();

    /// <summary>Потокобезопасная очередь событий журнала ожидающих записи в БД.</summary>
    private readonly ConcurrentQueue<EventLogEntity> _eventQueue = new();

    /// <summary>
    /// Таймер для периодического сброса буфера в БД.
    /// System.Threading.Timer — таймер, выполняющийся в пуле потоков.
    /// </summary>
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

    /// <summary>
    /// Добавляет значение тега в очередь на запись.
    /// Вызывается из обработчика события TagValueChanged.
    /// Метод быстрый — не блокирует вызывающий поток.
    /// </summary>
    public void Enqueue(TagValueEntity value)
    {
        _tagQueue.Enqueue(value);
    }

    /// <summary>
    /// Добавляет запись журнала события в очередь на запись.
    /// </summary>
    public void EnqueueEvent(EventLogEntity eventLog)
    {
        _eventQueue.Enqueue(eventLog);
    }

    /// <summary>
    /// Запускает сервис записи в БД (IHostedService.StartAsync).
    /// Если запись в БД отключена — ничего не делает и выходит.
    /// </summary>
    public Task StartAsync(CancellationToken cancellationToken)
    {
        if (!_dbConfig.Enabled)
        {
            _logger.LogInformation("Сохранение в БД отключено");
            return Task.CompletedTask; // Task.CompletedTask — уже завершённая задача (ничего не делаем)
        }

        _cancellationToken = cancellationToken;

        // Подписываемся на события от сервиса опроса.
        _pollingService.TagValueChanged += OnTagValueChanged;

        // Создаём таймер для периодического сброса буфера.
        // Первый аргумент (callback): метод, который вызывается по таймеру
        // Второй аргумент (state): передаётся в callback (не используем → null)
        // Третий аргумент (dueTime): задержка перед первым срабатыванием
        // Четвёртый аргумент (period): интервал между срабатываниями
        _flushTimer = new Timer(
            async _ => await FlushBufferAsync(),
            null,
            TimeSpan.FromMilliseconds(_dbConfig.SaveIntervalMs),
            TimeSpan.FromMilliseconds(_dbConfig.SaveIntervalMs));

        _logger.LogInformation("Сервис БД запущен (интервал: {Interval}ms)", _dbConfig.SaveIntervalMs);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Останавливает сервис записи в БД (IHostedService.StopAsync).
    /// Останавливает таймер и записывает оставшиеся данные из буфера.
    /// </summary>
    public async Task StopAsync(CancellationToken cancellationToken)
    {
        _pollingService.TagValueChanged -= OnTagValueChanged; // Отписываемся от события

        // Timeout.Infinite — останавливаем таймер (больше не срабатывает).
        _flushTimer?.Change(Timeout.Infinite, 0);

        // Последний сброс буфера перед остановкой — чтобы не потерять данные.
        await FlushBufferAsync();

        _logger.LogInformation("Сервис БД остановлен");
    }

    /// <summary>
    /// Обработчик события TagValueChanged от PollingService.
    /// Создаёт сущность БД из аргументов события и добавляет в очередь.
    ///
    /// Если в очереди накопилось больше BufferSize элементов — немедленно
    /// запускаем сброс в БД (без ожидания таймера).
    /// </summary>
    private void OnTagValueChanged(object? sender, TagValueChangedEventArgs e)
    {
        // Создаём объект TagValueEntity (строка таблицы TagValues в БД).
        var entity = new TagValueEntity
        {
            TagName = e.TagName,
            Value = e.Value?.ToString() ?? "", // null → пустая строка
            Timestamp = e.Timestamp,
            Quality = e.Quality,
            DeviceName = e.DeviceName
        };
        Enqueue(entity);

        // Если буфер переполнен — сбрасываем немедленно.
        if (_tagQueue.Count >= _dbConfig.BufferSize)
        {
            // "_" = fire-and-forget: запускаем задачу не ожидая её завершения.
            // Это нормально для записи в БД — мы не можем блокировать поток опроса.
            _ = FlushBufferAsync();
        }
    }

    /// <summary>
    /// Записывает все накопленные данные из очередей в базу данных.
    /// Выгребает все элементы из обеих очередей и выполняет пакетную вставку.
    ///
    /// "await using var context" — создаёт контекст БД и гарантирует его
    /// закрытие после выхода из блока (даже при ошибке).
    /// </summary>
    private async Task FlushBufferAsync()
    {
        // Выгребаем все элементы из очередей в обычные List<>.
        var tagBatch = new List<TagValueEntity>();
        while (_tagQueue.TryDequeue(out var item))
            tagBatch.Add(item);

        var eventBatch = new List<EventLogEntity>();
        while (_eventQueue.TryDequeue(out var ev))
            eventBatch.Add(ev);

        // Если нечего записывать — выходим.
        if (tagBatch.Count == 0 && eventBatch.Count == 0) return;

        try
        {
            // Создаём контекст базы данных. "await using" гарантирует закрытие соединения.
            await using var context = new ScadaDbContext(_dbConfig);

            // Создаём таблицы если их ещё нет.
            await context.EnsureCreatedAsync();

            // Добавляем данные в контекст EF Core (в память, ещё не в БД).
            if (tagBatch.Count > 0)
                await context.TagValues.AddRangeAsync(tagBatch, _cancellationToken);

            if (eventBatch.Count > 0)
                await context.EventLogs.AddRangeAsync(eventBatch, _cancellationToken);

            // Сохраняем все изменения в БД одной транзакцией.
            await context.SaveChangesAsync(_cancellationToken);

            _logger.LogDebug("Сохранено {TagCount} тегов и {EventCount} событий в БД",
                tagBatch.Count, eventBatch.Count);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка при сохранении данных в БД");
        }
    }

    /// <summary>
    /// Освобождает ресурсы (IDisposable).
    /// Таймер — неуправляемый ресурс, его нужно явно удалить.
    /// </summary>
    public void Dispose()
    {
        _flushTimer?.Dispose();
    }
}
