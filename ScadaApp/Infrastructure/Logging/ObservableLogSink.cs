using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace ScadaApp.Infrastructure.Logging;

/// <summary>
/// Модель одной записи в журнале событий.
/// Используется для привязки данных в DataGrid журнала главного окна.
/// Это простой POCO-класс (Plain Old CLR Object) — только данные, без логики.
/// </summary>
public class LogEntry
{
    /// <summary>Дата и время события.</summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Уровень важности: "Verbose", "Debug", "Information", "Warning", "Error", "Fatal".
    /// Отображается в колонке "Уровень" таблицы журнала.
    /// </summary>
    public string Level { get; set; } = "";

    /// <summary>Текст сообщения события.</summary>
    public string Message { get; set; } = "";
}

/// <summary>
/// Serilog Sink (приёмник), который добавляет записи логирования
/// в ObservableCollection для отображения в DataGrid главного окна.
///
/// Serilog Sink — это "место назначения" для лог-сообщений.
/// Стандартные sink'и: файл, консоль, база данных.
/// ObservableLogSink — наш кастомный sink для WPF UI.
///
/// Реализует интерфейс ILogEventSink из Serilog.Core —
/// нужно только реализовать метод Emit(LogEvent).
///
/// ВАЖНО: Serilog может вызывать Emit() из любого потока!
/// Поэтому все изменения ObservableCollection выполняются через Dispatcher —
/// планировщик задач UI-потока WPF.
/// </summary>
public class ObservableLogSink : ILogEventSink
{
    /// <summary>
    /// Максимальное количество записей в журнале UI.
    /// Старые записи удаляются при превышении лимита.
    /// const — константа, не меняется во время выполнения.
    /// </summary>
    private const int MaxEntries = 1000;

    /// <summary>
    /// Коллекция записей журнала для привязки к DataGrid.
    /// ObservableCollection — автоматически уведомляет UI при добавлении/удалении.
    /// Привязывается в XAML: ItemsSource="{Binding LogEntries}"
    /// </summary>
    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    /// <summary>
    /// Dispatcher UI-потока.
    /// Dispatcher.BeginInvoke() — ставит задачу в очередь UI-потока.
    /// Все изменения UI в WPF должны выполняться только в UI-потоке.
    /// </summary>
    private readonly Dispatcher _dispatcher;

    public ObservableLogSink(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <summary>
    /// Метод, вызываемый Serilog при каждом новом лог-сообщении.
    /// Создаёт LogEntry и добавляет в коллекцию через Dispatcher.
    ///
    /// logEvent.RenderMessage() — форматирует шаблон сообщения с подстановкой значений.
    /// Например: "Конфигурация загружена ({DeviceCount} устройств)" → "Конфигурация загружена (2 устройств)"
    /// </summary>
    public void Emit(LogEvent logEvent)
    {
        var entry = new LogEntry
        {
            // LocalDateTime — конвертируем из DateTimeOffset в локальное DateTime.
            Timestamp = logEvent.Timestamp.LocalDateTime,
            // .ToString() — возвращает строковое представление уровня: "Information" и т.д.
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage()
        };

        // Проверяем, выполняемся ли мы уже в UI-потоке.
        if (_dispatcher.CheckAccess())
        {
            // Уже в UI-потоке — добавляем напрямую.
            AddEntry(entry);
        }
        else
        {
            // В другом потоке — ставим в очередь UI-потока.
            // BeginInvoke (асинхронно) — не блокирует вызывающий поток.
            _dispatcher.BeginInvoke(() => AddEntry(entry));
        }
    }

    /// <summary>
    /// Добавляет запись в коллекцию и удаляет старые при превышении лимита.
    /// Всегда вызывается из UI-потока.
    /// </summary>
    private void AddEntry(LogEntry entry)
    {
        LogEntries.Add(entry);

        // Удаляем самую старую запись (индекс 0), пока превышен лимит.
        while (LogEntries.Count > MaxEntries)
            LogEntries.RemoveAt(0);
    }
}
