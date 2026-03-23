using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using ScadaApp.Data.Entities;
using ScadaApp.Models;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaApp.Data.Repositories;

/// <summary>
/// Реализация репозитория для асинхронного сохранения значений тегов в базу данных.
///
/// Паттерн "Репозиторий" (Repository Pattern):
///   Абстрагирует доступ к данным за интерфейсом ITagValueRepository.
///   Вызывающий код не знает про EF Core, SQL, тип СУБД — он просто вызывает:
///     repository.SaveRangeAsync(values)
///
/// В данном приложении репозиторий регистрируется как Transient — каждый раз
/// создаётся новый экземпляр. Это важно, так как ScadaDbContext создаётся внутри
/// методов и сразу закрывается (await using).
///
/// Примечание: в данный момент репозиторий НЕ используется напрямую — 
/// DatabaseService создаёт контекст самостоятельно. Репозиторий подготовлен
/// для будущего использования.
/// </summary>
public class TagValueRepository : ITagValueRepository
{
    private readonly DatabaseConfig _dbConfig;
    private readonly ILogger<TagValueRepository> _logger;

    public TagValueRepository(DatabaseConfig dbConfig, ILogger<TagValueRepository> logger)
    {
        _dbConfig = dbConfig;
        _logger = logger;
    }

    /// <summary>
    /// Асинхронно сохраняет список значений тегов в базу данных.
    ///
    /// IEnumerable&lt;TagValueEntity&gt; — принимает любую коллекцию (List, массив и т.д.).
    /// CancellationToken ct = default — необязательный параметр отмены.
    ///
    /// "await using var context" — создаёт контекст и гарантирует его закрытие
    /// после выхода из блока (даже при исключении).
    /// </summary>
    public async Task SaveRangeAsync(IEnumerable<TagValueEntity> values, CancellationToken ct = default)
    {
        try
        {
            // Создаём новый DbContext для этого запроса.
            await using var context = new ScadaDbContext(_dbConfig);

            // Создаём таблицы если нужно.
            await context.EnsureCreatedAsync();

            // Добавляем все сущности в контекст (в память EF Core, ещё не в БД).
            await context.TagValues.AddRangeAsync(values, ct);

            // Записываем все изменения в БД одной транзакцией.
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения значений тегов в БД");
        }
    }

    /// <summary>
    /// Асинхронно сохраняет одну запись журнала событий в базу данных.
    /// </summary>
    public async Task SaveEventAsync(EventLogEntity eventLog, CancellationToken ct = default)
    {
        try
        {
            await using var context = new ScadaDbContext(_dbConfig);
            await context.EnsureCreatedAsync();

            // AddAsync — добавляем один элемент (в отличие от AddRangeAsync).
            await context.EventLogs.AddAsync(eventLog, ct);
            await context.SaveChangesAsync(ct);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения записи журнала в БД");
        }
    }
}
