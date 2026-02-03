using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace DevCrew.Desktop.Behaviors;

public sealed class LoadedCommandBehavior
{
    private LoadedCommandBehavior()
    {
    }
    public static readonly AttachedProperty<ICommand?> CommandProperty =
        AvaloniaProperty.RegisterAttached<LoadedCommandBehavior, Control, ICommand?>("Command");

    public static readonly AttachedProperty<object?> CommandParameterProperty =
        AvaloniaProperty.RegisterAttached<LoadedCommandBehavior, Control, object?>("CommandParameter");

    public static readonly AttachedProperty<bool> ExecuteOnceProperty =
        AvaloniaProperty.RegisterAttached<LoadedCommandBehavior, Control, bool>("ExecuteOnce", true);

    public static ICommand? GetCommand(Control control) => control.GetValue(CommandProperty);

    public static void SetCommand(Control control, ICommand? value) => control.SetValue(CommandProperty, value);

    public static object? GetCommandParameter(Control control) => control.GetValue(CommandParameterProperty);

    public static void SetCommandParameter(Control control, object? value) => control.SetValue(CommandParameterProperty, value);

    public static bool GetExecuteOnce(Control control) => control.GetValue(ExecuteOnceProperty);

    public static void SetExecuteOnce(Control control, bool value) => control.SetValue(ExecuteOnceProperty, value);

    static LoadedCommandBehavior()
    {
        CommandProperty.Changed.AddClassHandler<Control>(OnCommandChanged);
    }

    private static void OnCommandChanged(Control control, AvaloniaPropertyChangedEventArgs e)
    {
        control.Loaded -= OnLoaded;

        if (e.NewValue is not null)
        {
            control.Loaded += OnLoaded;
        }
    }

    private static void OnLoaded(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Control control)
            return;

        var command = GetCommand(control);
        if (command is null)
            return;

        var parameter = GetCommandParameter(control);
        if (command.CanExecute(parameter))
        {
            command.Execute(parameter);
        }

        if (GetExecuteOnce(control))
        {
            control.Loaded -= OnLoaded;
        }
    }
}
