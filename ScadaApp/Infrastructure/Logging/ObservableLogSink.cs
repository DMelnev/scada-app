using Serilog.Core;
using Serilog.Events;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Threading;

namespace ScadaApp.Infrastructure.Logging;

/// <summary>Запись журнала для отображения в UI.</summary>
public class LogEntry
{
    public DateTime Timestamp { get; set; }
    public string Level { get; set; } = "";
    public string Message { get; set; } = "";
}

/// <summary>Serilog Sink, пишущий записи в ObservableCollection для отображения в UI.</summary>
public class ObservableLogSink : ILogEventSink
{
    private const int MaxEntries = 1000;

    public ObservableCollection<LogEntry> LogEntries { get; } = new();

    private readonly Dispatcher _dispatcher;

    public ObservableLogSink(Dispatcher dispatcher)
    {
        _dispatcher = dispatcher;
    }

    /// <inheritdoc/>
    public void Emit(LogEvent logEvent)
    {
        var entry = new LogEntry
        {
            Timestamp = logEvent.Timestamp.LocalDateTime,
            Level = logEvent.Level.ToString(),
            Message = logEvent.RenderMessage()
        };

        if (_dispatcher.CheckAccess())
        {
            AddEntry(entry);
        }
        else
        {
            _dispatcher.BeginInvoke(() => AddEntry(entry));
        }
    }

    private void AddEntry(LogEntry entry)
    {
        LogEntries.Add(entry);
        while (LogEntries.Count > MaxEntries)
            LogEntries.RemoveAt(0);
    }
}
