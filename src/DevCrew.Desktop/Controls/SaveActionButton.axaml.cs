using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace DevCrew.Desktop.Controls;

public partial class SaveActionButton : UserControl
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<SaveActionButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<SaveActionButton, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<string?> ToolTipTextProperty =
        AvaloniaProperty.Register<SaveActionButton, string?>(nameof(ToolTipText));

    public ICommand? Command
    {
        get => GetValue(CommandProperty);
        set => SetValue(CommandProperty, value);
    }

    public object? CommandParameter
    {
        get => GetValue(CommandParameterProperty);
        set => SetValue(CommandParameterProperty, value);
    }

    public string? ToolTipText
    {
        get => GetValue(ToolTipTextProperty);
        set => SetValue(ToolTipTextProperty, value);
    }

    public SaveActionButton()
    {
        InitializeComponent();
    }
}
