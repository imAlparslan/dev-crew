using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for Base64 decoding to binary file output.
/// </summary>
public partial class Base64DecoderViewModel : BaseViewModel
{
    private readonly IBase64EncoderService _base64EncoderService;

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
        IBase64EncoderService base64EncoderService)
        : base(errorHandler)
    {
        _base64EncoderService = base64EncoderService;
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
            StatusMessage = $"Dosya yuklendi: {fileName}";
        }
        catch (UnauthorizedAccessException)
        {
            StatusMessage = "Dosyaya erisim reddedildi";
            IsError = true;
            return;
        }
        catch (IOException ex)
        {
            StatusMessage = $"Dosya okuma hatasi: {ex.Message}";
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
                StatusMessage = "Ana pencere bulunamadi";
                IsError = true;
                return;
            }

            var storageProvider = Avalonia.Controls.TopLevel.GetTopLevel(topLevel.MainWindow)?.StorageProvider;
            if (storageProvider is null)
            {
                StatusMessage = "Depolama saglayicisi baslatilamadi";
                IsError = true;
                return;
            }

            var suggestedLocation = await storageProvider.TryGetWellKnownFolderAsync(
                Avalonia.Platform.Storage.WellKnownFolder.Documents);

            var files = await storageProvider.OpenFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = "Base64 dosyasi sec",
                    AllowMultiple = false,
                    SuggestedStartLocation = suggestedLocation,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Tum Dosyalar") { Patterns = new[] { "*" } }
                    }
                });

            if (files.Count > 0)
            {
                await SetSelectedFileAsync(files[0].Path.LocalPath);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Dosya secme hatasi: {ex.Message}";
            IsError = true;
            ErrorHandler.LogException(ex, "Browse Base64 decoder file");
        }
    }

    [RelayCommand]
    private async Task DecodeAsync()
    {
        if (!HasInput)
        {
            StatusMessage = "Lutfen Base64 icerigi girin veya bir dosya secin";
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
                StatusMessage = $"Basariyla cozuldu ({FormattedFileSize})";
                IsError = false;
            }
            else
            {
                ClearDecodedOutput();
                StatusMessage = result.ErrorMessage ?? "Cozme hatasi";
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
                StatusMessage = "Ana pencere bulunamadi";
                IsError = true;
                return;
            }

            var storageProvider = Avalonia.Controls.TopLevel.GetTopLevel(topLevel.MainWindow)?.StorageProvider;
            if (storageProvider is null)
            {
                StatusMessage = "Depolama saglayicisi baslatilamadi";
                IsError = true;
                return;
            }

            var suggestedLocation = await storageProvider.TryGetWellKnownFolderAsync(
                Avalonia.Platform.Storage.WellKnownFolder.Documents);

            var file = await storageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = "Cozulmus dosyayi kaydet",
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
                    StatusMessage = "Dosya basariyla kaydedildi!";
                    IsError = false;
                }
                catch (Exception ex)
                {
                    StatusMessage = $"Dosya kaydedilirken hata: {ex.Message}";
                    IsError = true;
                    ErrorHandler.LogException(ex, "SaveBinary");
                }
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Dosya secimi sirasinda hata: {ex.Message}";
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
