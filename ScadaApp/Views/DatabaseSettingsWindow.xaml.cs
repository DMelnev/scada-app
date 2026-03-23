using ScadaApp.Models;
using ScadaApp.ViewModels;
using System.Windows;

namespace ScadaApp.Views;

/// <summary>Окно настроек базы данных.</summary>
public partial class DatabaseSettingsWindow : Window
{
    private readonly DatabaseSettingsViewModel _vm;

    public DatabaseSettingsWindow(DatabaseSettingsViewModel viewModel, AppConfig config)
    {
        InitializeComponent();
        _vm = viewModel;
        DataContext = _vm;
        _vm.Load(config.Database);
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
