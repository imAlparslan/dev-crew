using CommunityToolkit.Mvvm.ComponentModel;
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

    public MainWindowViewModel(IApplicationService applicationService)
    {
        _applicationService = applicationService;
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
