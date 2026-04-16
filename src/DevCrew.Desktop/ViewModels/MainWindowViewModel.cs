using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Application.Services;
using DevCrew.Desktop.Services;
using DevCrew.Desktop.ViewModels.MenuItemViewModels;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// Main window ViewModel
/// </summary>
public partial class MainWindowViewModel : BaseViewModel
{
    private readonly IApplicationService _applicationService;
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly Func<CreateGuidViewModel> _createGuidViewModelFactory;
    private readonly Func<JwtDecoderViewModel> _jwtDecoderViewModelFactory;
    private readonly Func<JwtBuilderViewModel> _jwtBuilderViewModelFactory;
    private readonly Func<JsonFormatterViewModel> _jsonFormatterViewModelFactory;
    private readonly Func<JsonDiffViewModel> _jsonDiffViewModelFactory;
    private readonly Func<Base64EncoderViewModel> _base64EncoderViewModelFactory;
    private readonly Func<Base64DecoderViewModel> _base64DecoderViewModelFactory;
    private readonly Func<RegexViewModel> _regexViewModelFactory;
    private readonly Func<SettingsViewModel> _settingsViewModelFactory;
    private readonly ILocalizationService _localizationService;

    private DashboardMenuItemViewModel? _dashboardItem;
    private CreateGuidMenuItemViewModel? _createGuidItem;
    private JwtDecoderMenuItemViewModel? _jwtDecoderItem;
    private JwtBuilderMenuItemViewModel? _jwtBuilderItem;
    private JsonFormatterMenuItemViewModel? _jsonFormatterItem;
    private JsonDiffMenuItemViewModel? _jsonDiffItem;
    private Base64EncoderMenuItemViewModel? _base64EncoderItem;
    private Base64DecoderMenuItemViewModel? _base64DecoderItem;
    private RegexMenuItemViewModel? _regexItem;
    private SettingsMenuItemViewModel? _settingsItem;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string menuTitle = string.Empty;

    [ObservableProperty]
    private string closeTabTooltip = string.Empty;

    [ObservableProperty]
    private bool isSidebarOpen = true;

    [ObservableProperty]
    private TabItemViewModel? selectedTab;

    /// <summary>
    /// Sidebar menu items.
    /// </summary>
    public ObservableCollection<MenuItemViewModel> MenuItems { get; } = new();

    /// <summary>
    /// Sidebar bottom menu items.
    /// </summary>
    public ObservableCollection<MenuItemViewModel> BottomMenuItems { get; } = new();

