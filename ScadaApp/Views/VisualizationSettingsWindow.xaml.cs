using ScadaApp.Models;
using ScadaApp.ViewModels;
using System.Windows;

namespace ScadaApp.Views;

/// <summary>Окно настроек 3D-визуализации.</summary>
public partial class VisualizationSettingsWindow : Window
{
    private readonly VisualizationSettingsViewModel _vm;

    public VisualizationSettingsWindow(VisualizationSettingsViewModel viewModel, AppConfig config)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
        _vm.Load(config.Visualization);
    }

    private void SaveClick(object sender, RoutedEventArgs e)
    {
        _vm.SaveCommand.Execute(null);
        DialogResult = true;
    }

    private void CancelClick(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
