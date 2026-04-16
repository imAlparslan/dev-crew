using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels.MenuItemViewModels;

public sealed class JwtDecoderMenuItemViewModel : MenuItemViewModel
{
    public const string MenuId = "jwt-decoder";
    private const string MenuKind = "Primary";
    private const string MenuIcon = "🔐";

    public override string LocalizationKey => "menu.jwt_decoder";

    private JwtDecoderMenuItemViewModel(string header, IRelayCommand command)
        : base(MenuId, header, command, MenuKind, MenuIcon) { }

    public static JwtDecoderMenuItemViewModel Create(string header, IRelayCommand command)
        => new(header, command);
}