    /// <summary>
    /// Collection of open tabs.
    /// </summary>
    public ObservableCollection<TabItemViewModel> Tabs { get; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MainWindowViewModel"/> class.
    /// </summary>
    /// <param name="errorHandler">Error handler for centralized logging.</param>
    /// <param name="applicationService">Application service.</param>
    /// <param name="dashboardViewModel">Dashboard view model.</param>
    /// <param name="createGuidViewModelFactory">Factory for new GUID view models.</param>
    /// <param name="jwtDecoderViewModelFactory">Factory for new JWT Decoder view models.</param>
    /// <param name="jwtBuilderViewModelFactory">Factory for new JWT Builder view models.</param>
    /// <param name="jsonFormatterViewModelFactory">Factory for new JSON Formatter view models.</param>
    /// <param name="jsonDiffViewModelFactory">Factory for new JSON Diff view models.</param>
    /// <param name="base64EncoderViewModelFactory">Factory for new Base64 Encoder view models.</param>
    /// <param name="base64DecoderViewModelFactory">Factory for new Base64 Decoder view models.</param>
    /// <param name="regexViewModelFactory">Factory for new Regex view models.</param>
    /// <param name="settingsViewModelFactory">Factory for new Settings view models.</param>
    /// <param name="localizationService">Localization service for runtime language switching.</param>
    public MainWindowViewModel(
        IErrorHandler errorHandler,
        IApplicationService applicationService,
        DashboardViewModel dashboardViewModel,
        Func<CreateGuidViewModel> createGuidViewModelFactory,
        Func<JwtDecoderViewModel> jwtDecoderViewModelFactory,
        Func<JwtBuilderViewModel> jwtBuilderViewModelFactory,
        Func<JsonFormatterViewModel> jsonFormatterViewModelFactory,
        Func<JsonDiffViewModel> jsonDiffViewModelFactory,
        Func<Base64EncoderViewModel> base64EncoderViewModelFactory,
        Func<Base64DecoderViewModel> base64DecoderViewModelFactory,
        Func<RegexViewModel> regexViewModelFactory,
        Func<SettingsViewModel> settingsViewModelFactory,
        ILocalizationService localizationService)
        : base(errorHandler)
    {
        _applicationService = applicationService;
        _dashboardViewModel = dashboardViewModel;
        _createGuidViewModelFactory = createGuidViewModelFactory;
        _jwtDecoderViewModelFactory = jwtDecoderViewModelFactory;
        _jwtBuilderViewModelFactory = jwtBuilderViewModelFactory;
        _jsonFormatterViewModelFactory = jsonFormatterViewModelFactory;
        _jsonDiffViewModelFactory = jsonDiffViewModelFactory;
        _base64EncoderViewModelFactory = base64EncoderViewModelFactory;
        _base64DecoderViewModelFactory = base64DecoderViewModelFactory;
        _regexViewModelFactory = regexViewModelFactory;
        _settingsViewModelFactory = settingsViewModelFactory;
        _localizationService = localizationService;
        _localizationService.LanguageChanged += OnLanguageChanged;

        InitializeMenuItems();
        RefreshLocalizedText();

        // Open Dashboard tab on startup
        OpenDashboard();
    }

    private void InitializeMenuItems()
    {
        _dashboardItem = DashboardMenuItemViewModel.Create(GetMenuHeader(DashboardMenuItemViewModel.MenuId), OpenDashboardCommand);
        _createGuidItem = CreateGuidMenuItemViewModel.Create(GetMenuHeader(CreateGuidMenuItemViewModel.MenuId), OpenCreateGuidTabCommand);
        _jwtDecoderItem = JwtDecoderMenuItemViewModel.Create(GetMenuHeader(JwtDecoderMenuItemViewModel.MenuId), OpenJwtDecoderTabCommand);
        _jwtBuilderItem = JwtBuilderMenuItemViewModel.Create(GetMenuHeader(JwtBuilderMenuItemViewModel.MenuId), OpenJwtBuilderTabCommand);
        _jsonFormatterItem = JsonFormatterMenuItemViewModel.Create(GetMenuHeader(JsonFormatterMenuItemViewModel.MenuId), OpenJsonFormatterTabCommand);
        _jsonDiffItem = JsonDiffMenuItemViewModel.Create(GetMenuHeader(JsonDiffMenuItemViewModel.MenuId), OpenJsonDiffTabCommand);
        _base64EncoderItem = Base64EncoderMenuItemViewModel.Create(GetMenuHeader(Base64EncoderMenuItemViewModel.MenuId), OpenBase64EncoderTabCommand);
        _base64DecoderItem = Base64DecoderMenuItemViewModel.Create(GetMenuHeader(Base64DecoderMenuItemViewModel.MenuId), OpenBase64DecoderTabCommand);
        _regexItem = RegexMenuItemViewModel.Create(GetMenuHeader(RegexMenuItemViewModel.MenuId), OpenRegexTabCommand);
        _settingsItem = SettingsMenuItemViewModel.Create(GetMenuHeader(SettingsMenuItemViewModel.MenuId), OpenSettingsTabCommand);

        MenuItemViewModel[] primaryItems =
        [
            _dashboardItem, _createGuidItem, _jwtDecoderItem, _jwtBuilderItem,
            _jsonFormatterItem, _jsonDiffItem, _base64EncoderItem, _base64DecoderItem, _regexItem
        ];

        foreach (var item in primaryItems)
        {
            MenuItems.Add(item);
            _dashboardViewModel.MenuItems.Add(item);
        }

        BottomMenuItems.Add(_settingsItem);
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
    }

    [RelayCommand]
    private void OpenDashboard()
    {
        SetSelectedMenuItem("dashboard");
        OpenOrSelectTab("dashboard", GetTabHeader("dashboard"), _dashboardViewModel, false, "🎯");
    }

    [RelayCommand]
    private void OpenCreateGuidTab()
    {
        SetSelectedMenuItem("create-guid");
        var existingTab = Tabs.FirstOrDefault(t => t.Id == "create-guid");
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        var createGuidViewModel = _createGuidViewModelFactory();
        OpenOrSelectTab("create-guid", GetTabHeader("create-guid"), createGuidViewModel, true, "🎲");
    }

    [RelayCommand]
    private void OpenJwtDecoderTab()
    {
        SetSelectedMenuItem("jwt-decoder");
        var existingTab = Tabs.FirstOrDefault(t => t.Id == "jwt-decoder");
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        var jwtDecoderViewModel = _jwtDecoderViewModelFactory();
        OpenOrSelectTab("jwt-decoder", GetTabHeader("jwt-decoder"), jwtDecoderViewModel, true, "🔐");
    }

    [RelayCommand]
    private void OpenJwtBuilderTab()
    {
        SetSelectedMenuItem("jwt-builder");
        var existingTab = Tabs.FirstOrDefault(t => t.Id == "jwt-builder");
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        var jwtBuilderViewModel = _jwtBuilderViewModelFactory();
        OpenOrSelectTab("jwt-builder", GetTabHeader("jwt-builder"), jwtBuilderViewModel, true, "🔧");
    }

    [RelayCommand]
    private void OpenJsonFormatterTab()
    {
        SetSelectedMenuItem("json-formatter");
        var existingTab = Tabs.FirstOrDefault(t => t.Id == "json-formatter");
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        var jsonFormatterViewModel = _jsonFormatterViewModelFactory();
        OpenOrSelectTab("json-formatter", GetTabHeader("json-formatter"), jsonFormatterViewModel, true, "📋");
    }

    [RelayCommand]
    private void OpenJsonDiffTab()
    {
        SetSelectedMenuItem("json-diff");
        var existingTab = Tabs.FirstOrDefault(t => t.Id == "json-diff");
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        var jsonDiffViewModel = _jsonDiffViewModelFactory();
        OpenOrSelectTab("json-diff", GetTabHeader("json-diff"), jsonDiffViewModel, true, "🧩");
    }

    [RelayCommand]
    private void OpenBase64EncoderTab()
    {
        SetSelectedMenuItem("base64-encoder");
        var existingTab = Tabs.FirstOrDefault(t => t.Id == "base64-encoder");
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        var base64EncoderViewModel = _base64EncoderViewModelFactory();
        OpenOrSelectTab("base64-encoder", GetTabHeader("base64-encoder"), base64EncoderViewModel, true, "🧬");
    }

    [RelayCommand]
    private void OpenBase64DecoderTab()
    {
        SetSelectedMenuItem("base64-decoder");
        var existingTab = Tabs.FirstOrDefault(t => t.Id == "base64-decoder");
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        var base64DecoderViewModel = _base64DecoderViewModelFactory();
        OpenOrSelectTab("base64-decoder", GetTabHeader("base64-decoder"), base64DecoderViewModel, true, "🔓");
    }

    [RelayCommand]
    private void OpenRegexTab()
    {
        SetSelectedMenuItem("regex");
        var existingTab = Tabs.FirstOrDefault(t => t.Id == "regex");
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        var regexViewModel = _regexViewModelFactory();
        OpenOrSelectTab("regex", GetTabHeader("regex"), regexViewModel, true, "🔎");
    }

    [RelayCommand]
    private void OpenSettingsTab()
    {
        SetSelectedMenuItem("settings");
        var existingTab = Tabs.FirstOrDefault(t => t.Id == "settings");
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        var settingsViewModel = _settingsViewModelFactory();
        OpenOrSelectTab("settings", GetTabHeader("settings"), settingsViewModel, true, "⚙️");
    }

    private void SetSelectedMenuItem(string id)
    {
        foreach (var item in MenuItems)
        {
            item.IsSelected = item.Id == id;
        }

        foreach (var item in BottomMenuItems)
        {
            item.IsSelected = item.Id == id;
        }
    }

    partial void OnSelectedTabChanged(TabItemViewModel? value)
    {
        if (value == null)
        {
            SetSelectedMenuItem(string.Empty);
            return;
        }

        if (value.Id == "dashboard" || value.Id == "create-guid" || value.Id == "jwt-decoder" || value.Id == "jwt-builder" || value.Id == "json-formatter" || value.Id == "json-diff" || value.Id == "base64-encoder" || value.Id == "base64-decoder" || value.Id == "regex" || value.Id == "settings")
        {
            SetSelectedMenuItem(value.Id);
            return;
        }

        SetSelectedMenuItem(string.Empty);
    }

    [RelayCommand]
    private void CloseTab(TabItemViewModel tab)
    {
        if (tab == null || !tab.IsClosable) return;

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        // Switch to another tab if available
        if (Tabs.Count > 0)
        {
            SelectedTab = index < Tabs.Count ? Tabs[index] : Tabs[^1];
        }
        else
        {
            // Open Dashboard if no tabs remain
            OpenDashboard();
        }
    }

    private void OpenOrSelectTab(string id, string header, object content, bool isClosable, string? icon = null)
    {
        // Find existing tab
        var existingTab = Tabs.FirstOrDefault(t => t.Id == id);
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        // Create new tab
        var newTab = new TabItemViewModel
        {
            Id = id,
            Header = header,
            Content = content,
            IsClosable = isClosable,
            Icon = icon,
            Tooltip = header
        };

        Tabs.Add(newTab);
        SelectedTab = newTab;
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        RefreshLocalizedText();
    }

    private void RefreshLocalizedText()
    {
        Title = _localizationService.GetString("app.title");
        MenuTitle = _localizationService.GetString("main.menu");
        CloseTabTooltip = _localizationService.GetString("main.close_tab");

        foreach (var item in MenuItems.Concat(BottomMenuItems))
            item.RefreshHeader(_localizationService);

        foreach (var tab in Tabs)
        {
            tab.Header = GetTabHeader(tab.Id);
            tab.Tooltip = GetTabHeader(tab.Id);
        }
    }

    private string GetMenuHeader(string id)
    {
        return id switch
        {
            "dashboard" => _localizationService.GetString("menu.dashboard"),
            "create-guid" => _localizationService.GetString("menu.create_guid"),
            "jwt-decoder" => _localizationService.GetString("menu.jwt_decoder"),
            "jwt-builder" => _localizationService.GetString("menu.jwt_builder"),
            "json-formatter" => _localizationService.GetString("menu.json_formatter"),
            "json-diff" => _localizationService.GetString("menu.json_diff"),
            "base64-encoder" => _localizationService.GetString("menu.base64_encoder"),
            "base64-decoder" => _localizationService.GetString("menu.base64_decoder"),
            "regex" => _localizationService.GetString("menu.regex"),
            "settings" => _localizationService.GetString("menu.settings"),
            _ => id
        };
    }

    private string GetTabHeader(string id)
    {
        return GetMenuHeader(id);
    }

    /// <summary>
    /// Runs when ViewModel is initialized
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            await _applicationService.InitializeAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Başlatma hatası: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
