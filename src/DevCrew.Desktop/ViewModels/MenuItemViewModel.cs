using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for sidebar menu items.
/// </summary>
public partial class MenuItemViewModel : ObservableObject
{
    public MenuItemViewModel(string id, string header, IRelayCommand command, string kind = "Primary", string? icon = null)
    {
        Id = id;
        Header = header;
        Command = command;
        Kind = kind;
        Icon = icon;
    }

    public string Id { get; }

    public string Header { get; }

    public IRelayCommand Command { get; }

    /// <summary>
    /// Visual kind used by the view to style the button.
    /// </summary>
    public string Kind { get; }

    public string? Icon { get; }

    [ObservableProperty]
    private bool isSelected;
}
