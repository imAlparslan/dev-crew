using System.Windows.Input;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace DevCrew.Desktop.Controls;

public partial class EyeToggleButton : UserControl
{
    public static readonly StyledProperty<ICommand?> CommandProperty =
        AvaloniaProperty.Register<EyeToggleButton, ICommand?>(nameof(Command));

    public static readonly StyledProperty<object?> CommandParameterProperty =
        AvaloniaProperty.Register<EyeToggleButton, object?>(nameof(CommandParameter));

    public static readonly StyledProperty<string?> ToolTipTextProperty =
        AvaloniaProperty.Register<EyeToggleButton, string?>(nameof(ToolTipText));

    public static readonly StyledProperty<bool> IsToggledProperty =
        AvaloniaProperty.Register<EyeToggleButton, bool>(nameof(IsToggled));

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

    public bool IsToggled
    {
        get => GetValue(IsToggledProperty);
        set => SetValue(IsToggledProperty, value);
    }

    public EyeToggleButton()
    {
        InitializeComponent();
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);

        if (change.Property == IsToggledProperty && this.FindControl<Button>("PART_Button") is { } btn)
        {
            if ((bool)change.NewValue!)
            {
                btn.Classes.Remove("NeutralActionButton");
                btn.Classes.Add("AccentActionButton");
            }
            else
            {
                btn.Classes.Remove("AccentActionButton");
                btn.Classes.Add("NeutralActionButton");
            }
        }
    }
}
