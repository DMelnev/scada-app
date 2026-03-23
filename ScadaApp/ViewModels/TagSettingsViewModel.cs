using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScadaApp.Models;
using System.Collections.ObjectModel;

namespace ScadaApp.ViewModels;

/// <summary>
/// ViewModel для окна "Настройка тегов" (TagSettingsWindow).
/// Управляет выбором устройства и редактированием его тегов.
///
/// Логика работы:
/// 1. Пользователь выбирает устройство из выпадающего списка
/// 2. В таблице появляются теги этого устройства (копия, не оригинал)
/// 3. Пользователь редактирует теги прямо в таблице
/// 4. При нажатии "Сохранить" — теги записываются обратно в устройство
/// </summary>
public partial class TagSettingsViewModel : ObservableObject
{
    /// <summary>
    /// Список устройств для выпадающего списка (ComboBox).
    /// Заполняется методом LoadDevices() из конфига приложения.
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<DeviceConfig> _devices = new();

    /// <summary>
    /// Выбранное устройство в ComboBox.
    /// При изменении — автоматически вызывается OnSelectedDeviceChanged()
    /// (partial-метод, генерируемый CommunityToolkit.Mvvm).
    /// </summary>
    [ObservableProperty]
    private DeviceConfig? _selectedDevice;

    /// <summary>
    /// Список тегов выбранного устройства для редактирования в DataGrid.
    /// Это рабочая копия тегов: изменения применяются только при нажатии "Сохранить".
    /// </summary>
    [ObservableProperty]
    private ObservableCollection<TagConfig> _tags = new();

    /// <summary>
    /// Метод, вызываемый автоматически при изменении свойства SelectedDevice.
    /// "partial" — часть автоматически генерируемого кода от [ObservableProperty].
    /// Когда пользователь выбирает другое устройство — загружаем его теги.
    /// </summary>
    partial void OnSelectedDeviceChanged(DeviceConfig? value)
    {
        // Если устройство выбрано — загружаем его теги в рабочую коллекцию.
        // Если нет (null) — показываем пустую коллекцию.
        Tags = value != null
            ? new ObservableCollection<TagConfig>(value.Tags)
            : new ObservableCollection<TagConfig>();
    }

    /// <summary>
    /// Загружает список устройств из конфига в коллекцию для ComboBox.
    /// IEnumerable&lt;T&gt; — базовый интерфейс для любой коллекции (List, массив и т.д.).
    /// </summary>
    public void LoadDevices(System.Collections.Generic.IEnumerable<DeviceConfig> devices)
    {
        Devices = new ObservableCollection<DeviceConfig>(devices);
    }

    /// <summary>
    /// Команда добавления нового тега.
    /// Создаёт тег с именем "NewTag" и добавляет в рабочую коллекцию Tags.
    /// Требует, чтобы было выбрано устройство (иначе нет смысла добавлять тег).
    /// </summary>
    [RelayCommand]
    private void AddTag()
    {
        if (SelectedDevice == null) return;
        var tag = new TagConfig { Name = "NewTag" };
        Tags.Add(tag);
    }

    /// <summary>
    /// Команда удаления тега из таблицы.
    /// Принимает тег как параметр (он передаётся из кнопки в строке таблицы).
    /// Знак "?" — параметр может быть null (защита от случайного вызова).
    /// </summary>
    [RelayCommand]
    private void RemoveTag(TagConfig? tag)
    {
        if (tag != null) Tags.Remove(tag);
    }

    /// <summary>
    /// Команда сохранения изменений тегов.
    /// Записывает рабочую коллекцию Tags обратно в выбранное устройство.
    /// После этого главное окно получит DialogResult = true и сохранит конфиг.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (SelectedDevice == null) return;
        // Очищаем теги устройства и заполняем из нашей рабочей коллекции.
        SelectedDevice.Tags.Clear();
        SelectedDevice.Tags.AddRange(Tags);
    }
}
