using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels.MenuItemViewModels;

public sealed class Base64DecoderMenuItemViewModel : MenuItemViewModel
{
    public const string MenuId = "base64-decoder";
    private const string MenuKind = "Primary";
    private const string MenuIcon = "🔓";

    public override string LocalizationKey => "menu.base64_decoder";

    private Base64DecoderMenuItemViewModel(string header, IRelayCommand command)
        : base(MenuId, header, command, MenuKind, MenuIcon) { }

    public static Base64DecoderMenuItemViewModel Create(string header, IRelayCommand command)
        => new(header, command);
}
