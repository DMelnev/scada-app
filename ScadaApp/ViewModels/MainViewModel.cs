using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ScadaApp.Infrastructure.Logging;
using ScadaApp.Models;
using ScadaApp.Services;
using ScadaApp.Views;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace ScadaApp.ViewModels;

/// <summary>Элемент отображения тега в главной таблице.</summary>
public partial class TagDisplayItem : ObservableObject
{
    [ObservableProperty] private string _deviceName = "";
    [ObservableProperty] private string _tagName = "";
    [ObservableProperty] private string _value = "—";
    [ObservableProperty] private string _quality = "—";
    [ObservableProperty] private DateTime _lastUpdate = DateTime.MinValue;
    [ObservableProperty] private bool _isConnected;

    public string DeviceId { get; set; } = "";
    public string TagId { get; set; } = "";
}

/// <summary>ViewModel главного окна приложения.</summary>
public partial class MainViewModel : ObservableObject
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IConfigurationService _configService;
    private readonly PollingService _pollingService;
    private readonly ObservableLogSink _logSink;
    private readonly ILogger<MainViewModel> _logger;

    [ObservableProperty] private AppConfig _config = new();
    [ObservableProperty] private bool _isPolling;
    [ObservableProperty] private string _statusText = "Готов";

    public ObservableCollection<TagDisplayItem> Tags { get; } = new();
    public ObservableCollection<LogEntry> LogEntries { get; }

    public MainViewModel(
        IServiceProvider serviceProvider,
        IConfigurationService configService,
        PollingService pollingService,
        ObservableLogSink logSink,
        ILogger<MainViewModel> logger)
    {
        _serviceProvider = serviceProvider;
        _configService = configService;
        _pollingService = pollingService;
        _logSink = logSink;
        _logger = logger;

        LogEntries = logSink.LogEntries;
        _pollingService.TagValueChanged += OnTagValueChanged;
    }

    public async Task LoadConfigAsync()
    {
        Config = await _configService.LoadAsync();
        RefreshTagList();
        _logger.LogInformation("Конфигурация загружена ({DeviceCount} устройств)", Config.Devices.Count);
    }

    private void RefreshTagList()
    {
        Tags.Clear();
        foreach (var device in Config.Devices)
        {
            foreach (var tag in device.Tags)
            {
                Tags.Add(new TagDisplayItem
                {
                    DeviceId = device.Id,
                    DeviceName = device.Name,
                    TagId = tag.Id,
                    TagName = tag.Name,
                });
            }
        }
        StatusText = $"Устройств: {Config.Devices.Count}, Тегов: {Tags.Count}";
    }

    private void OnTagValueChanged(object? sender, TagValueChangedEventArgs e)
    {
        var item = Tags.FirstOrDefault(t => t.DeviceId == e.DeviceId && t.TagName == e.TagName);
        if (item != null)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                item.Value = e.Value?.ToString() ?? "null";
                item.Quality = e.Quality;
                item.LastUpdate = e.Timestamp.ToLocalTime();
                item.IsConnected = true;
            });
        }
    }

    [RelayCommand(CanExecute = nameof(CanStartPolling))]
    private async Task StartPolling()
    {
        _pollingService.Configure(Config.Devices);
        await _pollingService.StartAsync(default);
        IsPolling = true;
        StatusText = "Опрос запущен";
        _logger.LogInformation("Опрос запущен");
    }

    private bool CanStartPolling() => !IsPolling;

    [RelayCommand(CanExecute = nameof(CanStopPolling))]
    private async Task StopPolling()
    {
        await _pollingService.StopAsync(default);
        IsPolling = false;
        StatusText = "Опрос остановлен";
        _logger.LogInformation("Опрос остановлен");
    }

    private bool CanStopPolling() => IsPolling;

    [RelayCommand]
    private void OpenDeviceSettings()
    {
        var window = _serviceProvider.GetRequiredService<DeviceSettingsWindow>();
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            _ = _configService.SaveAsync(Config);
            RefreshTagList();
        }
    }

    [RelayCommand]
    private void OpenTagSettings()
    {
        var window = _serviceProvider.GetRequiredService<TagSettingsWindow>();
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            _ = _configService.SaveAsync(Config);
            RefreshTagList();
        }
    }

    [RelayCommand]
    private void OpenDatabaseSettings()
    {
        var window = _serviceProvider.GetRequiredService<DatabaseSettingsWindow>();
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            _ = _configService.SaveAsync(Config);
        }
    }

    [RelayCommand]
    private void OpenVisualizationSettings()
    {
        var window = _serviceProvider.GetRequiredService<VisualizationSettingsWindow>();
        window.Owner = Application.Current.MainWindow;
        if (window.ShowDialog() == true)
        {
            _ = _configService.SaveAsync(Config);
        }
    }
}
