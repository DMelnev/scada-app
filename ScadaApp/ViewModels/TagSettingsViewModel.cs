using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScadaApp.Models;
using System.Collections.ObjectModel;

namespace ScadaApp.ViewModels;

/// <summary>ViewModel окна настройки тегов.</summary>
public partial class TagSettingsViewModel : ObservableObject
{
    [ObservableProperty] private ObservableCollection<DeviceConfig> _devices = new();
    [ObservableProperty] private DeviceConfig? _selectedDevice;
    [ObservableProperty] private ObservableCollection<TagConfig> _tags = new();

    partial void OnSelectedDeviceChanged(DeviceConfig? value)
    {
        Tags = value != null
            ? new ObservableCollection<TagConfig>(value.Tags)
            : new ObservableCollection<TagConfig>();
    }

    public void LoadDevices(System.Collections.Generic.IEnumerable<DeviceConfig> devices)
    {
        Devices = new ObservableCollection<DeviceConfig>(devices);
    }

    [RelayCommand]
    private void AddTag()
    {
        if (SelectedDevice == null) return;
        var tag = new TagConfig { Name = "NewTag" };
        Tags.Add(tag);
    }

    [RelayCommand]
    private void RemoveTag(TagConfig? tag)
    {
        if (tag != null) Tags.Remove(tag);
    }

    [RelayCommand]
    private void Save()
    {
        if (SelectedDevice == null) return;
        SelectedDevice.Tags.Clear();
        SelectedDevice.Tags.AddRange(Tags);
    }
}
