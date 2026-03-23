using ScadaApp.ViewModels;
using System.Windows;

namespace ScadaApp.Views;

/// <summary>Главное окно приложения.</summary>
public partial class MainWindow : Window
{
    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
