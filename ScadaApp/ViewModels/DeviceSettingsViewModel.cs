using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScadaApp.Models;
using ScadaApp.Services;
using System.Collections.ObjectModel;
using System.Threading.Tasks;

namespace ScadaApp.ViewModels;

/// <summary>ViewModel окна настройки устройств.</summary>
public partial class DeviceSettingsViewModel : ObservableObject
{
    private readonly IConfigurationService _configService;

    [ObservableProperty] private ObservableCollection<DeviceConfig> _devices = new();
    [ObservableProperty] private DeviceConfig? _selectedDevice;

    private AppConfig? _config;

    public DeviceSettingsViewModel(IConfigurationService configService)
    {
        _configService = configService;
    }

    public async Task LoadAsync(AppConfig config)
    {
        _config = config;
        Devices = new ObservableCollection<DeviceConfig>(config.Devices);
    }

    [RelayCommand]
    private void AddDevice()
    {
        var device = new DeviceConfig { Name = "Новое устройство" };
        Devices.Add(device);
        SelectedDevice = device;
    }

    [RelayCommand]
    private void RemoveDevice()
    {
        if (SelectedDevice == null) return;
        Devices.Remove(SelectedDevice);
        SelectedDevice = null;
    }

    [RelayCommand]
    private void Save()
    {
        if (_config == null) return;
        _config.Devices.Clear();
        _config.Devices.AddRange(Devices);
    }

    [RelayCommand]
    private void Cancel()
    {
        if (_config != null)
            Devices = new ObservableCollection<DeviceConfig>(_config.Devices);
    }
}
