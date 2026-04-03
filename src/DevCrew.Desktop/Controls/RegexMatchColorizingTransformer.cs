using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using AvaloniaEdit.Document;
using AvaloniaEdit.Rendering;
using DevCrew.Desktop.Models;
using DevCrew.Desktop.ViewModels;

namespace DevCrew.Desktop.Controls;

public sealed class RegexMatchColorizingTransformer : IBackgroundRenderer
{
    private readonly List<HighlightDisplayItem> _matchedRanges = [];
    private readonly List<SimpleSegment> _segments = [];

    public KnownLayer Layer => KnownLayer.Selection;

    public IReadOnlyList<HighlightDisplayItem> MatchedRanges => _matchedRanges.AsReadOnly();

    public void SetMatches(IReadOnlyList<RegexHighlightDisplayItem> matches)
    {
        _matchedRanges.Clear();
        _segments.Clear();

        if (matches.Count == 0)
        {
            return;
        }

        foreach (var match in matches)
        {
            if (match.Length <= 0)
            {
                continue;
            }

            var startIndex = match.Index;
            var endIndex = match.Index + match.Length;

            if (startIndex < 0)
            {
                continue;
            }

            _matchedRanges.Add(new HighlightDisplayItem(
                Index: startIndex,
                Length: match.Length,
                Value: match.Value,
                TooltipText: string.IsNullOrWhiteSpace(match.TooltipText)
                    ? FormatTooltip(startIndex, endIndex, match.Value)
                    : match.TooltipText));

            _segments.Add(new SimpleSegment(startIndex, match.Length));
        }
    }

    public void Draw(TextView textView, DrawingContext drawingContext)
    {
        if (_segments.Count == 0 || textView.Document is null)
        {
            return;
        }

        var defaultHighlightBrush = (IBrush?)Application.Current?.FindResource("BrushRegexHighlight") ?? Brushes.Transparent;
        var textLength = textView.Document.TextLength;
        var builder = new BackgroundGeometryBuilder
        {
            AlignToWholePixels = true,
            CornerRadius = 3
        };

        foreach (var segment in _segments)
        {
            if (segment.Length <= 0 || segment.Offset < 0 || segment.Offset >= textLength)
            {
                continue;
            }

            var clampedLength = Math.Min(segment.Length, textLength - segment.Offset);
            if (clampedLength <= 0)
            {
                continue;
            }

            builder.AddSegment(textView, new SimpleSegment(segment.Offset, clampedLength));
        }

        var geometry = builder.CreateGeometry();
        if (geometry is not null)
        {
            drawingContext.DrawGeometry(defaultHighlightBrush, null, geometry);
        }
    }

    private static string FormatTooltip(int startIndex, int endIndex, string value)
    {
        return $"Position: {startIndex}-{endIndex}\nLength: {endIndex - startIndex}\nValue: {value}";
    }
}
