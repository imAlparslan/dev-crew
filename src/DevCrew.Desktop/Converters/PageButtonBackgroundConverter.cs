using System.Globalization;
using Avalonia;
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

        var app = Application.Current;
        if (app?.Resources == null)
            return null;

        // Ellipsis buttons should not be clickable and have muted appearance
        if (pageText == "...")
        {
            if (app.Resources.TryGetResource("ColorButtonPageDisabled", null, out var color) && color is Color disabledColor)
            {
                return new SolidColorBrush(disabledColor);
            }
            return null;
        }

        // Parse current page
        if (!int.TryParse(currentPageObj.ToString(), out int currentPage))
            return null;

        if (!int.TryParse(pageText, out int pageNum))
            return null;

        // Highlight current page with accent color
        if (pageNum == currentPage)
        {
            if (app.Resources.TryGetResource("ColorAccent", null, out var color) && color is Color accentColor)
            {
                return new SolidColorBrush(accentColor);
            }
        }

        // Default button color
        if (app.Resources.TryGetResource("ColorButtonPageDefault", null, out var defaultColor) && defaultColor is Color buttonColor)
        {
            return new SolidColorBrush(buttonColor);
        }

        return null;
    }
}
