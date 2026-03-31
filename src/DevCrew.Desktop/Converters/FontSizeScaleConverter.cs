using System.Globalization;
using Avalonia.Data.Converters;

namespace DevCrew.Desktop.Converters;

public sealed class FontSizeScaleConverter : IValueConverter
{
    private const double BaseSize = 16.0;

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is double scale)
        {
            return BaseSize * scale;
        }
        return BaseSize;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
