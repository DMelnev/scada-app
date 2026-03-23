using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScadaApp.Models;
using System.Collections.ObjectModel;

namespace ScadaApp.ViewModels;

/// <summary>ViewModel окна настроек 3D-визуализации.</summary>
public partial class VisualizationSettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private string _serviceUrl = "ws://localhost:8080";
    [ObservableProperty] private ObservableCollection<TagVisualizationMapping> _mappings = new();
    [ObservableProperty] private TagVisualizationMapping? _selectedMapping;

    private VisualizationConfig? _config;

    public void Load(VisualizationConfig config)
    {
        _config = config;
        Enabled = config.Enabled;
        ServiceUrl = config.ServiceUrl;
        Mappings = new ObservableCollection<TagVisualizationMapping>(config.Mappings);
    }

    [RelayCommand]
    private void AddMapping()
    {
        var mapping = new TagVisualizationMapping();
        Mappings.Add(mapping);
        SelectedMapping = mapping;
    }

    [RelayCommand]
    private void RemoveMapping()
    {
        if (SelectedMapping != null)
            Mappings.Remove(SelectedMapping);
    }

    [RelayCommand]
    private void Save()
    {
        if (_config == null) return;
        _config.Enabled = Enabled;
        _config.ServiceUrl = ServiceUrl;
        _config.Mappings.Clear();
        _config.Mappings.AddRange(Mappings);
    }
}
