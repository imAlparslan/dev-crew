using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for Base64 encoding from file input.
/// </summary>
public partial class Base64EncoderViewModel : BaseViewModel
{
    private readonly IBase64EncoderService _base64EncoderService;
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasSelectedFile))]
    private string selectedFilePath = string.Empty;

    [ObservableProperty]
    private string selectedFileDisplayName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasOutput))]
    private string outputBase64 = string.Empty;

    [ObservableProperty]
    private string statusMessage = string.Empty;

    [ObservableProperty]
    private bool isError;

    public bool HasSelectedFile => !string.IsNullOrWhiteSpace(SelectedFilePath);

    public bool HasOutput => !string.IsNullOrWhiteSpace(OutputBase64);

    public Base64EncoderViewModel(
        IErrorHandler errorHandler,
        IBase64EncoderService base64EncoderService,
        IClipboardService clipboardService)
        : base(errorHandler)
    {
        _base64EncoderService = base64EncoderService;
        _clipboardService = clipboardService;
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

        StatusMessage = $"Dosya secildi: {fileName}";
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
                    Title = "Dosya sec",
                    AllowMultiple = false,
                    SuggestedStartLocation = suggestedLocation,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType("Tum Dosyalar") { Patterns = new[] { "*" } }
                    }
                });

            if (files.Count > 0)
            {
                SetSelectedFile(files[0].Path.LocalPath);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"Dosya secme hatasi: {ex.Message}";
            IsError = true;
            ErrorHandler.LogException(ex, "Browse Base64 file");
        }
    }

    [RelayCommand]
    private async Task EncodeAsync()
    {
        if (!HasSelectedFile)
        {
            StatusMessage = "Lutfen once bir dosya secin";
            IsError = true;
            return;
        }

        try
        {
            if (!File.Exists(SelectedFilePath))
            {
                StatusMessage = "Secilen dosya bulunamadi";
                IsError = true;
                return;
            }

            var fileBytes = await File.ReadAllBytesAsync(SelectedFilePath);
            var result = _base64EncoderService.Encode(fileBytes);

            if (result.IsSuccess)
            {
                OutputBase64 = result.Output;
                StatusMessage = "Dosya Base64 formatina cevrildi";
                IsError = false;
            }
            else
            {
                OutputBase64 = string.Empty;
                StatusMessage = result.ErrorMessage ?? "Encoding hatasi";
                IsError = true;
            }
        }
        catch (UnauthorizedAccessException)
        {
            OutputBase64 = string.Empty;
            StatusMessage = "Dosyaya erisim reddedildi";
            IsError = true;
        }
        catch (IOException ex)
        {
            OutputBase64 = string.Empty;
            StatusMessage = $"Dosya okuma hatasi: {ex.Message}";
            IsError = true;
            ErrorHandler.LogException(ex, "Read Base64 file");
        }
        catch (Exception ex)
        {
            OutputBase64 = string.Empty;
            StatusMessage = $"Encoding hatasi: {ex.Message}";
            IsError = true;
            ErrorHandler.LogException(ex, "Encode Base64 file");
        }
    }

    [RelayCommand]
    private async Task CopyOutputAsync()
    {
        if (HasOutput)
        {
            await _clipboardService.TrySetTextAsync(OutputBase64);
        }
    }
}