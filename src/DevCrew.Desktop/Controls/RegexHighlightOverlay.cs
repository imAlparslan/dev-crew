using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using DevCrew.Desktop.ViewModels;

namespace DevCrew.Desktop.Controls;

public sealed class RegexHighlightOverlay : Control
{
    public static readonly StyledProperty<IBrush?> HighlightBrushProperty =
        AvaloniaProperty.Register<RegexHighlightOverlay, IBrush?>(nameof(HighlightBrush));

    public static readonly StyledProperty<IBrush?> HoverBrushProperty =
        AvaloniaProperty.Register<RegexHighlightOverlay, IBrush?>(nameof(HoverBrush));

    public static readonly StyledProperty<IBrush?> HighlightBorderBrushProperty =
        AvaloniaProperty.Register<RegexHighlightOverlay, IBrush?>(nameof(HighlightBorderBrush));

    public static readonly StyledProperty<double> HighlightBorderThicknessProperty =
        AvaloniaProperty.Register<RegexHighlightOverlay, double>(nameof(HighlightBorderThickness), 1);

    public IBrush? HighlightBrush
    {
        get => GetValue(HighlightBrushProperty);
        set => SetValue(HighlightBrushProperty, value);
    }

    public IBrush? HoverBrush
    {
        get => GetValue(HoverBrushProperty);
        set => SetValue(HoverBrushProperty, value);
    }

    public IBrush? HighlightBorderBrush
    {
        get => GetValue(HighlightBorderBrushProperty);
        set => SetValue(HighlightBorderBrushProperty, value);
    }

    public double HighlightBorderThickness
    {
        get => GetValue(HighlightBorderThicknessProperty);
        set => SetValue(HighlightBorderThicknessProperty, value);
    }

    private readonly List<RenderedRegion> _regions = [];

    private TextLayout? _textLayout;
    private string _text = string.Empty;
    private IReadOnlyList<RegexHighlightDisplayItem> _matches = [];
    private Thickness _textPadding;
    private Vector _scrollOffset;
    private Typeface _typeface = Typeface.Default;
    private double _fontSize = 14;
    private RegexHighlightDisplayItem? _hoveredMatch;

    public void UpdatePresentation(
        TextLayout? textLayout,
        string text,
        IReadOnlyList<RegexHighlightDisplayItem> matches,
        Thickness textPadding,
        Vector scrollOffset,
        Typeface typeface,
        double fontSize)
    {
        _textLayout = textLayout;
        _text = text;
        _matches = matches;
        _textPadding = textPadding;
        _scrollOffset = scrollOffset;
        _typeface = typeface;
        _fontSize = fontSize;
        BuildRegions();
        InvalidateVisual();
    }

    public void SetHoveredMatch(RegexHighlightDisplayItem? match)
    {
        if (ReferenceEquals(_hoveredMatch, match))
        {
            return;
        }

        _hoveredMatch = match;
        InvalidateVisual();
    }

    public RegexHighlightDisplayItem? GetMatchAt(Point point)
    {
        return _regions.FirstOrDefault(region => region.Bounds.Contains(point))?.Match;
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);

        using var clip = context.PushClip(new Rect(Bounds.Size));
        var defaultHighlightBrush = (IBrush?)Application.Current?.FindResource("BrushRegexHighlight") ?? Brushes.Transparent;
        var defaultHoverBrush = (IBrush?)Application.Current?.FindResource("BrushRegexHighlightHover") ?? defaultHighlightBrush;
        var borderBrush = (IBrush?)Application.Current?.FindResource("BrushRegexHighlightBorder") ?? defaultHighlightBrush;

        var highlightBrush = HighlightBrush ?? defaultHighlightBrush;
        var hoverBrush = HoverBrush ?? defaultHoverBrush;
        var pen = new Pen(HighlightBorderBrush ?? borderBrush, HighlightBorderThickness);

        foreach (var region in _regions)
        {
            var brush = ReferenceEquals(region.Match, _hoveredMatch) ? hoverBrush : highlightBrush;
            context.DrawRectangle(brush, pen, region.Bounds, 4, 4);
        }
    }

    private void BuildRegions()
    {
        _regions.Clear();

        if (_textLayout is null || _matches.Count == 0)
        {
            return;
        }

        var lineTop = 0d;
        foreach (var line in _textLayout.TextLines)
        {
            var lineStart = line.FirstTextSourceIndex;
            var lineEnd = lineStart + line.Length;

            foreach (var match in _matches)
            {
                var matchStart = match.Index;
                var matchEnd = match.Index + match.Length;
                if (matchEnd <= lineStart || matchStart >= lineEnd)
                {
                    continue;
                }

                var segmentStart = Math.Max(matchStart, lineStart);
                var segmentEnd = Math.Min(matchEnd, lineEnd);
                var segmentLength = segmentEnd - segmentStart;
                if (segmentLength <= 0)
                {
                    continue;
                }

                var prefixLength = segmentStart - lineStart;
                var lineText = SafeSubstring(_text, lineStart, line.Length);
                var safePrefixLength = Math.Clamp(prefixLength, 0, lineText.Length);
                var prefixText = safePrefixLength > 0 ? lineText[..safePrefixLength] : string.Empty;
                var segmentText = SafeSubstring(_text, segmentStart, segmentLength);

                var x = _textPadding.Left + MeasureWidth(prefixText) - _scrollOffset.X;
                var width = Math.Max(4, MeasureWidth(segmentText));
                var y = _textPadding.Top + lineTop - _scrollOffset.Y;
                var height = Math.Max(4, line.Height);

                _regions.Add(new RenderedRegion(new Rect(x, y, width, height), match));
            }

            lineTop += line.Height;
        }
    }

    private double MeasureWidth(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        using var layout = new TextLayout(
            text,
            _typeface,
            _fontSize,
            Brushes.Transparent,
            TextAlignment.Left,
            TextWrapping.NoWrap);

        return layout.WidthIncludingTrailingWhitespace;
    }

    private static string SafeSubstring(string source, int start, int length)
    {
        if (string.IsNullOrEmpty(source) || length <= 0)
        {
            return string.Empty;
        }

        // Line indices can briefly lag behind text updates while editing.
        if (start < 0)
        {
            length += start;
            start = 0;
        }

        if (start >= source.Length || length <= 0)
        {
            return string.Empty;
        }

        var safeLength = Math.Min(length, source.Length - start);
        return source.Substring(start, safeLength);
    }

    private sealed record RenderedRegion(Rect Bounds, RegexHighlightDisplayItem Match);
}