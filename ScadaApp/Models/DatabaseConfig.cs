namespace ScadaApp.Models;

/// <summary>Тип БД, строка подключения, интервал сохранения, включено/выкл.</summary>
public class DatabaseConfig
{
    public bool Enabled { get; set; } = false;

    /// <summary>Тип БД: "SQLite", "SqlServer".</summary>
    public string DatabaseType { get; set; } = "SQLite";

    public string ConnectionString { get; set; } = "Data Source=scada.db";
    public int SaveIntervalMs { get; set; } = 5000;
    public int BufferSize { get; set; } = 100;
}
