using System;
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
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGuid))]
    private string currentGuid = string.Empty;
    /// <summary>
    /// Indicates whether a GUID value is available.
    /// </summary>
    public bool HasGuid => !string.IsNullOrWhiteSpace(CurrentGuid);

    [ObservableProperty]
    private bool isGuidCopied;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateGuidViewModel"/> class.
    /// </summary>
    /// <param name="guidService">GUID generation service.</param>
    /// <param name="clipboardService">Clipboard access service.</param>
    public CreateGuidViewModel(IGuidService guidService, IClipboardService clipboardService)
    {
        _guidService = guidService;
        _clipboardService = clipboardService;
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
            if (!string.IsNullOrWhiteSpace(CurrentGuid))
            {
                IsGuidCopied = await _clipboardService.TrySetTextAsync(CurrentGuid);
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Clipboard error: {ex.Message}");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        CurrentGuid = string.Empty;
    }

    partial void OnCurrentGuidChanged(string value)
    {
        IsGuidCopied = false;
    }
}
