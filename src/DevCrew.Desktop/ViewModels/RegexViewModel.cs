using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Application.Services;
using DevCrew.Desktop.Services;

namespace DevCrew.Desktop.ViewModels;

public partial class RegexViewModel : BaseViewModel
{
    private readonly IRegexService _regexService;
    private readonly ILocalizationService _localizationService;
    private CancellationTokenSource? _refreshCancellationTokenSource;

    [ObservableProperty]
    private string pattern = string.Empty;

    [ObservableProperty]
    private string inputText = string.Empty;

    [ObservableProperty]
    private string sourcePath = string.Empty;

    [ObservableProperty]
    private bool ignoreCase;

    [ObservableProperty]
    private bool multiline;

    [ObservableProperty]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private bool isValid;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MatchCountSummary))]
    private int matchCount;

    [ObservableProperty]
    private IReadOnlyList<RegexHighlightDisplayItem> matches = [];

    public string MatchCountSummary => _localizationService.GetString("regex.match_count_summary", MatchCount);

    public RegexViewModel(
        IErrorHandler errorHandler,
        IRegexService regexService,
        ILocalizationService localizationService)
        : base(errorHandler)
    {
        _regexService = regexService;
        _localizationService = localizationService;
    }

    public async Task SetSelectedFileAsync(string filePath)
    {
        SourcePath = filePath;
        await LoadFromPathAsync(filePath);
    }

    partial void OnPatternChanged(string value) => ScheduleRefresh();

    partial void OnInputTextChanged(string value) => ScheduleRefresh();

    partial void OnIgnoreCaseChanged(bool value) => ScheduleRefresh();

    partial void OnMultilineChanged(bool value) => ScheduleRefresh();

    [RelayCommand]
    private void Clear()
    {
        _refreshCancellationTokenSource?.Cancel();
        Pattern = string.Empty;
        InputText = string.Empty;
        SourcePath = string.Empty;
        ValidationMessage = string.Empty;
        IsValid = false;
        IsError = false;
        MatchCount = 0;
        Matches = [];
    }

    [RelayCommand]
    private async Task BrowseFileAsync()
    {
        try
        {
            var topLevel = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (topLevel?.MainWindow is null)
            {
                ValidationMessage = _localizationService.GetString("regex.window_not_found");
                IsError = true;
                IsValid = false;
                return;
            }

            var storageProvider = Avalonia.Controls.TopLevel.GetTopLevel(topLevel.MainWindow)?.StorageProvider;
            if (storageProvider is null)
            {
                ValidationMessage = _localizationService.GetString("regex.storage_not_available");
                IsError = true;
                IsValid = false;
                return;
            }

            var suggestedLocation = await storageProvider.TryGetWellKnownFolderAsync(Avalonia.Platform.Storage.WellKnownFolder.Documents);
            var files = await storageProvider.OpenFilePickerAsync(
                new Avalonia.Platform.Storage.FilePickerOpenOptions
                {
                    Title = _localizationService.GetString("regex.open_dialog_title"),
                    AllowMultiple = false,
                    SuggestedStartLocation = suggestedLocation,
                    FileTypeFilter =
                    [
                        new Avalonia.Platform.Storage.FilePickerFileType(_localizationService.GetString("regex.all_files")) { Patterns = ["*"] }
                    ]
                });

            if (files.Count == 0)
            {
                return;
            }

            await LoadFromPathAsync(files[0].Path.LocalPath);
        }
        catch (Exception ex)
        {
            ValidationMessage = _localizationService.GetString("regex.load_failed", ex.Message);
            IsError = true;
            IsValid = false;
            ErrorHandler.LogException(ex, "BrowseRegexFile");
        }
    }

    [RelayCommand]
    private async Task LoadFromPathAsync()
    {
        await LoadFromPathAsync(SourcePath);
    }

    private async Task LoadFromPathAsync(string? filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
        {
            ValidationMessage = _localizationService.GetString("regex.path_required");
            IsError = true;
            IsValid = false;
            return;
        }

        if (!File.Exists(filePath))
        {
            ValidationMessage = _localizationService.GetString("regex.file_not_found", filePath);
            IsError = true;
            IsValid = false;
            return;
        }

        try
        {
            InputText = await File.ReadAllTextAsync(filePath);
            SourcePath = filePath;
            ValidationMessage = _localizationService.GetString("regex.file_loaded", Path.GetFileName(filePath));
            IsError = false;
            IsValid = true;
        }
        catch (UnauthorizedAccessException ex)
        {
            ValidationMessage = _localizationService.GetString("regex.access_denied", ex.Message);
            IsError = true;
            IsValid = false;
            ErrorHandler.LogException(ex, "LoadRegexFileUnauthorized");
        }
        catch (PathTooLongException ex)
        {
            ValidationMessage = _localizationService.GetString("regex.path_too_long", ex.Message);
            IsError = true;
            IsValid = false;
            ErrorHandler.LogException(ex, "LoadRegexFilePathTooLong");
        }
        catch (NotSupportedException ex)
        {
            ValidationMessage = _localizationService.GetString("regex.invalid_path", ex.Message);
            IsError = true;
            IsValid = false;
            ErrorHandler.LogException(ex, "LoadRegexFileInvalidPath");
        }
        catch (IOException ex)
        {
            ValidationMessage = _localizationService.GetString("regex.io_error", ex.Message);
            IsError = true;
            IsValid = false;
            ErrorHandler.LogException(ex, "LoadRegexFileIo");
        }
        catch (Exception ex)
        {
            ValidationMessage = _localizationService.GetString("regex.load_failed", ex.Message);
            IsError = true;
            IsValid = false;
            ErrorHandler.LogException(ex, "LoadRegexFile");
        }
    }

    private void ScheduleRefresh()
    {
        _refreshCancellationTokenSource?.Cancel();
        _refreshCancellationTokenSource?.Dispose();
        var cancellationTokenSource = new CancellationTokenSource();
        _refreshCancellationTokenSource = cancellationTokenSource;
        _ = RefreshMatchesAsync(cancellationTokenSource.Token);
    }

    private async Task RefreshMatchesAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(120, cancellationToken);
        }
        catch (OperationCanceledException)
        {
            return;
        }

        if (cancellationToken.IsCancellationRequested)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(Pattern))
        {
            Matches = [];
            MatchCount = 0;

            if (string.IsNullOrWhiteSpace(InputText))
            {
                ValidationMessage = string.Empty;
                IsError = false;
                IsValid = false;
            }
            else
            {
                ValidationMessage = _localizationService.GetString("regex.pattern_required");
                IsError = true;
                IsValid = false;
            }

            return;
        }

        try
        {
            var result = _regexService.FindMatches(Pattern, InputText, IgnoreCase, Multiline);
            if (!result.IsValid)
            {
                Matches = [];
                MatchCount = 0;
                ValidationMessage = _localizationService.GetStringOrFallback(
                    result.ErrorKey,
                    result.ErrorMessage ?? _localizationService.GetString("common.error_unknown"),
                    result.ErrorArgs ?? []);
                IsError = true;
                IsValid = false;
                return;
            }

            Matches = result.Matches.Select(CreateDisplayItem).ToArray();
            MatchCount = result.MatchCount;
            IsError = false;
            IsValid = true;
            ValidationMessage = result.MatchCount > 0
                ? _localizationService.GetString("regex.matches_found", result.MatchCount)
                : _localizationService.GetString("regex.no_matches");
        }
        catch (Exception ex)
        {
            Matches = [];
            MatchCount = 0;
            ValidationMessage = _localizationService.GetString("regex.processing_failed", ex.Message);
            IsError = true;
            IsValid = false;
            ErrorHandler.LogException(ex, "RefreshRegexMatches");
        }
    }

    private RegexHighlightDisplayItem CreateDisplayItem(DevCrew.Core.Domain.Results.RegexMatchItem match)
    {
        var tooltipLines = new List<string>
        {
            _localizationService.GetString("regex.hover_pattern", Pattern),
            _localizationService.GetString("regex.hover_span", match.Index, match.Length),
            _localizationService.GetString("regex.hover_value", match.Value)
        };

        if (match.Captures.Count == 0)
        {
            tooltipLines.Add(_localizationService.GetString("regex.hover_no_captures"));
        }
        else
        {
            tooltipLines.Add(_localizationService.GetString("regex.hover_captures"));
            tooltipLines.AddRange(match.Captures.Select(capture =>
                _localizationService.GetString("regex.hover_capture_item", capture.Name, capture.Value, capture.Index, capture.Length)));
        }

        return new RegexHighlightDisplayItem(match.Index, match.Length, match.Value, string.Join(Environment.NewLine, tooltipLines));
    }
}

public sealed record RegexHighlightDisplayItem(int Index, int Length, string Value, string TooltipText);