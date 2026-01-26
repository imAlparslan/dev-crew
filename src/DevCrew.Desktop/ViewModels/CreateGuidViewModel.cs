using System;
using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// GUID oluşturma görünümü için ViewModel
/// Her tab instance'ının kendi state'ini tutar
/// </summary>
public partial class CreateGuidViewModel : ObservableObject
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGuid))]
    private string currentGuid = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string statusMessage = string.Empty;

    public bool HasGuid => !string.IsNullOrWhiteSpace(CurrentGuid);
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    [RelayCommand]
    private void GenerateGuid()
    {
        CurrentGuid = Guid.NewGuid().ToString();
        ShowStatusMessage("Yeni GUID oluşturuldu!");
    }

    [RelayCommand]
    private async Task CopyToClipboard()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
                ? desktop.MainWindow
                : null;

            if (topLevel?.Clipboard != null && !string.IsNullOrWhiteSpace(CurrentGuid))
            {
                await topLevel.Clipboard.SetTextAsync(CurrentGuid);
                ShowStatusMessage("Panoya kopyalandı!");
            }
        }
        catch (Exception)
        {
            ShowStatusMessage("Kopyalama başarısız!");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CurrentGuid = string.Empty;
        StatusMessage = string.Empty;
    }

    private async void ShowStatusMessage(string message)
    {
        StatusMessage = message;
        
        // 3 saniye sonra mesajı temizle
        await Task.Delay(3000);
        StatusMessage = string.Empty;
    }
}
