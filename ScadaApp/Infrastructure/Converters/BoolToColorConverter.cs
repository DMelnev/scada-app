using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ScadaApp.Infrastructure.Converters;

/// <summary>
/// Конвертер значений WPF: преобразует булево значение (bool) в цвет (Color/Brush).
/// Используется в XAML для окрашивания ячейки "Связь" в таблице тегов:
///   true  → зелёный (устройство подключено)
///   false → красный (нет связи)
///
/// IValueConverter — стандартный интерфейс WPF для конвертеров привязки данных.
/// Методы интерфейса:
///   Convert()     — из источника данных в UI (data → display)
///   ConvertBack() — обратно (display → data), не нужен для однонаправленной привязки
///
/// Как использовать в XAML:
///   &lt;DataGridTextColumn&gt;
///     &lt;DataGridTextColumn.ElementStyle&gt;
///       &lt;Style&gt;
///         &lt;Setter Property="Background" Value="{Binding IsConnected, Converter={StaticResource BoolToColorConverter}}"/&gt;
///       &lt;/Style&gt;
///     &lt;/DataGridTextColumn.ElementStyle&gt;
///   &lt;/DataGridTextColumn&gt;
///
/// TrueColor и FalseColor — свойства, которые можно переопределить в XAML.
/// </summary>
public class BoolToColorConverter : IValueConverter
{
    /// <summary>
    /// Цвет для значения true. Задаётся в App.xaml как свойство конвертера.
    /// По умолчанию: Colors.Green (зелёный).
    /// object — принимает Color, потому что XAML может передавать разные типы.
    /// </summary>
    public object TrueColor { get; set; } = Colors.Green;

    /// <summary>
    /// Цвет для значения false.
    /// По умолчанию: Colors.Red (красный).
    /// </summary>
    public object FalseColor { get; set; } = Colors.Red;

    /// <summary>
    /// Преобразует bool → SolidColorBrush (кисть нужного цвета).
    /// WPF использует кисти (Brush) для задания цвета фона/текста.
    ///
    /// Параметры:
    ///   value      — входное значение из привязки (ожидаем bool)
    ///   targetType — тип, в который нужно преобразовать (обычно Brush)
    ///   parameter  — дополнительный параметр из XAML (не используем)
    ///   culture    — культура (локаль) для форматирования (не используем)
    /// </summary>
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            // Тернарный оператор: если b == true → TrueColor, иначе → FalseColor
            // (Color) — явное приведение типа object к Color (мы знаем, что там Color)
            return new SolidColorBrush(b ? (Color)TrueColor : (Color)FalseColor);

        // Если value не bool — возвращаем серый цвет как нейтральный.
        return new SolidColorBrush(Colors.Gray);
    }

    /// <summary>
    /// Обратное преобразование (из цвета в bool).
    /// Не поддерживается — конвертер работает только в одну сторону.
    /// NotSupportedException — исключение "операция не поддерживается".
    /// </summary>
    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
