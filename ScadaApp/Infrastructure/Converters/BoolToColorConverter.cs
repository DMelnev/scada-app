using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace ScadaApp.Infrastructure.Converters;

/// <summary>Конвертер булевого значения в цвет.</summary>
public class BoolToColorConverter : IValueConverter
{
    public object TrueColor { get; set; } = Colors.Green;
    public object FalseColor { get; set; } = Colors.Red;

    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is bool b)
            return new SolidColorBrush(b ? (Color)TrueColor : (Color)FalseColor);
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
