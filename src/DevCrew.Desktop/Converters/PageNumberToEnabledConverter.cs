using System;
using System.Globalization;
using Avalonia.Data.Converters;

namespace DevCrew.Desktop.Converters;

public sealed class PageNumberToEnabledConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is string pageText)
        {
            // Disable ellipsis buttons
            return pageText != "...";
        }

        return true;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
