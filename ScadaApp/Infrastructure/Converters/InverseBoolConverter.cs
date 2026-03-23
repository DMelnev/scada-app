using System;
using System.Globalization;
using System.Windows.Data;

namespace ScadaApp.Infrastructure.Converters;

/// <summary>
/// Конвертер значений WPF: инвертирует булево значение (bool → !bool).
/// Используется в XAML для управления активностью кнопок через привязку данных.
///
/// Пример использования в XAML:
///   &lt;!-- Кнопка "Старт" активна когда IsPolling == false (опрос НЕ запущен) --&gt;
///   IsEnabled="{Binding IsPolling, Converter={StaticResource InverseBoolConverter}}"
///
///   &lt;!-- Кнопка "Стоп" активна когда IsPolling == true (опрос запущен) --&gt;
///   IsEnabled="{Binding IsPolling}"
///
/// Конвертер зарегистрирован в App.xaml как ресурс приложения:
///   &lt;converters:InverseBoolConverter x:Key="InverseBoolConverter"/&gt;
/// И доступен во всех окнах приложения как {StaticResource InverseBoolConverter}.
/// </summary>
public class InverseBoolConverter : IValueConverter
{
    /// <summary>
    /// Преобразует bool → !bool (инвертирует значение).
    /// "value is bool b" — паттерн-матчинг: проверяет тип и присваивает переменной.
    /// Если value не bool — возвращает оригинальное значение без изменений.
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;

    /// <summary>
    /// Обратное преобразование — тоже инверсия (операция симметрична).
    /// Используется при двустороннем связывании (TwoWay Binding).
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => value is bool b ? !b : value;
}
