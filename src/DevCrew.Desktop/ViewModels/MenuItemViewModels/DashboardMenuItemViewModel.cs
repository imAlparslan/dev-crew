using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels.MenuItemViewModels;

public sealed class DashboardMenuItemViewModel : MenuItemViewModel
{
    public const string MenuId = "dashboard";
    private const string MenuKind = "Primary";
    private const string MenuIcon = "🎯";

    public override string LocalizationKey => "menu.dashboard";

    private DashboardMenuItemViewModel(string header, IRelayCommand command)
        : base(MenuId, header, command, MenuKind, MenuIcon) { }

    public static DashboardMenuItemViewModel Create(string header, IRelayCommand command)
        => new(header, command);
}
