using Microsoft.EntityFrameworkCore;
using ScadaApp.Data.Entities;
using ScadaApp.Models;
using System.Threading.Tasks;

namespace ScadaApp.Data;

/// <summary>
/// Контекст базы данных Entity Framework Core для приложения SCADA.
///
/// Entity Framework Core (EF Core) — ORM (Object-Relational Mapper) библиотека.
/// ORM позволяет работать с базой данных через C#-объекты (классы),
/// не писая SQL-запросы вручную.
///
/// Принцип работы:
///   1. Описываем "сущности" (Entity) — C#-классы, соответствующие таблицам в БД.
///   2. DbContext управляет соединением и операциями с БД.
///   3. DbSet&lt;T&gt; — это "коллекция" записей таблицы. Можно добавлять, искать, удалять.
///   4. SaveChangesAsync() — записывает все изменения в БД одной транзакцией.
///
/// Поддерживаемые СУБД (определяется по DatabaseConfig.DatabaseType):
///   SQLite    — файловая БД, данные в файле .db
///   SqlServer — Microsoft SQL Server
/// </summary>
public class ScadaDbContext : DbContext
{
    /// <summary>
    /// Конфигурация БД (тип СУБД и строка подключения).
    /// Передаётся в конструктор при создании контекста.
    /// </summary>
    private readonly DatabaseConfig _config;

    public ScadaDbContext(DatabaseConfig config)
    {
        _config = config;
    }

    /// <summary>
    /// Набор записей таблицы TagValues (история значений тегов).
    /// => Set&lt;TagValueEntity&gt;() — стандартный способ объявить DbSet&lt;T&gt;.
    /// Использование: context.TagValues.AddRangeAsync(items)
    /// </summary>
    public DbSet<TagValueEntity> TagValues => Set<TagValueEntity>();

    /// <summary>
    /// Набор записей таблицы EventLogs (журнал событий в БД).
    /// </summary>
    public DbSet<EventLogEntity> EventLogs => Set<EventLogEntity>();

    /// <summary>
    /// Настройка подключения к базе данных (вызывается EF Core автоматически).
    /// optionsBuilder.IsConfigured — проверяет, не была ли уже настроена конфигурация.
    /// switch — выбираем провайдер в зависимости от типа СУБД из конфига.
    /// </summary>
    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        switch (_config.DatabaseType)
        {
            case "SqlServer":
                // Используем Microsoft SQL Server с заданной строкой подключения.
                optionsBuilder.UseSqlServer(_config.ConnectionString);
                break;
            default:
                // По умолчанию (включая "SQLite") — файловая БД SQLite.
                optionsBuilder.UseSqlite(_config.ConnectionString);
                break;
        }
    }

    /// <summary>
    /// Конфигурация модели данных (структуры таблиц).
    /// Вызывается EF Core при первом создании контекста.
    ///
    /// modelBuilder — построитель модели. Через него задаём:
    ///   - Первичные ключи (PK)
    ///   - Ограничения длины строк (HasMaxLength)
    ///   - Индексы для ускорения запросов (HasIndex)
    ///   - Обязательные поля (IsRequired)
    /// </summary>
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // ── Таблица TagValues ────────────────────────────────────────────────
        modelBuilder.Entity<TagValueEntity>(e =>
        {
            // Первичный ключ — поле Id (тип long, автоинкремент).
            e.HasKey(x => x.Id);

            // Ограничения длины строк — предотвращают хранение слишком длинных данных.
            e.Property(x => x.TagName).IsRequired().HasMaxLength(256);  // Обязательное, макс. 256 символов
            e.Property(x => x.Value).HasMaxLength(512);                  // Макс. 512 символов
            e.Property(x => x.Quality).HasMaxLength(32);                 // "Good"/"Bad" — короткие
            e.Property(x => x.DeviceName).HasMaxLength(256);

            // Индексы ускоряют поиск по этим полям.
            // Поиск по времени нужен для запросов истории за период.
            e.HasIndex(x => x.Timestamp);
            // Поиск по имени тега нужен для фильтрации конкретного тега.
            e.HasIndex(x => x.TagName);
        });

        // ── Таблица EventLogs ────────────────────────────────────────────────
        modelBuilder.Entity<EventLogEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Level).HasMaxLength(32);
            e.Property(x => x.Message).HasMaxLength(2048);   // Сообщения могут быть длинными
            e.Property(x => x.Source).HasMaxLength(256);
            e.HasIndex(x => x.Timestamp);
        });
    }

    /// <summary>
    /// Создаёт базу данных и все таблицы, если они ещё не существуют.
    /// Используется при первом запуске и при проверке подключения.
    ///
    /// EnsureCreated() — в отличие от Migrate(), просто создаёт схему.
    /// Не поддерживает миграции (обновление структуры существующей БД).
    /// </summary>
    public async Task EnsureCreatedAsync()
    {
        await Database.EnsureCreatedAsync();
    }
}
