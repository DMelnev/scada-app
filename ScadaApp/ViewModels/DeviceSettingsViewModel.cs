using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScadaApp.Models;
using ScadaApp.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ScadaApp.ViewModels;

/// <summary>
/// ViewModel для окна "Настройка устройств" (DeviceSettingsWindow).
/// Управляет списком устройств и формой редактирования выбранного устройства.
///
/// Паттерн: пользователь выбирает устройство в списке слева,
/// редактирует его свойства в форме справа,
/// нажимает "Сохранить" — изменения сохраняются в конфиг.
/// </summary>
public partial class DeviceSettingsViewModel : ObservableObject
{
    // Сервис конфигурации для сохранения изменений.
    private readonly IConfigurationService _configService;

    /// <summary>
    /// Список всех устройств, отображаемый в ListBox слева.
    /// ObservableCollection автоматически уведомляет UI при добавлении/удалении.
    /// Это рабочая копия списка — изменения применяются только при нажатии "Сохранить".
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DeviceConfig> _devices = new();

    /// <summary>
    /// Выбранное в ListBox устройство.
    /// При выборе устройства форма справа показывает его свойства.
    /// Знак "?" — может быть null (если ни одно устройство не выбрано).
    /// </summary>
    [ObservableProperty]
    private DeviceConfig? _selectedDevice;

    /// <summary>
    /// Ссылка на корневой конфиг приложения.
    /// Используется при загрузке (читаем Devices) и при сохранении (пишем Devices).
    /// Хранится как поле, а не свойство, чтобы не отображаться в UI.
    /// </summary>
    private AppConfig? _config;

    /// <summary>
    /// Конструктор получает сервис конфигурации через DI.
    /// </summary>
    public DeviceSettingsViewModel(IConfigurationService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// Загружает список устройств из конфига в ObservableCollection для редактирования.
    /// Создаётся НОВАЯ коллекция (не ссылка на config.Devices), чтобы изменения
    /// не применялись автоматически — только при нажатии "Сохранить".
    /// </summary>
    public async Task LoadAsync(AppConfig config)
    {
        _config = config;
        // new ObservableCollection<>(config.Devices) создаёт копию списка.
        Devices = new ObservableCollection<DeviceConfig>(config.Devices);
    }

    /// <summary>
    /// Команда добавления нового устройства.
    /// Создаёт новый DeviceConfig с именем по умолчанию и добавляет в список.
    /// Автоматически выбирает новое устройство, чтобы пользователь сразу видел форму.
    /// </summary>
    [RelayCommand]
    private void AddDevice()
    {
        var device = new DeviceConfig { Name = "Новое устройство" };
        Devices.Add(device);
        SelectedDevice = device; // Выбираем созданное устройство в списке
    }

    /// <summary>
    /// Команда удаления выбранного устройства из списка.
    /// Проверяем, что SelectedDevice != null (иначе нечего удалять).
    /// После удаления сбрасываем выбор (SelectedDevice = null).
    /// </summary>
    [RelayCommand]
    private void RemoveDevice()
    {
        if (SelectedDevice == null) return;
        Devices.Remove(SelectedDevice);
        SelectedDevice = null;
    }

    /// <summary>
    /// Команда сохранения изменений.
    /// Применяет рабочую коллекцию Devices к основному конфигу (_config.Devices).
    /// После этого главное окно получит DialogResult = true и сохранит конфиг в файл.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (_config == null) return;
        // Очищаем список в конфиге и заполняем его из нашей рабочей коллекции.
        _config.Devices.Clear();
        _config.Devices.AddRange(Devices);
    }

    /// <summary>
    /// Команда отмены изменений.
    /// Восстанавливает рабочую коллекцию из конфига, отбрасывая все несохранённые правки.
    /// </summary>
    [RelayCommand]
    private void Cancel()
    {
        if (_config != null)
            Devices = new ObservableCollection<DeviceConfig>(_config.Devices);
    }
}
