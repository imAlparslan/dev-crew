using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;

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

    [ObservableProperty]
    private string title = "DevCrew";

    [ObservableProperty]
    private bool isSidebarOpen = true;

    [ObservableProperty]
    private TabItemViewModel? selectedTab;

    /// <summary>
    /// Sidebar menu items.
    /// </summary>
    public ObservableCollection<MenuItemViewModel> MenuItems { get; } = new();

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
        Func<Base64DecoderViewModel> base64DecoderViewModelFactory)
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

        InitializeMenuItems();

        // Open Dashboard tab on startup
        OpenDashboard();
    }

    private void InitializeMenuItems()
    {
        var dashboardItem = new MenuItemViewModel("dashboard", "Dashboard", OpenDashboardCommand, "Primary", "🎯");
        var createGuidItem = new MenuItemViewModel("create-guid", "Create Guid", OpenCreateGuidTabCommand, "Primary", "🎲");
        var jwtDecoderItem = new MenuItemViewModel("jwt-decoder", "JWT Decoder", OpenJwtDecoderTabCommand, "Primary", "🔐");
        var jwtBuilderItem = new MenuItemViewModel("jwt-builder", "JWT Builder", OpenJwtBuilderTabCommand, "Primary", "🔧");
        var jsonFormatterItem = new MenuItemViewModel("json-formatter", "JSON Formatter", OpenJsonFormatterTabCommand, "Primary", "📋");
        var jsonDiffItem = new MenuItemViewModel("json-diff", "JSON Diff", OpenJsonDiffTabCommand, "Primary", "🧩");
        var base64EncoderItem = new MenuItemViewModel("base64-encoder", "Base64 Encoder", OpenBase64EncoderTabCommand, "Primary", "🧬");
        var base64DecoderItem = new MenuItemViewModel("base64-decoder", "Base64 Decoder", OpenBase64DecoderTabCommand, "Primary", "🔓");

        MenuItems.Add(dashboardItem);
        MenuItems.Add(createGuidItem);
        MenuItems.Add(jwtDecoderItem);
        MenuItems.Add(jwtBuilderItem);
        MenuItems.Add(jsonFormatterItem);
        MenuItems.Add(jsonDiffItem);
        MenuItems.Add(base64EncoderItem);
        MenuItems.Add(base64DecoderItem);

        // Populate Dashboard MenuItems
        _dashboardViewModel.MenuItems.Add(dashboardItem);
        _dashboardViewModel.MenuItems.Add(createGuidItem);
        _dashboardViewModel.MenuItems.Add(jwtDecoderItem);
        _dashboardViewModel.MenuItems.Add(jwtBuilderItem);
        _dashboardViewModel.MenuItems.Add(jsonFormatterItem);
        _dashboardViewModel.MenuItems.Add(jsonDiffItem);
        _dashboardViewModel.MenuItems.Add(base64EncoderItem);
        _dashboardViewModel.MenuItems.Add(base64DecoderItem);
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
        OpenOrSelectTab("dashboard", "Dashboard", _dashboardViewModel, false, "🎯");
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
        OpenOrSelectTab("create-guid", "Create Guid", createGuidViewModel, true, "🎲");
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
        OpenOrSelectTab("jwt-decoder", "JWT Decoder", jwtDecoderViewModel, true, "🔐");
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
        OpenOrSelectTab("jwt-builder", "JWT Builder", jwtBuilderViewModel, true, "🔧");
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
        OpenOrSelectTab("json-formatter", "JSON Formatter", jsonFormatterViewModel, true, "📋");
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
        OpenOrSelectTab("json-diff", "JSON Diff", jsonDiffViewModel, true, "🧩");
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
        OpenOrSelectTab("base64-encoder", "Base64 Encoder", base64EncoderViewModel, true, "🧬");
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
        OpenOrSelectTab("base64-decoder", "Base64 Decoder", base64DecoderViewModel, true, "🔓");
    }

    private void SetSelectedMenuItem(string id)
    {
        foreach (var item in MenuItems)
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

        if (value.Id == "dashboard" || value.Id == "create-guid" || value.Id == "jwt-decoder" || value.Id == "jwt-builder" || value.Id == "json-formatter" || value.Id == "json-diff" || value.Id == "base64-encoder" || value.Id == "base64-decoder")
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
