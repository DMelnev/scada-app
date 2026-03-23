using ScadaApp.Data.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaApp.Data.Repositories;

/// <summary>Репозиторий для асинхронного сохранения значений тегов.</summary>
public interface ITagValueRepository
{
    /// <summary>Сохранить список значений тегов в БД.</summary>
    Task SaveRangeAsync(IEnumerable<TagValueEntity> values, CancellationToken ct = default);

    /// <summary>Сохранить запись журнала событий.</summary>
    Task SaveEventAsync(EventLogEntity eventLog, CancellationToken ct = default);
}
