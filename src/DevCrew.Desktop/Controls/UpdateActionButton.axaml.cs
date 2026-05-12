using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace DevCrew.Desktop.Controls;

public partial class UpdateActionButton : UserControl
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<UpdateActionButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<UpdateActionButton, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<string?> ToolTipTextProperty =
        AvaloniaProperty.Register<UpdateActionButton, string?>(nameof(ToolTipText));

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

    public UpdateActionButton()
    {
        InitializeComponent();
    }
}
