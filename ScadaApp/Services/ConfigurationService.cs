using Microsoft.Extensions.Logging;
using ScadaApp.Models;
using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;

namespace ScadaApp.Services;

/// <summary>
/// Реализация сервиса конфигурации через JSON-файл config.json.
/// Загружает конфигурацию при старте приложения и сохраняет её при любом изменении настроек.
///
/// JSON (JavaScript Object Notation) — текстовый формат хранения данных.
/// Файл config.json лежит в папке рядом с исполняемым файлом (.exe).
/// </summary>
public class ConfigurationService : IConfigurationService
{
    /// <summary>
    /// Полный путь к файлу конфигурации.
    /// AppDomain.CurrentDomain.BaseDirectory — папка, где находится .exe файл.
    /// Path.Combine объединяет путь к папке и имя файла в полный путь.
    /// static readonly — константа класса, вычисляется один раз при запуске.
    /// </summary>
    private static readonly string ConfigPath =
        Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.json");

    /// <summary>
    /// Параметры JSON-сериализации.
    /// WriteIndented = true — форматирует JSON с отступами (читаемый вид).
    /// </summary>
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
    /// <summary>
    /// Асинхронно загружает конфигурацию из файла config.json.
    /// Если файл не найден или произошла ошибка — возвращает пустой конфиг по умолчанию.
    ///
    /// async Task&lt;AppConfig&gt; — метод возвращает AppConfig, но асинхронно
    /// (не блокирует UI пока читает файл с диска).
    /// await — приостанавливает выполнение до завершения операции чтения файла.
    /// </summary>
    public async Task<AppConfig> LoadAsync()
    {
        try
        {
            // Проверяем, существует ли файл конфигурации.
            if (!File.Exists(ConfigPath))
            {
                _logger.LogInformation("Файл конфигурации не найден, создаётся конфиг по умолчанию");
                return new AppConfig(); // Возвращаем пустой конфиг
            }

            // Читаем весь текст файла в строку (асинхронно).
            var json = await File.ReadAllTextAsync(ConfigPath);

            // JsonSerializer.Deserialize&lt;T&gt;() преобразует JSON-строку в объект C#.
            // Знак "?" у типа — результат может быть null (если JSON пустой).
            var config = JsonSerializer.Deserialize<AppConfig>(json, JsonOptions);

            // Если десериализация вернула null — возвращаем пустой конфиг.
            return config ?? new AppConfig();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка чтения конфигурации, используется конфиг по умолчанию");
            return new AppConfig();
        }
    }

    /// <inheritdoc/>
    /// <summary>
    /// Асинхронно сохраняет конфигурацию в файл config.json.
    /// При любой ошибке записывает её в лог (файл не перезаписывается частично).
    /// </summary>
    public async Task SaveAsync(AppConfig config)
    {
        try
        {
            // JsonSerializer.Serialize() преобразует объект C# в JSON-строку.
            var json = JsonSerializer.Serialize(config, JsonOptions);

            // Записываем строку в файл (создаёт файл или перезаписывает существующий).
            await File.WriteAllTextAsync(ConfigPath, json);

            _logger.LogInformation("Конфигурация сохранена в {Path}", ConfigPath);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Ошибка сохранения конфигурации");
        }
    }
}
