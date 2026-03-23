using Microsoft.EntityFrameworkCore;
using ScadaApp.Data.Entities;
using ScadaApp.Models;
using System.Threading.Tasks;

namespace ScadaApp.Data;

/// <summary>EF Core DbContext для хранения значений тегов и журнала событий.</summary>
public class ScadaDbContext : DbContext
{
    private readonly DatabaseConfig _config;

    public ScadaDbContext(DatabaseConfig config)
    {
        _config = config;
    }

    public DbSet<TagValueEntity> TagValues => Set<TagValueEntity>();
    public DbSet<EventLogEntity> EventLogs => Set<EventLogEntity>();

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        if (optionsBuilder.IsConfigured) return;

        switch (_config.DatabaseType)
        {
            case "SqlServer":
                optionsBuilder.UseSqlServer(_config.ConnectionString);
                break;
            default:
                optionsBuilder.UseSqlite(_config.ConnectionString);
                break;
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TagValueEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.TagName).IsRequired().HasMaxLength(256);
            e.Property(x => x.Value).HasMaxLength(512);
            e.Property(x => x.Quality).HasMaxLength(32);
            e.Property(x => x.DeviceName).HasMaxLength(256);
            e.HasIndex(x => x.Timestamp);
            e.HasIndex(x => x.TagName);
        });

        modelBuilder.Entity<EventLogEntity>(e =>
        {
            e.HasKey(x => x.Id);
            e.Property(x => x.Level).HasMaxLength(32);
            e.Property(x => x.Message).HasMaxLength(2048);
            e.Property(x => x.Source).HasMaxLength(256);
            e.HasIndex(x => x.Timestamp);
        });
    }

    /// <summary>Создаёт БД, если она ещё не создана.</summary>
    public async Task EnsureCreatedAsync()
    {
        await Database.EnsureCreatedAsync();
    }
}
