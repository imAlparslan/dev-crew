using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;
using DevCrew.Desktop.Services;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the JSON Formatter view
/// </summary>
public partial class JsonFormatterViewModel : BaseViewModel
{
    private readonly IJsonFormatterService _jsonFormatterService;
    private readonly IClipboardService _clipboardService;
    private readonly ILocalizationService _localizationService;
    private string _lastFormatMode = "prettify"; // Track whether user used prettify or minify

    [ObservableProperty]
    private string inputJson = string.Empty;

    [ObservableProperty]
    private string outputJson = string.Empty;

    [ObservableProperty]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private bool isValid;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private bool isSortKeysEnabled;

    [ObservableProperty]
    private string? sourceFileExtension;

    /// <summary>
    /// Indicates whether output has content
    /// </summary>
    public bool HasOutput => !string.IsNullOrWhiteSpace(OutputJson);

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFormatterViewModel"/> class.
    /// </summary>
    /// <param name="errorHandler">Error handler for centralized logging.</param>
    /// <param name="jsonFormatterService">JSON formatting service.</param>
    /// <param name="clipboardService">Clipboard access service.</param>
    /// <param name="localizationService">Localization service for user-facing text.</param>
    public JsonFormatterViewModel(
        IErrorHandler errorHandler,
        IJsonFormatterService jsonFormatterService,
        IClipboardService clipboardService,
        ILocalizationService localizationService)
        : base(errorHandler)
    {
        _jsonFormatterService = jsonFormatterService;
        _clipboardService = clipboardService;
        _localizationService = localizationService;
    }

    /// <summary>
    /// Called when InputJson property changes - automatically validates and formats the input
    /// </summary>
    partial void OnInputJsonChanged(string value)
    {
        // Always clear output immediately when input changes
        OutputJson = string.Empty;
        OnPropertyChanged(nameof(HasOutput));

        if (string.IsNullOrWhiteSpace(value))
        {
            ValidationMessage = string.Empty;
            IsValid = false;
            IsError = false;
            SourceFileExtension = null;
            return;
        }

        var result = _jsonFormatterService.Validate(value);
        UpdateValidationState(result);
        
        // Auto-format using the last used mode
        if (result.IsValid)
        {
            FormatWithCurrentMode();
        }
    }

    /// <summary>
    /// Called when IsSortKeysEnabled property changes - reformat output if it exists
    /// </summary>
    partial void OnIsSortKeysEnabledChanged(bool value)
    {
        // Reformat using the last used mode if we have valid input
        if (!string.IsNullOrWhiteSpace(InputJson) && IsValid)
        {
            FormatWithCurrentMode();
        }
    }

    /// <summary>
    /// Formats the input using the last mode that was used (prettify or minify)
    /// </summary>
    private void FormatWithCurrentMode()
    {
        var result = _lastFormatMode == "minify"
            ? _jsonFormatterService.Minify(InputJson, IsSortKeysEnabled)
            : _jsonFormatterService.Prettify(InputJson, IsSortKeysEnabled);

        if (result.IsValid)
        {
            OutputJson = result.Output;
            UpdateValidationState(result);
        }
        else
        {
            OutputJson = string.Empty;
            UpdateValidationState(result);
        }
        OnPropertyChanged(nameof(HasOutput));
    }

    /// <summary>
    /// Prettifies the input JSON
    /// </summary>
    [RelayCommand]
    private void Prettify()
    {
        _lastFormatMode = "prettify";
        var result = _jsonFormatterService.Prettify(InputJson, IsSortKeysEnabled);
        UpdateValidationState(result);
        
        if (result.IsValid)
        {
            OutputJson = result.Output;
            OnPropertyChanged(nameof(HasOutput));
        }
    }

    /// <summary>
    /// Minifies the input JSON
    /// </summary>
    [RelayCommand]
    private void Minify()
    {
        _lastFormatMode = "minify";
        var result = _jsonFormatterService.Minify(InputJson, IsSortKeysEnabled);
        UpdateValidationState(result);
        
        if (result.IsValid)
        {
            OutputJson = result.Output;
            OnPropertyChanged(nameof(HasOutput));
        }
    }

    /// <summary>
    /// Clears all input and output
    /// </summary>
    [RelayCommand]
    private void Clear()
    {
        InputJson = string.Empty;
        OutputJson = string.Empty;
        ValidationMessage = string.Empty;
        IsValid = false;
        IsError = false;
        OnPropertyChanged(nameof(HasOutput));
    }

    /// <summary>
    /// Copies output to clipboard
    /// </summary>
    [RelayCommand]
    private async Task CopyOutputAsync()
    {
        if (!string.IsNullOrWhiteSpace(OutputJson))
        {
            await _clipboardService.TrySetTextAsync(OutputJson);
        }
    }

