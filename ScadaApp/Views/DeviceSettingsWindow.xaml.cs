using ScadaApp.Models;
using ScadaApp.ViewModels;
using System.Windows;
using System.Windows.Data;

namespace ScadaApp.Views;

/// <summary>Окно настройки устройств.</summary>
public partial class DeviceSettingsWindow : Window
{
    private readonly DeviceSettingsViewModel _vm;

    public DeviceSettingsWindow(DeviceSettingsViewModel viewModel, Models.AppConfig config)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
        _ = _vm.LoadAsync(config);
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        _vm.SaveCommand.Execute(null);
        DialogResult = true;
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        _vm.CancelCommand.Execute(null);
        DialogResult = false;
    }
}
