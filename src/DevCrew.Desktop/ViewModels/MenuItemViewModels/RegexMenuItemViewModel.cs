using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels.MenuItemViewModels;

public sealed class RegexMenuItemViewModel : MenuItemViewModel
{
    public const string MenuId = "regex";
    private const string MenuKind = "Primary";
    private const string MenuIcon = "🔎";

    public override string LocalizationKey => "menu.regex";

    private RegexMenuItemViewModel(string header, IRelayCommand command)
        : base(MenuId, header, command, MenuKind, MenuIcon) { }

    public static RegexMenuItemViewModel Create(string header, IRelayCommand command)
        => new(header, command);
}
