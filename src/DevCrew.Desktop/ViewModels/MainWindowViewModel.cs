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

    [ObservableProperty]
    private string title = "DevCrew";

    [ObservableProperty]
    private string statusMessage = "Uygulamaya hoş geldiniz";

    [ObservableProperty]
    private bool isSidebarOpen = true;

    [ObservableProperty]
    private TabItemViewModel? selectedTab;

    public ObservableCollection<TabItemViewModel> Tabs { get; } = new();

    public MainWindowViewModel(IApplicationService applicationService)
    {
        _applicationService = applicationService;
        
        // Başlangıçta Dashboard tab'ını aç
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
        OpenOrSelectTab("dashboard", "Dashboard", new DashboardViewModel(), false, "🎯");
    }

    [RelayCommand]
    private void OpenCreateGuidTab()
    {
        OpenOrSelectTab("create-guid", "Create Guid", new CreateGuidViewModel(), true, "🎲");
    }

    [RelayCommand]
    private async Task CloseTab(TabItemViewModel tab)
    {
        if (tab == null || !tab.IsClosable) return;

        // Kaydedilmemiş değişiklikler varsa onay iste
        if (tab.HasUnsavedChanges)
        {
            // TODO: Dialog implementation gerekli - şimdilik direkt kapat
            // var result = await ShowConfirmationDialog("Kaydedilmemiş değişiklikler var. Devam etmek istiyor musunuz?");
            // if (!result) return;
        }

        var index = Tabs.IndexOf(tab);
        Tabs.Remove(tab);

        // Başka tab varsa ona geç
        if (Tabs.Count > 0)
        {
            SelectedTab = index < Tabs.Count ? Tabs[index] : Tabs[^1];
        }
        else
        {
            // Hiç tab kalmadıysa Dashboard'u aç
            OpenDashboard();
        }
    }

    private void OpenOrSelectTab(string id, string header, object content, bool isClosable, string? icon = null)
    {
        // Mevcut tab'ı bul
        var existingTab = Tabs.FirstOrDefault(t => t.Id == id);
        if (existingTab != null)
        {
            SelectedTab = existingTab;
            return;
        }

        // Yeni tab oluştur
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
    /// ViewModel başlatıldığında çalışır
    /// </summary>
    public async Task InitializeAsync()
    {
        try
        {
            IsLoading = true;
            await _applicationService.InitializeAsync();
            StatusMessage = "Uygulama başarıyla başlatıldı";
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Başlatma hatası: {ex.Message}";
            StatusMessage = "Hata oluştu";
        }
        finally
        {
            IsLoading = false;
        }
    }
}
