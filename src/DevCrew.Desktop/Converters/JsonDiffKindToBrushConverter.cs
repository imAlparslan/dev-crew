using System.Globalization;
using Avalonia.Data.Converters;
using Avalonia.Media;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Shared.Enums;

namespace DevCrew.Desktop.Converters;

public sealed class JsonDiffKindToBrushConverter : IValueConverter
{
    private static readonly IBrush AddedBackground = new SolidColorBrush(Color.Parse("#1F2E4A2A"));
    private static readonly IBrush RemovedBackground = new SolidColorBrush(Color.Parse("#1F4A2A2A"));
    private static readonly IBrush ChangedBackground = new SolidColorBrush(Color.Parse("#1F5A4A1F"));
    private static readonly IBrush UnchangedBackground = new SolidColorBrush(Color.Parse("#15334155"));

    private static readonly IBrush AddedForeground = new SolidColorBrush(Color.Parse("#86EFAC"));
    private static readonly IBrush RemovedForeground = new SolidColorBrush(Color.Parse("#FCA5A5"));
    private static readonly IBrush ChangedForeground = new SolidColorBrush(Color.Parse("#FCD34D"));
    private static readonly IBrush UnchangedForeground = new SolidColorBrush(Color.Parse("#CBD5E1"));

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not JsonDiffKind kind)
        {
            return UnchangedForeground;
        }

        var mode = parameter as string;
        var isBackground = string.Equals(mode, "Background", StringComparison.OrdinalIgnoreCase);

        return (kind, isBackground) switch
        {
            (JsonDiffKind.Added, true) => AddedBackground,
            (JsonDiffKind.Removed, true) => RemovedBackground,
            (JsonDiffKind.Changed, true) => ChangedBackground,
            (JsonDiffKind.Unchanged, true) => UnchangedBackground,
            (JsonDiffKind.Added, false) => AddedForeground,
            (JsonDiffKind.Removed, false) => RemovedForeground,
            (JsonDiffKind.Changed, false) => ChangedForeground,
            _ => UnchangedForeground
        };
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
