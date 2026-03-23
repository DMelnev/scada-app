using System;

namespace ScadaApp.Data.Entities;

/// <summary>
/// Сущность (Entity) для хранения одного значения тега в базе данных.
/// Соответствует одной строке таблицы "TagValues".
///
/// Сущность в EF Core — это обычный C#-класс (POCO).
/// EF Core автоматически создаёт таблицу в БД на основе структуры этого класса.
///
/// Каждый раз, когда PollingService успешно читает значение тега,
/// создаётся один экземпляр TagValueEntity и добавляется в буфер DatabaseService.
/// </summary>
public class TagValueEntity
{
    /// <summary>
    /// Первичный ключ (уникальный идентификатор записи в БД).
    /// long — 64-битное целое число (позволяет хранить миллиарды записей).
    /// EF Core автоматически назначает значение при вставке (автоинкремент).
    /// </summary>
    public long Id { get; set; }

    /// <summary>
    /// Имя тега. Соответствует полю Name в TagConfig.
    /// Используется для поиска истории конкретного тега.
    /// Максимальная длина: 256 символов (задано в ScadaDbContext.OnModelCreating).
    /// </summary>
    public string TagName { get; set; } = string.Empty;

    /// <summary>
    /// Значение тега в виде строки.
    /// Конвертируется из object?.ToString() в DatabaseService.OnTagValueChanged().
    /// Числа хранятся как строки: "23.5", "1024", "True".
    /// Максимальная длина: 512 символов.
    /// </summary>
    public string Value { get; set; } = string.Empty;

    /// <summary>
    /// Момент времени чтения значения (UTC).
    /// Хранится в UTC, конвертируется в локальное время при отображении.
    /// Индексируется для быстрого поиска по диапазону дат.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Качество значения:
    /// "Good"      — успешное чтение
    /// "Bad"       — ошибка чтения
    /// "Uncertain" — сомнительное качество
    /// Максимальная длина: 32 символа.
    /// </summary>
    public string Quality { get; set; } = "Good";

    /// <summary>
    /// Имя устройства (ПЛК), от которого получено значение.
    /// Полезно для фильтрации данных по устройству.
    /// Максимальная длина: 256 символов.
    /// </summary>
    public string DeviceName { get; set; } = "";
}