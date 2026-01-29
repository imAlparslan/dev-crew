using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DevCrew.Desktop.Converters;

public sealed class BooleanToBrushConverter : IValueConverter
{
    public IBrush? TrueBrush { get; set; }
    public IBrush? FalseBrush { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is bool flag && flag)
        {
            return TrueBrush;
        }

        return FalseBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
