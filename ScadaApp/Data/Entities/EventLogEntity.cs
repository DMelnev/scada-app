using System;

namespace ScadaApp.Data.Entities;

/// <summary>
/// Сущность для хранения записи журнала событий в базе данных.
/// Соответствует одной строке таблицы "EventLogs".
///
/// Используется для долгосрочного хранения важных событий приложения в БД.
/// В отличие от ObservableLogSink (хранит 1000 записей только в памяти),
/// таблица EventLogs позволяет хранить историю событий постоянно.
///
/// Примечание: в текущей версии приложения эта таблица создаётся,
/// но записи добавляются только через метод DatabaseService.EnqueueEvent(),
/// который пока не вызывается автоматически.
/// </summary>
public class EventLogEntity
{
    /// <summary>
    /// Первичный ключ (уникальный идентификатор записи).
    /// Автоинкремент — EF Core автоматически назначает значение.
    /// </summary>
    public long Id { get; set; }

    /// <summary>Дата и время события (UTC).</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Уровень важности события.
    /// Возможные значения: "Info", "Warning", "Error", "Debug".
    /// Максимальная длина: 32 символа.
    /// </summary>
    public string Level { get; set; } = "Info";

    /// <summary>
    /// Текст сообщения события.
    /// Максимальная длина: 2048 символов (достаточно для большинства сообщений).
    /// </summary>
    public string Message { get; set; } = "";

    /// <summary>
    /// Источник события (имя класса или компонента, создавшего запись).
    /// Например: "PollingService", "DatabaseService", "SiemensDriver".
    /// Максимальная длина: 256 символов.
    /// </summary>
    public string Source { get; set; } = "";
}
