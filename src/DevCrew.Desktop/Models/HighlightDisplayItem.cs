namespace DevCrew.Desktop.Models;

/// <summary>
/// Represents a highlighted text segment with metadata for tooltip display.
/// Used by DocumentColorizingTransformer implementations to expose match information
/// for hover effects and contextual information.
/// </summary>
public sealed record HighlightDisplayItem(
    int Index,
    int Length,
    string Value,
    string TooltipText);
