using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DevCrew.Desktop.Controls;

public partial class CopyActionButton : UserControl
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<CopyActionButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<CopyActionButton, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<string?> ToolTipTextProperty =
        AvaloniaProperty.Register<CopyActionButton, string?>(nameof(ToolTipText));

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

    private bool _isFeedbackActive;

    public CopyActionButton()
    {
        InitializeComponent();
        PART_Button.AddHandler(Button.ClickEvent, OnButtonClicked, RoutingStrategies.Tunnel);
    }

    private async void OnButtonClicked(object? sender, RoutedEventArgs e)
    {
        if (_isFeedbackActive) return;
        _isFeedbackActive = true;

        PART_Button.Classes.Remove("AccentActionButton");
        PART_Button.Classes.Add("SuccessActionButton");

        await Task.Delay(2000);

        PART_Button.Classes.Remove("SuccessActionButton");
        PART_Button.Classes.Add("AccentActionButton");

        _isFeedbackActive = false;
    }
}
