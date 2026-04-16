using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels.MenuItemViewModels;

public sealed class Base64EncoderMenuItemViewModel : MenuItemViewModel
{
    public const string MenuId = "base64-encoder";
    private const string MenuKind = "Primary";
    private const string MenuIcon = "🧬";

    public override string LocalizationKey => "menu.base64_encoder";

    private Base64EncoderMenuItemViewModel(string header, IRelayCommand command)
        : base(MenuId, header, command, MenuKind, MenuIcon) { }

    public static Base64EncoderMenuItemViewModel Create(string header, IRelayCommand command)
        => new(header, command);
}
