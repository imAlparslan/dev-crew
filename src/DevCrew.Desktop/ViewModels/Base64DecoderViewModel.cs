using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using DevCrew.Core.Models.Results;
using DevCrew.Core.ViewModels;
using DevCrew.Desktop.Services;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for Base64 decoding to binary file output.
/// </summary>
public partial class Base64DecoderViewModel : BaseViewModel
{
    private readonly IBase64EncoderService _base64EncoderService;
    private readonly ILocalizationService _localizationService;

    private byte[]? _decodedBytes;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedFile))]
    private string selectedFilePath = string.Empty;

    [ObservableProperty]
    private string selectedFileDisplayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasInput))]
    private string inputBase64 = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasDecodedOutput))]
    private long decodedByteCount;

    [ObservableProperty]
    private string detectedMimeType = string.Empty;

    [ObservableProperty]
    private string detectedExtension = string.Empty;

    [ObservableProperty]
    private string formattedFileSize = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isError;

    public bool HasSelectedFile => !string.IsNullOrWhiteSpace(SelectedFilePath);

    public bool HasInput => !string.IsNullOrWhiteSpace(InputBase64);

    public bool HasDecodedOutput => _decodedBytes != null && _decodedBytes.Length > 0;

    public Base64DecoderViewModel(
        IErrorHandler errorHandler,
        IBase64EncoderService base64EncoderService,
        ILocalizationService localizationService)
        : base(errorHandler)
    {
        _base64EncoderService = base64EncoderService;
        _localizationService = localizationService;
    }

    public async Task SetSelectedFileAsync(string filePath)
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

        _decodedBytes = null;
        DecodedByteCount = 0;
        OnPropertyChanged(nameof(HasDecodedOutput));

        try
        {
            InputBase64 = await File.ReadAllTextAsync(filePath, System.Text.Encoding.UTF8);
            StatusMessage = _localizationService.GetString("base64decoder.file_loaded", fileName);
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage = _localizationService.GetString("base64decoder.access_denied");
            IsError = true;
            return;
        }
        catch (IOException ex)
        {
            StatusMessage = _localizationService.GetString("base64decoder.file_read_failed", ex.Message);
            IsError = true;
            ErrorHandler.LogException(ex, "SetSelectedFile read");
            return;
        }

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
                StatusMessage = _localizationService.GetString("base64decoder.window_not_found");
                IsError = true;
                return;
            }

            var storageProvider = Avalonia.Controls.TopLevel.GetTopLevel(topLevel.MainWindow)?.StorageProvider;
            if (storageProvider is null)
            {
                StatusMessage = _localizationService.GetString("base64decoder.storage_not_available");
                IsError = true;
                return;
            }

            var suggestedLocation = await storageProvider.TryGetWellKnownFolderAsync(
                Avalonia.Platform.Storage.WellKnownFolder.Documents);

            var files = await storageProvider.OpenFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = _localizationService.GetString("base64decoder.open_dialog_title"),
                    AllowMultiple = false,
                    SuggestedStartLocation = suggestedLocation,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType(_localizationService.GetString("base64decoder.all_files")) { Patterns = new[] { "*" } }
                    }
                });

            if (files.Count > 0)
            {
                await SetSelectedFileAsync(files[0].Path.LocalPath);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = _localizationService.GetString("base64decoder.file_picker_failed", ex.Message);
            IsError = true;
            ErrorHandler.LogException(ex, "Browse Base64 decoder file");
        }
    }

    [RelayCommand]
    private async Task DecodeAsync()
    {
        if (!HasInput)
        {
            StatusMessage = _localizationService.GetString("base64decoder.input_required");
            IsError = true;
            return;
        }

        try
        {
            await Task.Yield();
            var result = _base64EncoderService.Decode(InputBase64);

            if (result.IsSuccess && result.Output != null)
            {
                _decodedBytes = result.Output;
                DecodedByteCount = result.Output.Length;
                var (mimeType, ext) = Services.FileTypeDetector.Detect(result.Output);
                DetectedMimeType = mimeType;
                DetectedExtension = $".{ext}";
                FormattedFileSize = Services.FileTypeDetector.FormatSize(result.Output.Length);
                OnPropertyChanged(nameof(HasDecodedOutput));
                StatusMessage = _localizationService.GetString("base64decoder.completed_status", FormattedFileSize);
                IsError = false;
            }
            else
            {
                ClearDecodedOutput();
                StatusMessage = _localizationService.GetStringOrFallback(
                    result.ErrorKey,
                    result.ErrorMessage ?? _localizationService.GetString("common.error_unknown"),
                    result.ErrorArgs ?? []);
                IsError = true;
            }
        }
        catch (Exception ex)
        {
            ClearDecodedOutput();
            StatusMessage = $"Cozme hatasi: {ex.Message}";
            IsError = true;
            ErrorHandler.LogException(ex, "Decode Base64");
        }
    }

    [RelayCommand]
    private async Task SaveBinaryAsync()
    {
        if (_decodedBytes == null || _decodedBytes.Length == 0)
        {
            return;
        }

        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (topLevel?.MainWindow is null)
            {
                StatusMessage = _localizationService.GetString("base64decoder.window_not_found");
                IsError = true;
                return;
            }

            var storageProvider = Avalonia.Controls.TopLevel.GetTopLevel(topLevel.MainWindow)?.StorageProvider;
            if (storageProvider is null)
            {
                StatusMessage = _localizationService.GetString("base64decoder.storage_not_available");
                IsError = true;
                return;
            }

            var suggestedLocation = await storageProvider.TryGetWellKnownFolderAsync(
                Avalonia.Platform.Storage.WellKnownFolder.Documents);

            var file = await storageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = _localizationService.GetString("base64decoder.save_dialog_title"),
                    SuggestedFileName = string.IsNullOrEmpty(DetectedExtension)
                        ? "decoded_output"
                        : $"decoded_output{DetectedExtension}",
                    DefaultExtension = DetectedExtension.TrimStart('.'),
                    SuggestedStartLocation = suggestedLocation
                });

            if (file is not null)
            {
                try
                {
                    await using var stream = await file.OpenWriteAsync();
                    await stream.WriteAsync(_decodedBytes);
                    StatusMessage = _localizationService.GetString("base64decoder.file_saved");
                    IsError = false;
                }
                catch (Exception ex)
                {
                    StatusMessage = _localizationService.GetString("base64decoder.save_failed", ex.Message);
                    IsError = true;
                    ErrorHandler.LogException(ex, "SaveBinary");
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = _localizationService.GetString("base64decoder.file_picker_failed", ex.Message);
            IsError = true;
            ErrorHandler.LogException(ex, "SaveBinary");
        }
    }

    [RelayCommand]
    private void Clear()
    {
        SelectedFilePath = string.Empty;
        SelectedFileDisplayName = string.Empty;
        InputBase64 = string.Empty;
        ClearDecodedOutput();
        StatusMessage = string.Empty;
        IsError = false;
    }

    private void ClearDecodedOutput()
    {
        _decodedBytes = null;
        DecodedByteCount = 0;
        DetectedMimeType = string.Empty;
        DetectedExtension = string.Empty;
        FormattedFileSize = string.Empty;
        OnPropertyChanged(nameof(HasDecodedOutput));
    }
}
