using CommunityToolkit.Mvvm.ComponentModel;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// Dashboard görünümü için ViewModel
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string welcomeMessage = "DevCrew uygulamasına hoş geldiniz!";
}
