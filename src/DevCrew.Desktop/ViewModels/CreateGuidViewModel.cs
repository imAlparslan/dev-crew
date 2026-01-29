using System.Threading.Tasks;
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
    public bool HasGuid => !string.IsNullOrWhiteSpace(CurrentGuid);

    public CreateGuidViewModel(IGuidService guidService)
    {
        _guidService = guidService;
    }

    [RelayCommand]
    private void GenerateGuid()
    {
        CurrentGuid = _guidService.Generate();
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
            }
        }
        catch (Exception)
        {
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CurrentGuid = string.Empty;
    }
}
