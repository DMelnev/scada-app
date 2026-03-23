using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScadaApp.Models;
using System.Collections.ObjectModel;

namespace ScadaApp.ViewModels;

/// <summary>
/// ViewModel для окна "Настройка 3D-визуализации" (VisualizationSettingsWindow).
/// Управляет настройками подключения к 3D-сервису и таблицей маппинга тегов на объекты.
///
/// Маппинг — это связь между именем тега и именем 3D-объекта + его свойством.
/// Например: тег "Температура_1" → объект "Термометр_3D" → свойство "PositionY".
/// </summary>
public partial class VisualizationSettingsViewModel : ObservableObject
{
    /// <summary>
    /// Включён ли сервис 3D-визуализации.
    /// Привязывается к CheckBox "Включить 3D-визуализацию".
    /// </summary>
    [ObservableProperty] private bool _enabled;

    /// <summary>
    /// URL WebSocket-сервера 3D-визуализации.
    /// Привязывается к TextBox "URL сервиса".
    /// По умолчанию: "ws://localhost:8080".
    /// </summary>
    [ObservableProperty] private string _serviceUrl = "ws://localhost:8080";

    /// <summary>
    /// Список маппингов тег → объект/свойство для редактирования в DataGrid.
    /// Это рабочая копия — изменения сохраняются только при нажатии "Сохранить".
    /// </summary>
    [ObservableProperty] private ObservableCollection<TagVisualizationMapping> _mappings = new();

    /// <summary>
    /// Выбранный маппинг в DataGrid.
    /// Используется для команды удаления: удаляется именно выбранный элемент.
    /// </summary>
    [ObservableProperty] private TagVisualizationMapping? _selectedMapping;

    /// <summary>Ссылка на оригинальный конфиг для сохранения изменений.</summary>
    private VisualizationConfig? _config;

    /// <summary>
    /// Загружает настройки из VisualizationConfig в свойства ViewModel.
    /// Создаёт копии коллекций, чтобы изменения не применялись немедленно.
    /// </summary>
    public void Load(VisualizationConfig config)
    {
        _config = config;
        Enabled = config.Enabled;
        ServiceUrl = config.ServiceUrl;
        // Создаём новую ObservableCollection как копию списка маппингов.
        Mappings = new ObservableCollection<TagVisualizationMapping>(config.Mappings);
    }

    /// <summary>
    /// Команда добавления нового маппинга.
    /// Создаёт пустой TagVisualizationMapping, добавляет в коллекцию
    /// и сразу выделяет его (чтобы пользователь мог редактировать прямо в таблице).
    /// </summary>
    [RelayCommand]
    private void AddMapping()
    {
        var mapping = new TagVisualizationMapping();
        Mappings.Add(mapping);
        SelectedMapping = mapping; // Выделяем добавленную строку в таблице
    }

    /// <summary>
    /// Команда удаления выбранного маппинга.
    /// Проверяем, что SelectedMapping != null перед удалением.
    /// </summary>
    [RelayCommand]
    private void RemoveMapping()
    {
        if (SelectedMapping != null)
            Mappings.Remove(SelectedMapping);
    }

    /// <summary>
    /// Команда сохранения настроек.
    /// Копирует текущие значения свойств обратно в VisualizationConfig.
    /// После этого главное окно сохранит полный конфиг в файл config.json.
    /// </summary>
    [RelayCommand]
    private void Save()
    {
        if (_config == null) return;
        _config.Enabled = Enabled;
        _config.ServiceUrl = ServiceUrl;
        // Очищаем оригинальный список и заполняем из рабочей коллекции.
        _config.Mappings.Clear();
        _config.Mappings.AddRange(Mappings);
    }
}
