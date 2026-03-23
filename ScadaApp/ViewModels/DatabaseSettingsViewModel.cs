using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScadaApp.Data;
using ScadaApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace ScadaApp.ViewModels;

/// <summary>ViewModel окна настроек базы данных.</summary>
public partial class DatabaseSettingsViewModel : ObservableObject
{
    [ObservableProperty] private bool _enabled;
    [ObservableProperty] private string _databaseType = "SQLite";
    [ObservableProperty] private string _connectionString = "Data Source=scada.db";
    [ObservableProperty] private int _saveIntervalMs = 5000;
    [ObservableProperty] private int _bufferSize = 100;
    [ObservableProperty] private string _testConnectionResult = "";

    public IReadOnlyList<string> DatabaseTypes { get; } = new[] { "SQLite", "SqlServer" };

    private DatabaseConfig? _config;

    public void Load(DatabaseConfig config)
    {
        _config = config;
        Enabled = config.Enabled;
        DatabaseType = config.DatabaseType;
        ConnectionString = config.ConnectionString;
        SaveIntervalMs = config.SaveIntervalMs;
        BufferSize = config.BufferSize;
    }

    [RelayCommand]
    private void Save()
    {
        if (_config == null) return;
        _config.Enabled = Enabled;
        _config.DatabaseType = DatabaseType;
        _config.ConnectionString = ConnectionString;
        _config.SaveIntervalMs = SaveIntervalMs;
        _config.BufferSize = BufferSize;
    }

    [RelayCommand]
    private async Task TestConnection()
    {
        TestConnectionResult = "Проверка...";
        try
        {
            var testConfig = new DatabaseConfig
            {
                DatabaseType = DatabaseType,
                ConnectionString = ConnectionString
            };
            await using var context = new ScadaDbContext(testConfig);
            await context.EnsureCreatedAsync();
            TestConnectionResult = "✓ Подключение успешно";
        }
        catch (Exception ex)
        {
            TestConnectionResult = $"✗ Ошибка: {ex.Message}";
        }
    }
}
