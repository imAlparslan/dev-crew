using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels.MenuItemViewModels;

public sealed class JwtBuilderMenuItemViewModel : MenuItemViewModel
{
    public const string MenuId = "jwt-builder";
    private const string MenuKind = "Primary";
    private const string MenuIcon = "🔧";

    public override string LocalizationKey => "menu.jwt_builder";

    private JwtBuilderMenuItemViewModel(string header, IRelayCommand command)
        : base(MenuId, header, command, MenuKind, MenuIcon) { }

    public static JwtBuilderMenuItemViewModel Create(string header, IRelayCommand command)
        => new(header, command);
}
