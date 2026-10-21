using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels.MenuItemViewModels;

public sealed class McpMenuItemViewModel : MenuItemViewModel
{
    public const string MenuId = "mcp";
    private const string MenuKind = "Secondary";
    private const string MenuIcon = "🔌";

    public override string LocalizationKey => "menu.mcp";

    private McpMenuItemViewModel(string header, IRelayCommand command)
        : base(MenuId, header, command, MenuKind, MenuIcon) { }

    public static McpMenuItemViewModel Create(string header, IRelayCommand command)
        => new(header, command);
}
