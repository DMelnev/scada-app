using ScadaApp.Models;
using ScadaApp.ViewModels;
using System.Windows;

namespace ScadaApp.Views;

/// <summary>Окно настройки тегов.</summary>
public partial class TagSettingsWindow : Window
{
    private readonly TagSettingsViewModel _vm;

    public TagSettingsWindow(TagSettingsViewModel viewModel, AppConfig config)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
        _vm.LoadDevices(config.Devices);
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        _vm.SaveCommand.Execute(null);
        DialogResult = true;
    }

    private void CloseClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
