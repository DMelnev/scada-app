using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScadaApp.Data;
using ScadaApp.Models;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Windows;

namespace ScadaApp.ViewModels;

/// <summary>
/// ViewModel для окна "Настройка базы данных" (DatabaseSettingsWindow).
/// Управляет настройками подключения к БД и кнопкой проверки соединения.
///
/// Хранит все настройки как отдельные свойства (не ссылку на DatabaseConfig),
/// чтобы изменения не применялись немедленно — только при нажатии "Сохранить".
/// </summary>
public partial class DatabaseSettingsViewModel : ObservableObject
{
    /// <summary>
    /// Включена ли запись в базу данных.
    /// Привязывается к CheckBox "Включить запись в БД".
    /// </summary>
    [ObservableProperty] private bool _enabled;

    /// <summary>
    /// Тип СУБД: "SQLite" или "SqlServer".
    /// Привязывается к ComboBox с типами баз данных.
    /// </summary>
    [ObservableProperty] private string _databaseType = "SQLite";

    /// <summary>
    /// Строка подключения к базе данных.
    /// Привязывается к TextBox в форме.
    /// </summary>
    [ObservableProperty] private string _connectionString = "Data Source=scada.db";

    /// <summary>
    /// Интервал сохранения буфера в мс.
    /// Привязывается к TextBox "Интервал сохранения".
    /// </summary>
    [ObservableProperty] private int _saveIntervalMs = 5000;

    /// <summary>
    /// Максимальный размер буфера значений в памяти.
    /// Привязывается к TextBox "Размер буфера".
    /// </summary>
    [ObservableProperty] private int _bufferSize = 100;

    /// <summary>
    /// Результат проверки подключения — отображается под кнопкой "Проверить подключение".
    /// Примеры: "✓ Подключение успешно", "✗ Ошибка: ..."
    /// </summary>
    [ObservableProperty] private string _testConnectionResult = "";

    /// <summary>
    /// Список доступных типов СУБД для ComboBox.
    /// IReadOnlyList — только для чтения, элементы нельзя добавить или удалить.
    /// </summary>
    public IReadOnlyList<string> DatabaseTypes { get; } = new[] { "SQLite", "SqlServer" };

    /// <summary>
    /// Ссылка на оригинальный объект DatabaseConfig из конфига.
    /// Используется при сохранении (записываем свойства обратно в конфиг).
    /// </summary>
    private DatabaseConfig? _config;

    /// <summary>
    /// Загружает настройки из DatabaseConfig в свойства ViewModel.
    /// Вызывается из code-behind окна при его открытии.
    /// </summary>
    public void Load(DatabaseConfig config)
    {
        _config = config;
        // Копируем значения из конфига в свойства ViewModel.
        Enabled = config.Enabled;
        DatabaseType = config.DatabaseType;
        ConnectionString = config.ConnectionString;
        SaveIntervalMs = config.SaveIntervalMs;
        BufferSize = config.BufferSize;
    }

    /// <summary>
    /// Команда сохранения настроек.
    /// Копирует текущие значения свойств обратно в объект DatabaseConfig.
    /// После этого главное окно сохранит конфиг в файл.
    /// </summary>
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

    /// <summary>
    /// Команда проверки подключения к базе данных.
    /// Создаёт временный DbContext с текущими настройками и пытается подключиться.
    /// EnsureCreatedAsync() создаёт базу и таблицы, если их нет.
    ///
    /// async Task — асинхронный метод (не блокирует UI во время подключения).
    /// </summary>
    [RelayCommand]
    private async Task TestConnection()
    {
        TestConnectionResult = "Проверка...";
        try
        {
            // Создаём временный объект настроек с текущими значениями из формы.
            var testConfig = new DatabaseConfig
            {
                DatabaseType = DatabaseType,
                ConnectionString = ConnectionString
            };

            // "await using" — асинхронное освобождение ресурсов (закрытие соединения).
            // ScadaDbContext — наш класс, обёртка над Entity Framework Core.
            await using var context = new ScadaDbContext(testConfig);

            // Пытаемся создать/открыть базу данных.
            await context.EnsureCreatedAsync();

            TestConnectionResult = "✓ Подключение успешно";
        }
        catch (Exception ex)
        {
            // При любой ошибке — показываем текст ошибки.
            TestConnectionResult = $"✗ Ошибка: {ex.Message}";
        }
    }
}
