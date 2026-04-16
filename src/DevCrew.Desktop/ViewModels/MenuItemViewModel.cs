using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Desktop.Services;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for sidebar menu items.
/// </summary>
public abstract partial class MenuItemViewModel : ObservableObject
{
    /// <summary>
    /// The localization key used to fetch this item's display header.
    /// </summary>
    public abstract string LocalizationKey { get; }

    /// <summary>
    /// Updates the Header property using the given localization service.
    /// </summary>
    public void RefreshHeader(ILocalizationService localizationService)
        => Header = localizationService.GetString(LocalizationKey);

    protected MenuItemViewModel(string id, string header, IRelayCommand command, string kind = "Primary", string? icon = null)
    {
        Id = id;
        Header = header;
        Command = command;
        Kind = kind;
        Icon = icon;
    }

    public string Id { get; }

    [ObservableProperty]
    private string header = string.Empty;

    public IRelayCommand Command { get; }

    /// <summary>
    /// Visual kind used by the view to style the button.
    /// </summary>
    public string Kind { get; }

    public string? Icon { get; }

    [ObservableProperty]
    private bool isSelected;
}
