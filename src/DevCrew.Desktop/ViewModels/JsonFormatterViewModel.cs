using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the JSON Formatter view
/// </summary>
public partial class JsonFormatterViewModel : BaseViewModel
{
    private readonly IJsonFormatterService _jsonFormatterService;
    private readonly IClipboardService _clipboardService;

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

    /// <summary>
    /// Indicates whether output has content
    /// </summary>
    public bool HasOutput => !string.IsNullOrWhiteSpace(OutputJson);

    /// <summary>
    /// Initializes a new instance of the <see cref="JsonFormatterViewModel"/> class.
    /// </summary>
    /// <param name="errorHandler">Error handler for centralized logging.</param>
    /// <param name="jsonFormatterService">JSON formatter service.</param>
    /// <param name="clipboardService">Clipboard service.</param>
    public JsonFormatterViewModel(
        IErrorHandler errorHandler,
        IJsonFormatterService jsonFormatterService,
        IClipboardService clipboardService)
        : base(errorHandler)
    {
        _jsonFormatterService = jsonFormatterService;
        _clipboardService = clipboardService;
    }

    /// <summary>
    /// Called when InputJson property changes - automatically validates the input
    /// </summary>
    partial void OnInputJsonChanged(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            ValidationMessage = string.Empty;
            IsValid = false;
            IsError = false;
            return;
        }

        var result = _jsonFormatterService.Validate(value);
        UpdateValidationState(result);
    }

    /// <summary>
    /// Prettifies the input JSON
    /// </summary>
    [RelayCommand]
    private void Prettify()
    {
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
            ValidationMessage = result.ErrorMessage ?? "Hata oluştu";
        }
    }
}
