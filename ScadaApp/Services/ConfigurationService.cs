using Microsoft.Extensions.Logging;
using ScadaApp.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScadaApp.Services;

/// <summary>Реализация сервиса конфигурации через JSON-файл config.json.</summary>
public class ConfigurationService : IConfigurationService
{
    private static readonly string ConfigPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true
    };

    private readonly ILogger<ConfigurationService> _logger;

    public ConfigurationService(ILogger<ConfigurationService> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc/>
    public async Task<AppConfig> LoadAsync()
    {
        try
        {
            if (!File.Exists(ConfigPath))
            {
                _logger.LogInformation("Файл конфигурации не найден, создаётся конфиг по умолчанию");
                return new AppConfig();
            }

            var json = await File.ReadAllTextAsync(ConfigPath);
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);
            return config ?? new AppConfig();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка чтения конфигурации, используется конфиг по умолчанию");
            return new AppConfig();
        }
    }

    /// <inheritdoc/>
    public async Task SaveAsync(AppConfig config)
    {
        try
        {
            var json = JsonSerializer.Serialize(config, JsonOptions);
            await File.WriteAllTextAsync(ConfigPath, json);
            _logger.LogInformation("Конфигурация сохранена в {Path}", ConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения конфигурации");
        }
    }
}
