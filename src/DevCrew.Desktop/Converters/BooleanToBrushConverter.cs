using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DevCrew.Desktop.Converters;

public sealed class BooleanToBrushConverter : IValueConverter
{
    public IBrush? TrueBrush { get; set; }
    public IBrush? FalseBrush { get; set; }
    public string? TrueString { get; set; }
    public string? FalseString { get; set; }

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool? flag = value as bool?;
        
        if (!flag.HasValue)
        {
            // Return default for null values
            if (targetType == typeof(IBrush) || targetType == typeof(object))
                return FalseBrush;
            if (targetType == typeof(string))
                return FalseString;
        }

        if (targetType == typeof(string) || targetType == typeof(object))
        {
            if (TrueString != null || FalseString != null)
                return flag == true ? TrueString : FalseString;
        }
        
        if (targetType == typeof(IBrush) || targetType == typeof(object))
        {
            return flag == true ? TrueBrush : FalseBrush;
        }

        return flag == true ? TrueBrush : FalseBrush;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
