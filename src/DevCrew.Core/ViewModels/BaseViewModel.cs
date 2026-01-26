using CommunityToolkit.Mvvm.ComponentModel;

namespace DevCrew.Core.ViewModels;

/// <summary>
/// Temel ViewModel sınıfı
/// </summary>
public abstract class BaseViewModel : ObservableObject
{
    private bool _isLoading;
    private string? _errorMessage;

    /// <summary>
    /// Yüklenme durumu
    /// </summary>
    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    /// <summary>
    /// Hata mesajı
    /// </summary>
    public string? ErrorMessage
    {
        get => _errorMessage;
        set => SetProperty(ref _errorMessage, value);
    }

    /// <summary>
    /// Hata durumunu temizle
    /// </summary>
    public void ClearError()
    {
        ErrorMessage = null;
    }
}
