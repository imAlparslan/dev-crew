using System;
using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;

namespace DevCrew.Desktop.Converters;

public sealed class PageButtonBackgroundConverter : IMultiValueConverter
{
    public object? Convert(IList<object?> values, Type targetType, object? parameter, CultureInfo culture)
    {
        if (values.Count < 2)
            return null;

        var pageText = values[0] as string;
        var currentPageObj = values[1];

        if (string.IsNullOrEmpty(pageText) || currentPageObj == null)
            return null;

        // Ellipsis buttons should not be clickable and have muted appearance
        if (pageText == "...")
        {
            return new SolidColorBrush(Color.Parse("#3C3C3C")); // Darker gray for disabled appearance
        }

        // Parse current page
        if (!int.TryParse(currentPageObj.ToString(), out int currentPage))
            return null;

        if (!int.TryParse(pageText, out int pageNum))
            return null;

        // Highlight current page with accent color
        if (pageNum == currentPage)
        {
            return new SolidColorBrush(Color.Parse("#0E7C86")); // BrushAccent
        }

        return new SolidColorBrush(Color.Parse("#2D2D2D")); // Default button color
    }
}
