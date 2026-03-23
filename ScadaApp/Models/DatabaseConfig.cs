namespace ScadaApp.Models;

/// <summary>
/// Конфигурация подключения к базе данных.
/// Используется сервисом DatabaseService для хранения истории значений тегов
/// и записей журнала событий.
///
/// Поддерживаются два типа СУБД:
///   SQLite    — локальный файл, не требует сервера (подходит для разработки и небольших установок)
///   SqlServer — Microsoft SQL Server (для промышленных установок с многопользовательским доступом)
/// </summary>
public class DatabaseConfig
{
    /// <summary>
    /// Включена ли запись в базу данных.
    /// false — сервис DatabaseService не запускается, данные не сохраняются.
    /// true  — сервис запускается и периодически записывает значения тегов.
    /// По умолчанию: false (запись выключена).
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Тип системы управления базой данных.
    /// "SQLite"    — файловая база данных, данные хранятся в одном .db файле.
    /// "SqlServer" — Microsoft SQL Server, требует установленного сервера.
    /// По умолчанию: "SQLite".
    /// </summary>
    public string DatabaseType { get; set; } = "SQLite";

    /// <summary>
    /// Строка подключения к базе данных.
    /// Для SQLite:    "Data Source=scada.db"  (файл в папке приложения)
    ///                "Data Source=C:\Data\scada.db"  (полный путь)
    /// Для SqlServer: "Server=myserver;Database=ScadaDB;Trusted_Connection=True;"
    ///                "Server=192.168.0.100;Database=ScadaDB;User Id=sa;Password=pass;"
    /// По умолчанию: "Data Source=scada.db"
    /// </summary>
    public string ConnectionString { get; set; } = "Data Source=scada.db";

    /// <summary>
    /// Интервал между сохранениями буфера в базу данных (в миллисекундах).
    /// Значения тегов накапливаются в памяти и периодически записываются в БД.
    /// По умолчанию: 5000 (каждые 5 секунд).
    /// Уменьшение значения увеличивает нагрузку на диск/сервер.
    /// </summary>
    public int SaveIntervalMs { get; set; } = 5000;

    /// <summary>
    /// Максимальное количество значений тегов в буфере памяти.
    /// Если накопилось столько значений — они записываются немедленно,
    /// не дожидаясь следующего срабатывания таймера SaveIntervalMs.
    /// По умолчанию: 100.
    /// </summary>
    public int BufferSize { get; set; } = 100;
}
