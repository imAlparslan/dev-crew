using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;
using DevCrew.Desktop.Services;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for Base64 encoding from file input.
/// </summary>
public partial class Base64EncoderViewModel : BaseViewModel
{
    private const int PreviewCharacterLimit = 120_000;

    private readonly IBase64EncoderService _base64EncoderService;
    private readonly IClipboardService _clipboardService;
    private readonly ILocalizationService _localizationService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedFile))]
    private string selectedFilePath = string.Empty;

    [ObservableProperty]
    private string selectedFileDisplayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutput))]
    private string fullOutputBase64 = string.Empty;

    [ObservableProperty]
    private string outputBase64 = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowFullOutput))]
    private bool isPreviewMode;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanShowFullOutput))]
    private bool isFullOutputVisible;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isError;

    public bool HasSelectedFile => !string.IsNullOrWhiteSpace(SelectedFilePath);

    public bool HasOutput => !string.IsNullOrWhiteSpace(FullOutputBase64);

    public bool CanShowFullOutput => IsPreviewMode && !IsFullOutputVisible;

    public Base64EncoderViewModel(
        IErrorHandler errorHandler,
        IBase64EncoderService base64EncoderService,
        IClipboardService clipboardService,
        ILocalizationService localizationService)
        : base(errorHandler)
    {
        _base64EncoderService = base64EncoderService;
        _clipboardService = clipboardService;
        _localizationService = localizationService;
    }

    public void SetSelectedFile(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            return;
        }

        SelectedFilePath = filePath;
        var fileName = Path.GetFileName(filePath);
        var extension = Path.GetExtension(filePath);

        SelectedFileDisplayName = string.IsNullOrWhiteSpace(extension)
            ? fileName
            : $"{fileName} ({extension})";

        StatusMessage = _localizationService.GetString("base64encoder.file_selected", fileName);
        IsError = false;
    }

    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (topLevel?.MainWindow is null)
            {
                StatusMessage = _localizationService.GetString("base64encoder.window_not_found");
                IsError = true;
                return;
            }

            var storageProvider = Avalonia.Controls.TopLevel.GetTopLevel(topLevel.MainWindow)?.StorageProvider;
            if (storageProvider is null)
            {
                StatusMessage = _localizationService.GetString("base64encoder.storage_not_available");
                IsError = true;
                return;
            }

            var suggestedLocation = await storageProvider.TryGetWellKnownFolderAsync(
                Avalonia.Platform.Storage.WellKnownFolder.Documents);

            var files = await storageProvider.OpenFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = _localizationService.GetString("base64encoder.open_dialog_title"),
                    AllowMultiple = false,
                    SuggestedStartLocation = suggestedLocation,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType(_localizationService.GetString("base64encoder.all_files")) { Patterns = new[] { "*" } }
                    }
                });

            if (files.Count > 0)
            {
                SetSelectedFile(files[0].Path.LocalPath);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = _localizationService.GetString("base64encoder.file_select_failed", ex.Message);
            IsError = true;
            ErrorHandler.LogException(ex, "Browse Base64 file");
        }
    }

    [RelayCommand]
    private async Task EncodeAsync()
    {
        if (!HasSelectedFile)
        {
            StatusMessage = _localizationService.GetString("base64encoder.select_file_first");
            IsError = true;
            return;
        }

        try
        {
            if (!File.Exists(SelectedFilePath))
            {
                StatusMessage = _localizationService.GetString("base64encoder.selected_file_missing");
                IsError = true;
                return;
            }

            var fileBytes = await File.ReadAllBytesAsync(SelectedFilePath);
            var result = _base64EncoderService.Encode(fileBytes);

            if (result.IsSuccess)
            {
                SetOutput(result.Output);
                IsError = false;
            }
            else
            {
                ClearOutput();
                StatusMessage = _localizationService.GetStringOrFallback(
                    result.ErrorKey,
                    result.ErrorMessage ?? _localizationService.GetString("common.error_unknown"),
                    result.ErrorArgs ?? []);
                IsError = true;
            }
        }
        catch (UnauthorizedAccessException)
        {
            ClearOutput();
            StatusMessage = _localizationService.GetString("base64encoder.access_denied");
            IsError = true;
        }
        catch (IOException ex)
        {
            ClearOutput();
            StatusMessage = _localizationService.GetString("base64encoder.file_read_failed", ex.Message);
            IsError = true;
            ErrorHandler.LogException(ex, "Read Base64 file");
        }
        catch (Exception ex)
        {
            ClearOutput();
            StatusMessage = _localizationService.GetString("base64encoder.encode_failed", ex.Message);
            IsError = true;
            ErrorHandler.LogException(ex, "Encode Base64 file");
        }
    }

    [RelayCommand]
    private void ShowFullOutput()
    {
        if (!HasOutput || IsFullOutputVisible)
        {
            return;
        }

        OutputBase64 = FullOutputBase64;
        IsFullOutputVisible = true;
        StatusMessage = _localizationService.GetString("base64encoder.showing_full_output", FullOutputBase64.Length);
        IsError = false;
    }

    [RelayCommand]
    private async Task CopyOutputAsync()
    {
        if (HasOutput)
        {
            await _clipboardService.TrySetTextAsync(FullOutputBase64);
        }
    }

    private void SetOutput(string output)
    {
        FullOutputBase64 = output;
        IsFullOutputVisible = false;

        if (string.IsNullOrWhiteSpace(output))
        {
            OutputBase64 = string.Empty;
            IsPreviewMode = false;
            return;
        }

        if (output.Length > PreviewCharacterLimit)
        {
            OutputBase64 = output[..PreviewCharacterLimit];
            IsPreviewMode = true;
            StatusMessage = _localizationService.GetString("base64encoder.preview_status", PreviewCharacterLimit, output.Length);
            return;
        }

        OutputBase64 = output;
        IsPreviewMode = false;
        StatusMessage = _localizationService.GetString("base64encoder.completed_status", output.Length);
    }

    private void ClearOutput()
    {
        FullOutputBase64 = string.Empty;
        OutputBase64 = string.Empty;
        IsPreviewMode = false;
        IsFullOutputVisible = false;
    }
}