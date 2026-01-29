using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.ViewModels;
using DevCrew.Core.Services;

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

    public ObservableCollection<TabItemViewModel> Tabs { get; } = new();

    public MainWindowViewModel(
        IApplicationService applicationService,
        DashboardViewModel dashboardViewModel,
        Func<CreateGuidViewModel> createGuidViewModelFactory)
    {
        _applicationService = applicationService;
        _dashboardViewModel = dashboardViewModel;
        _createGuidViewModelFactory = createGuidViewModelFactory;
        
        // Open Dashboard tab on startup
        OpenDashboard();
    }

    [RelayCommand]
    private void ToggleSidebar()
    {
        IsSidebarOpen = !IsSidebarOpen;
    }

    [RelayCommand]
    private void OpenDashboard()
    {
        OpenOrSelectTab("dashboard", "Dashboard", _dashboardViewModel, false, "🎯");
    }

    [RelayCommand]
    private void OpenCreateGuidTab()
    {
        var createGuidViewModel = _createGuidViewModelFactory();
        OpenOrSelectTab("create-guid", "Create Guid", createGuidViewModel, true, "🎲");
    }

    [RelayCommand]
    private async Task CloseTab(TabItemViewModel tab)
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