    /// <summary>
    /// Saves the output JSON to a file
    /// </summary>
    [RelayCommand]
    private async Task SaveOutputAsync()
    {
        if (string.IsNullOrWhiteSpace(OutputJson))
        {
            return;
        }

        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (topLevel?.MainWindow is null)
            {
                ValidationMessage = _localizationService.GetString("jsonformatter.window_not_found");
                IsError = true;
                return;
            }

            // Use TopLevel storage provider
            var storageProvider = Avalonia.Controls.TopLevel.GetTopLevel(topLevel.MainWindow)?.StorageProvider;
            if (storageProvider is null)
            {
                ValidationMessage = _localizationService.GetString("jsonformatter.storage_not_available");
                IsError = true;
                return;
            }

            // Determine the default file extension
            var defaultExtension = SourceFileExtension ?? ".json";
            if (!defaultExtension.StartsWith("."))
            {
                defaultExtension = "." + defaultExtension;
            }

            var suggestedLocation = await storageProvider.TryGetWellKnownFolderAsync(
                Avalonia.Platform.Storage.WellKnownFolder.Documents);

            var file = await storageProvider.SaveFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerSaveOptions
                {
                    Title = _localizationService.GetString("jsonformatter.save_dialog_title"),
                    SuggestedFileName = $"output{defaultExtension}",
                    DefaultExtension = defaultExtension.TrimStart('.'),
                    SuggestedStartLocation = suggestedLocation
                });

            if (file is not null)
            {
                try
                {
                    await using var stream = await file.OpenWriteAsync();
                    await using var writer = new System.IO.StreamWriter(stream, System.Text.Encoding.UTF8);
                    await writer.WriteAsync(OutputJson);
                    ValidationMessage = _localizationService.GetString("jsonformatter.file_saved");
                    IsValid = true;
                    IsError = false;
                }
                catch (Exception ex)
                {
                    ValidationMessage = _localizationService.GetString("jsonformatter.save_failed", ex.Message);
                    IsError = true;
                    IsValid = false;
                    ErrorHandler.LogException(ex, "SaveOutput");
                }
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = _localizationService.GetString("jsonformatter.file_picker_failed", ex.Message);
            IsError = true;
            IsValid = false;
            ErrorHandler.LogException(ex, "SaveOutput");
        }
    }

    /// <summary>
    /// Opens file picker to browse and load a JSON file
    /// </summary>
    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (topLevel?.MainWindow is null)
            {
                ValidationMessage = _localizationService.GetString("jsonformatter.window_not_found");
                IsError = true;
                return;
            }

            var storageProvider = Avalonia.Controls.TopLevel.GetTopLevel(topLevel.MainWindow)?.StorageProvider;
            if (storageProvider is null)
            {
                ValidationMessage = _localizationService.GetString("jsonformatter.storage_not_available");
                IsError = true;
                return;
            }

            var suggestedLocation = await storageProvider.TryGetWellKnownFolderAsync(
                Avalonia.Platform.Storage.WellKnownFolder.Documents);

            var files = await storageProvider.OpenFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = _localizationService.GetString("jsonformatter.open_dialog_title"),
                    AllowMultiple = false,
                    SuggestedStartLocation = suggestedLocation,
                    FileTypeFilter = new[]
                    {
                        new Avalonia.Platform.Storage.FilePickerFileType(_localizationService.GetString("jsonformatter.all_files")) { Patterns = new[] { "*" } }
                    }
                });

            if (files.Count > 0)
            {
                var selectedFile = files[0];
                var fileExtension = System.IO.Path.GetExtension(selectedFile.Name);
                var fileContent = await System.IO.File.ReadAllTextAsync(selectedFile.Path.LocalPath, System.Text.Encoding.UTF8);

                InputJson = fileContent;
                SourceFileExtension = fileExtension;
                ValidationMessage = _localizationService.GetString("jsonformatter.file_loaded", selectedFile.Name);
                IsValid = true;
                IsError = false;
            }
        }
        catch (Exception ex)
        {
            ValidationMessage = _localizationService.GetString("jsonformatter.load_failed", ex.Message);
            IsError = true;
            IsValid = false;
            ErrorHandler.LogException(ex, "BrowseFile");
        }
    }

    /// <summary>
    /// Updates validation state based on result
    /// </summary>
    private void UpdateValidationState(JsonFormatterResult result)
    {
        IsValid = result.IsValid;
        IsError = !result.IsValid;
        
        if (result.IsValid)
        {
            ValidationMessage = string.Empty;
        }
        else
        {
            ValidationMessage = _localizationService.GetStringOrFallback(
                result.ErrorKey,
                result.ErrorMessage ?? _localizationService.GetString("common.error_unknown"),
                result.ErrorArgs ?? []);
        }
    }
}
