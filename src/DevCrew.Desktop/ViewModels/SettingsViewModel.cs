using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the settings view.
/// </summary>
public class SettingsViewModel : BaseViewModel
{
    public SettingsViewModel(IErrorHandler errorHandler)
        : base(errorHandler)
    {
    }

    public string Description => "Ayarlar konfigurasyonlari bu sayfaya eklenecek.";
}
