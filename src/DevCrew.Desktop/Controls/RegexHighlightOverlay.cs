using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Media.TextFormatting;
using DevCrew.Desktop.ViewModels;

namespace DevCrew.Desktop.Controls;

public sealed class RegexHighlightOverlay : Control
{
    private static readonly IBrush HighlightBrush = new SolidColorBrush(Color.FromArgb(120, 14, 165, 233));
    private static readonly IBrush HoverBrush = new SolidColorBrush(Color.FromArgb(170, 56, 189, 248));
    private static readonly IPen HighlightPen = new Pen(new SolidColorBrush(Color.FromArgb(180, 125, 211, 252)), 1);
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

        foreach (var region in _regions)
        {
            var brush = ReferenceEquals(region.Match, _hoveredMatch) ? HoverBrush : HighlightBrush;
            context.DrawRectangle(brush, HighlightPen, region.Bounds, 4, 4);
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
                var prefixText = prefixLength > 0 ? lineText[..prefixLength] : string.Empty;
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
        if (string.IsNullOrEmpty(source) || start >= source.Length || length <= 0)
        {
            return string.Empty;
        }

        var safeLength = Math.Min(length, source.Length - start);
        return source.Substring(start, safeLength);
    }

    private sealed record RenderedRegion(Rect Bounds, RegexHighlightDisplayItem Match);
}