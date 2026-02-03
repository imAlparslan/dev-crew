using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.LogicalTree;

namespace DevCrew.Desktop.Behaviors;

public sealed class FocusClearBehavior
{
    private FocusClearBehavior()
    {
    }
    public static readonly AttachedProperty<bool> IsEnabledProperty =
        AvaloniaProperty.RegisterAttached<FocusClearBehavior, Control, bool>("IsEnabled");

    public static bool GetIsEnabled(Control control) => control.GetValue(IsEnabledProperty);

    public static void SetIsEnabled(Control control, bool value) => control.SetValue(IsEnabledProperty, value);

    static FocusClearBehavior()
    {
        IsEnabledProperty.Changed.AddClassHandler<Control>(OnIsEnabledChanged);
    }

    private static void OnIsEnabledChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.NewValue is true)
        {
            control.PointerPressed += OnPointerPressed;
        }
        else
        {
            control.PointerPressed -= OnPointerPressed;
        }
    }

    private static void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (sender is not Control root)
            return;

        var source = e.Source as Control;
        if (source is null)
            return;

        if (source is TextBox || source.FindLogicalAncestorOfType<TextBox>() is not null)
            return;

        if (TopLevel.GetTopLevel(root)?.FocusManager is { } focusManager)
        {
            focusManager.ClearFocus();
        }
    }
}
