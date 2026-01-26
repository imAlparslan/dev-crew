using System.Threading.Tasks;
using Avalonia.Controls.ApplicationLifetimes;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the GUID creation view
/// Each tab instance maintains its own state
/// </summary>
public partial class CreateGuidViewModel : ObservableObject
{
    private readonly IGuidService _guidService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGuid))]
    private string currentGuid = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasStatusMessage))]
    private string statusMessage = string.Empty;

    public bool HasGuid => !string.IsNullOrWhiteSpace(CurrentGuid);
    public bool HasStatusMessage => !string.IsNullOrWhiteSpace(StatusMessage);

    public CreateGuidViewModel(IGuidService guidService)
    {
        _guidService = guidService;
    }

    [RelayCommand]
    private void GenerateGuid()
    {
        CurrentGuid = _guidService.Generate();
        ShowStatusMessage("New GUID created!");
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
                ShowStatusMessage("Copied to clipboard!");
            }
        }
        catch (Exception)
        {
            ShowStatusMessage("Copy failed!");
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
        
        // Clear message after 3 seconds
        await Task.Delay(3000);
        StatusMessage = string.Empty;
    }
}
