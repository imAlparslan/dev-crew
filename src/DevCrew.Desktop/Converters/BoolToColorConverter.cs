using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DevCrew.Desktop.Converters;

/// <summary>
/// Converts boolean values to Color based on parameter.
/// Parameter format: "TrueColor|FalseColor" (e.g., "#4CAF50|#F44336")
/// </summary>
public sealed class BoolToColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not bool boolValue || parameter is not string colorPair)
            return Colors.Transparent;

        var colors = colorPair.Split('|');
        if (colors.Length != 2)
            return Colors.Transparent;

        var colorString = boolValue ? colors[0] : colors[1];
        return Color.Parse(colorString);
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
