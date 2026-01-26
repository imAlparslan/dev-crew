using CommunityToolkit.Mvvm.ComponentModel;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the Dashboard view
/// </summary>
public partial class DashboardViewModel : ObservableObject
{
    [ObservableProperty]
    private string welcomeMessage = "DevCrew uygulamasına hoş geldiniz!";
}
