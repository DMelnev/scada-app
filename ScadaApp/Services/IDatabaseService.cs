using ScadaApp.Data.Entities;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ScadaApp.Services;

/// <summary>Сервис сохранения данных тегов и событий в базу данных.</summary>
public interface IDatabaseService
{
    /// <summary>Поставить значение тега в очередь на сохранение.</summary>
    void Enqueue(TagValueEntity value);

    /// <summary>Поставить событие в очередь на сохранение.</summary>
    void EnqueueEvent(EventLogEntity eventLog);
}
