using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScadaApp.Data.Entities;
using ScadaApp.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaApp.Data.Repositories;

/// <summary>Реализация репозитория для асинхронного сохранения значений тегов с буферизацией.</summary>
public class TagValueRepository : ITagValueRepository
{
    private readonly DatabaseConfig _dbConfig;
    private readonly ILogger<TagValueRepository> _logger;

    public TagValueRepository(DatabaseConfig dbConfig, ILogger<TagValueRepository> logger)
    {
        _dbConfig = dbConfig;
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task SaveRangeAsync(IEnumerable<TagValueEntity> values, CancellationToken ct = default)
    {
        try
        {
            await using var context = new ScadaDbContext(_dbConfig);
            await context.EnsureCreatedAsync();
            await context.TagValues.AddRangeAsync(values, ct);
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения значений тегов в БД");
        }
    }

    /// <inheritdoc/>
    public async Task SaveEventAsync(EventLogEntity eventLog, CancellationToken ct = default)
    {
        try
        {
            await using var context = new ScadaDbContext(_dbConfig);
            await context.EnsureCreatedAsync();
            await context.EventLogs.AddAsync(eventLog, ct);
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения записи журнала в БД");
        }
    }
}
