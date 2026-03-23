using ScadaApp.Models;
using System.Threading.Tasks;

namespace ScadaApp.Services;

/// <summary>Сервис загрузки и сохранения конфигурации приложения.</summary>
public interface IConfigurationService
{
    /// <summary>Загрузить конфигурацию из файла.</summary>
    Task<AppConfig> LoadAsync();

    /// <summary>Сохранить конфигурацию в файл.</summary>
    Task SaveAsync(AppConfig config);
}
