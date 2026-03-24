using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;
using DevCrew.Desktop.Services;

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
    private readonly Func<SettingsViewModel> _settingsViewModelFactory;
    private readonly ILocalizationService _localizationService;

    private MenuItemViewModel? _dashboardItem;
    private MenuItemViewModel? _createGuidItem;
    private MenuItemViewModel? _jwtDecoderItem;
    private MenuItemViewModel? _jwtBuilderItem;
    private MenuItemViewModel? _jsonFormatterItem;
    private MenuItemViewModel? _jsonDiffItem;
    private MenuItemViewModel? _base64EncoderItem;
    private MenuItemViewModel? _base64DecoderItem;
    private MenuItemViewModel? _settingsItem;

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
        _dashboardItem = new MenuItemViewModel("dashboard", GetMenuHeader("dashboard"), OpenDashboardCommand, "Primary", "🎯");
        _createGuidItem = new MenuItemViewModel("create-guid", GetMenuHeader("create-guid"), OpenCreateGuidTabCommand, "Primary", "🎲");
        _jwtDecoderItem = new MenuItemViewModel("jwt-decoder", GetMenuHeader("jwt-decoder"), OpenJwtDecoderTabCommand, "Primary", "🔐");
        _jwtBuilderItem = new MenuItemViewModel("jwt-builder", GetMenuHeader("jwt-builder"), OpenJwtBuilderTabCommand, "Primary", "🔧");
        _jsonFormatterItem = new MenuItemViewModel("json-formatter", GetMenuHeader("json-formatter"), OpenJsonFormatterTabCommand, "Primary", "📋");
        _jsonDiffItem = new MenuItemViewModel("json-diff", GetMenuHeader("json-diff"), OpenJsonDiffTabCommand, "Primary", "🧩");
        _base64EncoderItem = new MenuItemViewModel("base64-encoder", GetMenuHeader("base64-encoder"), OpenBase64EncoderTabCommand, "Primary", "🧬");
        _base64DecoderItem = new MenuItemViewModel("base64-decoder", GetMenuHeader("base64-decoder"), OpenBase64DecoderTabCommand, "Primary", "🔓");
        _settingsItem = new MenuItemViewModel("settings", GetMenuHeader("settings"), OpenSettingsTabCommand, "Secondary", "⚙️");

        MenuItems.Add(_dashboardItem);
        MenuItems.Add(_createGuidItem);
        MenuItems.Add(_jwtDecoderItem);
        MenuItems.Add(_jwtBuilderItem);
        MenuItems.Add(_jsonFormatterItem);
        MenuItems.Add(_jsonDiffItem);
        MenuItems.Add(_base64EncoderItem);
        MenuItems.Add(_base64DecoderItem);

        BottomMenuItems.Add(_settingsItem);

        // Populate Dashboard MenuItems
        _dashboardViewModel.MenuItems.Add(_dashboardItem);
        _dashboardViewModel.MenuItems.Add(_createGuidItem);
        _dashboardViewModel.MenuItems.Add(_jwtDecoderItem);
        _dashboardViewModel.MenuItems.Add(_jwtBuilderItem);
        _dashboardViewModel.MenuItems.Add(_jsonFormatterItem);
        _dashboardViewModel.MenuItems.Add(_jsonDiffItem);
        _dashboardViewModel.MenuItems.Add(_base64EncoderItem);
        _dashboardViewModel.MenuItems.Add(_base64DecoderItem);
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

        if (value.Id == "dashboard" || value.Id == "create-guid" || value.Id == "jwt-decoder" || value.Id == "jwt-builder" || value.Id == "json-formatter" || value.Id == "json-diff" || value.Id == "base64-encoder" || value.Id == "base64-decoder" || value.Id == "settings")
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

        if (_dashboardItem != null) _dashboardItem.Header = GetMenuHeader("dashboard");
        if (_createGuidItem != null) _createGuidItem.Header = GetMenuHeader("create-guid");
        if (_jwtDecoderItem != null) _jwtDecoderItem.Header = GetMenuHeader("jwt-decoder");
        if (_jwtBuilderItem != null) _jwtBuilderItem.Header = GetMenuHeader("jwt-builder");
        if (_jsonFormatterItem != null) _jsonFormatterItem.Header = GetMenuHeader("json-formatter");
        if (_jsonDiffItem != null) _jsonDiffItem.Header = GetMenuHeader("json-diff");
        if (_base64EncoderItem != null) _base64EncoderItem.Header = GetMenuHeader("base64-encoder");
        if (_base64DecoderItem != null) _base64DecoderItem.Header = GetMenuHeader("base64-decoder");
        if (_settingsItem != null) _settingsItem.Header = GetMenuHeader("settings");

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
