using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels.MenuItemViewModels;

public sealed class JsonDiffMenuItemViewModel : MenuItemViewModel
{
    public const string MenuId = "json-diff";
    private const string MenuKind = "Primary";
    private const string MenuIcon = "🧩";

    public override string LocalizationKey => "menu.json_diff";

    private JsonDiffMenuItemViewModel(string header, IRelayCommand command)
        : base(MenuId, header, command, MenuKind, MenuIcon) { }

    public static JsonDiffMenuItemViewModel Create(string header, IRelayCommand command)
        => new(header, command);
}
