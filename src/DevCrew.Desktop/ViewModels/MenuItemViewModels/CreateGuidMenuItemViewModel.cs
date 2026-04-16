using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels.MenuItemViewModels;

public sealed class CreateGuidMenuItemViewModel : MenuItemViewModel
{
    public const string MenuId = "create-guid";
    private const string MenuKind = "Primary";
    private const string MenuIcon = "🎲";

    public override string LocalizationKey => "menu.create_guid";

    private CreateGuidMenuItemViewModel(string header, IRelayCommand command)
        : base(MenuId, header, command, MenuKind, MenuIcon) { }

    public static CreateGuidMenuItemViewModel Create(string header, IRelayCommand command)
        => new(header, command);
}
