using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;

namespace DevCrew.Desktop.Controls;

public partial class BrowseActionButton : UserControl
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<BrowseActionButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<BrowseActionButton, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<string?> ToolTipTextProperty =
        AvaloniaProperty.Register<BrowseActionButton, string?>(nameof(ToolTipText));

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

    public BrowseActionButton()
    {
        InitializeComponent();
    }
}