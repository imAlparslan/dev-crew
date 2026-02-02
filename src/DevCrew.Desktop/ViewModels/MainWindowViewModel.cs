using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// Ana pencere ViewModel'i
/// </summary>
public partial class MainWindowViewModel : BaseViewModel
{
    private readonly IApplicationService _applicationService;
    private readonly DashboardViewModel _dashboardViewModel;
    private readonly Func<CreateGuidViewModel> _createGuidViewModelFactory;

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
    /// <param name="applicationService">Application service.</param>
    /// <param name="dashboardViewModel">Dashboard view model.</param>
    /// <param name="createGuidViewModelFactory">Factory for new GUID view models.</param>
    public MainWindowViewModel(
        IApplicationService applicationService,
        DashboardViewModel dashboardViewModel,
        Func<CreateGuidViewModel> createGuidViewModelFactory)
    {
        _applicationService = applicationService;
        _dashboardViewModel = dashboardViewModel;
        _createGuidViewModelFactory = createGuidViewModelFactory;

        InitializeMenuItems();

        // Open Dashboard tab on startup
        OpenDashboard();
    }

    private void InitializeMenuItems()
    {
        var dashboardItem = new MenuItemViewModel("dashboard", "Dashboard", OpenDashboardCommand, "Primary", "🎯");
        var createGuidItem = new MenuItemViewModel("create-guid", "Create Guid", OpenCreateGuidTabCommand, "Primary", "🎲");

        MenuItems.Add(dashboardItem);
        MenuItems.Add(createGuidItem);

        // Dashboard'daki MenuItems'ı doldur
        _dashboardViewModel.MenuItems.Add(dashboardItem);
        _dashboardViewModel.MenuItems.Add(createGuidItem);
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

        if (value.Id == "dashboard" || value.Id == "create-guid")
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
