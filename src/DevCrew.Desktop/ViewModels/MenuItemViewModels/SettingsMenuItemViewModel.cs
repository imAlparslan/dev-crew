using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels.MenuItemViewModels;

public sealed class SettingsMenuItemViewModel : MenuItemViewModel
{
    public const string MenuId = "settings";
    private const string MenuKind = "Secondary";
    private const string MenuIcon = "⚙️";

    public override string LocalizationKey => "menu.settings";

    private SettingsMenuItemViewModel(string header, IRelayCommand command)
        : base(MenuId, header, command, MenuKind, MenuIcon) { }

    public static SettingsMenuItemViewModel Create(string header, IRelayCommand command)
        => new(header, command);
}
