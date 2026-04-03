using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using DevCrew.Desktop.Controls;
using DevCrew.Desktop.Models;
using AvaloniaEdit;
using AvaloniaEdit.Document;

namespace DevCrew.Desktop.Behaviors;

/// <summary>
/// Behavior that adds hover tooltip support to TextEditor for highlighted content.
/// Tracks mouse position and displays tooltip information for matches from a highlight source.
/// </summary>
public static class HoverHighlightBehavior
{
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<object, TextEditor, bool>("IsEnabled");

    public static readonly AttachedProperty<object?> HighlightSourceProperty =
        AvaloniaProperty.RegisterAttached<object, TextEditor, object?>(
            "HighlightSource",
            null);

    static HoverHighlightBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<TextEditor>(OnIsEnabledChanged);
    }

    public static bool GetIsEnabled(TextEditor control) => control.GetValue(IsEnabledProperty);
    public static void SetIsEnabled(TextEditor control, bool value) => control.SetValue(IsEnabledProperty, value);

    public static object? GetHighlightSource(TextEditor control) => control.GetValue(HighlightSourceProperty);
    public static void SetHighlightSource(TextEditor control, object? value) => control.SetValue(HighlightSourceProperty, value);

    private static void OnIsEnabledChanged(TextEditor editor, AvaloniaPropertyChangedEventArgs e)
    {
        var isEnabled = e.NewValue is true;

        if (isEnabled)
        {
            AttachHandlers(editor);
        }
        else
        {
            DetachHandlers(editor);
        }
    }

    private static void AttachHandlers(TextEditor editor)
    {
        editor.AddHandler(
            InputElement.PointerMovedEvent,
            TextEditor_PointerMoved,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
        editor.AddHandler(
            InputElement.PointerExitedEvent,
            TextEditor_PointerExited,
            RoutingStrategies.Tunnel | RoutingStrategies.Bubble,
            handledEventsToo: true);
    }

    private static void DetachHandlers(TextEditor editor)
    {
        editor.RemoveHandler(InputElement.PointerMovedEvent, TextEditor_PointerMoved);
        editor.RemoveHandler(InputElement.PointerExitedEvent, TextEditor_PointerExited);
    }

    private static void TextEditor_PointerMoved(object? sender, PointerEventArgs e)
    {
        if (sender is not TextEditor editor)
            return;

        var highlightSource = GetHighlightSource(editor);
        var ranges = GetMatchedRanges(highlightSource);
        if (ranges.Count == 0)
        {
            ToolTip.SetTip(editor, null);
            ToolTip.SetIsOpen(editor, false);
            return;
        }

        var point = e.GetPosition(editor.TextArea.TextView);
        var match = FindMatchAtPoint(editor, point, ranges);

        if (match is not null)
        {
            ToolTip.SetTip(editor, match.TooltipText);
            ToolTip.SetIsOpen(editor, true);
        }
        else
        {
            ToolTip.SetTip(editor, null);
            ToolTip.SetIsOpen(editor, false);
        }
    }

    private static void TextEditor_PointerExited(object? sender, PointerEventArgs e)
    {
        if (sender is TextEditor editor)
        {
            ToolTip.SetTip(editor, null);
            ToolTip.SetIsOpen(editor, false);
        }
    }

    /// <summary>
    /// Finds a match at the given screen point within the editor.
    /// Uses line-based character position detection.
    /// </summary>
    private static HighlightDisplayItem? FindMatchAtPoint(
        TextEditor editor,
        Point point,
        IReadOnlyList<HighlightDisplayItem> ranges)
    {
        try
        {
            var textView = editor.TextArea.TextView;

            // TextView.GetPositionFloor expects coordinates in document space.
            // Pointer positions are relative to the viewport, so include scroll offset.
            var documentPoint = point + textView.ScrollOffset;

            // Get the TextViewPosition from the point
            var textViewPosition = textView.GetPositionFloor(documentPoint);
            if (textViewPosition is null)
                return null;

            var position = textViewPosition.Value;

            // Get the document line and absolute offset
            var document = editor.Document;
            if (document is null)
                return null;

            var line = document.GetLineByNumber(position.Line);
            if (line is null)
                return null;

            int absoluteOffset = document.GetOffset(new TextLocation(position.Line, position.Column));
            absoluteOffset = Math.Clamp(absoluteOffset, 0, document.TextLength);

            // Find a match that contains this offset
            var matchAtPoint = ranges.FirstOrDefault(m =>
                m.Index <= absoluteOffset && absoluteOffset < m.Index + m.Length);

            return matchAtPoint;
        }
        catch
        {
            // Silently handle any position calculation errors
            return null;
        }
    }

    private static IReadOnlyList<HighlightDisplayItem> GetMatchedRanges(object? highlightSource)
    {
        return highlightSource switch
        {
            RegexMatchColorizingTransformer renderer => renderer.MatchedRanges,
            _ => []
        };
    }
}
