using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels.MenuItemViewModels;

public sealed class JsonFormatterMenuItemViewModel : MenuItemViewModel
{
    public const string MenuId = "json-formatter";
    private const string MenuKind = "Primary";
    private const string MenuIcon = "📋";

    public override string LocalizationKey => "menu.json_formatter";

    private JsonFormatterMenuItemViewModel(string header, IRelayCommand command)
        : base(MenuId, header, command, MenuKind, MenuIcon) { }

    public static JsonFormatterMenuItemViewModel Create(string header, IRelayCommand command)
        => new(header, command);
}
