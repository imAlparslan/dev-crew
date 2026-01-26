using Avalonia.Data.Converters;
using System.Globalization;

namespace DevCrew.Desktop.Converters;

/// <summary>
/// Boolean değerini tersine çeviren converter
/// </summary>
public class BooleanInverseConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        return value is bool b ? !b : false;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo? culture)
    {
        return value is bool b ? !b : false;
    }
}
