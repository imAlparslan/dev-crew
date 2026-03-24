using System.Collections.ObjectModel;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using DevCrew.Core.Models.Results;
using DevCrew.Core.Enums;
using DevCrew.Core.ViewModels;
using DevCrew.Desktop.Services;

namespace DevCrew.Desktop.ViewModels;

public partial class JsonDiffViewModel : BaseViewModel
{
    private readonly IJsonDiffService _jsonDiffService;
    private readonly IClipboardService _clipboardService;
    private readonly ILocalizationService _localizationService;

    private List<JsonPathDiffEntry> _allPathDiffs = [];
    private List<JsonLineDiffEntry> _allLineDiffs = [];
    private JsonDiffSummary _summary = new();

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCompare))]
    private string leftJson = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanCompare))]
    private string rightJson = string.Empty;

    [ObservableProperty]
    private string validationMessage = string.Empty;

    [ObservableProperty]
    private bool isValid;

    [ObservableProperty]
    private bool isError;

    [ObservableProperty]
    private string pathFilter = string.Empty;

    [ObservableProperty]
    private string lineDiffOutput = string.Empty;

    [ObservableProperty]
    private string selectedSortField = "Path";

    [ObservableProperty]
    private bool sortDescending;

    [ObservableProperty]
    private bool showAdded = true;

    [ObservableProperty]
    private bool showRemoved = true;

    [ObservableProperty]
    private bool showChanged = true;

    [ObservableProperty]
    private bool showUnchanged;

    [ObservableProperty]
    private bool ignoreObjectPropertyOrder = true;

    [ObservableProperty]
    private bool treatArrayOrderAsSignificant = true;

    [ObservableProperty]
    private bool ignoreWhitespaceDifferences = true;

    [ObservableProperty]
    private bool treatNullAndEmptyStringAsEqual;

    [ObservableProperty]
    private string? leftSourceFileExtension;

    [ObservableProperty]
    private string? rightSourceFileExtension;

    public ObservableCollection<JsonPathDiffEntry> FilteredPathDiffs { get; } = [];

    public ObservableCollection<LineDiffItem> LineDiffItems { get; } = [];

    public ObservableCollection<string> SortFields { get; } = ["Path", "Type"];

    public bool CanCompare => !string.IsNullOrWhiteSpace(LeftJson) && !string.IsNullOrWhiteSpace(RightJson);

    public bool HasPathDiffs => FilteredPathDiffs.Count > 0;

    public bool HasLineDiffOutput => LineDiffItems.Count > 0;

    public int AddedCount => _summary.AddedCount;

    public int RemovedCount => _summary.RemovedCount;

    public int ChangedCount => _summary.ChangedCount;

    public int UnchangedCount => _summary.UnchangedCount;

    public int TotalDiffCount => _summary.TotalDifferences;

    public JsonDiffViewModel(
        IErrorHandler errorHandler,
        IJsonDiffService jsonDiffService,
        IClipboardService clipboardService,
        ILocalizationService localizationService)
        : base(errorHandler)
    {
        _jsonDiffService = jsonDiffService;
        _clipboardService = clipboardService;
        _localizationService = localizationService;

        FilteredPathDiffs.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasPathDiffs));
        LineDiffItems.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasLineDiffOutput));
    }

    [RelayCommand]
    private void Compare()
    {
        if (!CanCompare)
        {
            ValidationMessage = _localizationService.GetString("jsondiff.compare_inputs_required");
            IsError = true;
            IsValid = false;
            return;
        }

        var options = new JsonDiffOptions
        {
            IgnoreObjectPropertyOrder = IgnoreObjectPropertyOrder,
            TreatArrayOrderAsSignificant = TreatArrayOrderAsSignificant,
            IgnoreWhitespaceDifferences = IgnoreWhitespaceDifferences,
            TreatNullAndEmptyStringAsEqual = TreatNullAndEmptyStringAsEqual
        };

        var result = _jsonDiffService.Compare(LeftJson, RightJson, options);
        if (!result.IsValid)
        {
            ValidationMessage = _localizationService.GetStringOrFallback(
                result.ErrorKey,
                result.ErrorMessage ?? _localizationService.GetString("common.error_unknown"),
                result.ErrorArgs ?? []);
            IsError = true;
            IsValid = false;
            _allPathDiffs = [];
            _allLineDiffs = [];
            _summary = new JsonDiffSummary();
            LineDiffOutput = string.Empty;
            LineDiffItems.Clear();
            ApplyPathFiltersAndSorting();
            RaiseSummaryPropertiesChanged();
            return;
        }

        _allPathDiffs = [.. result.PathDiffs];
        _allLineDiffs = [.. result.LineDiffs];
        _summary = result.Summary;

        ValidationMessage = _localizationService.GetString("jsondiff.compare_complete", result.Summary.TotalDifferences);
        IsError = false;
        IsValid = true;

        BuildLineDiffOutput();
        ApplyPathFiltersAndSorting();
        RaiseSummaryPropertiesChanged();
    }

    [RelayCommand]
    private void Clear()
    {
        LeftJson = string.Empty;
        RightJson = string.Empty;
        ValidationMessage = string.Empty;
        IsError = false;
        IsValid = false;
        PathFilter = string.Empty;
        LineDiffOutput = string.Empty;
        LineDiffItems.Clear();
        LeftSourceFileExtension = null;
        RightSourceFileExtension = null;
        _allPathDiffs = [];
        _allLineDiffs = [];
        _summary = new JsonDiffSummary();
        ApplyPathFiltersAndSorting();
        RaiseSummaryPropertiesChanged();
    }

    [RelayCommand]
    private async Task CopyPathDiffsAsync()
    {
        if (FilteredPathDiffs.Count == 0)
        {
            return;
        }

        var builder = new StringBuilder();
        foreach (var item in FilteredPathDiffs)
        {
            builder.AppendLine($"[{item.Kind}] {item.Path}");
            builder.AppendLine($"  Sol : {item.LeftValue}");
            builder.AppendLine($"  Sağ : {item.RightValue}");
        }

        await _clipboardService.TrySetTextAsync(builder.ToString());
    }

    [RelayCommand]
    private async Task CopyLineDiffAsync()
    {
        if (string.IsNullOrWhiteSpace(LineDiffOutput))
        {
            return;
        }

        await _clipboardService.TrySetTextAsync(LineDiffOutput);
    }

    [RelayCommand]
    private async Task BrowseLeftFileAsync()
    {
        var content = await ReadJsonFromPickerAsync();
        if (content.FileContent == null)
        {
            return;
        }

        LeftJson = content.FileContent;
        LeftSourceFileExtension = content.Extension;
        ValidationMessage = $"Sol dosya yüklendi: {content.FileName}";
        IsError = false;
        IsValid = true;
    }

    [RelayCommand]
    private async Task BrowseRightFileAsync()
    {
        var content = await ReadJsonFromPickerAsync();
        if (content.FileContent == null)
        {
            return;
        }

        RightJson = content.FileContent;
        RightSourceFileExtension = content.Extension;
        ValidationMessage = $"Sağ dosya yüklendi: {content.FileName}";
        IsError = false;
        IsValid = true;
    }

    public void SetLeftJsonFromDroppedFile(string fileContent, string extension, string fileName)
    {
        LeftJson = fileContent;
        LeftSourceFileExtension = extension;
        ValidationMessage = $"Sol dosya yüklendi: {fileName}";
        IsError = false;
        IsValid = true;
    }

    public void SetRightJsonFromDroppedFile(string fileContent, string extension, string fileName)
    {
        RightJson = fileContent;
        RightSourceFileExtension = extension;
        ValidationMessage = $"Sağ dosya yüklendi: {fileName}";
        IsError = false;
        IsValid = true;
    }

    partial void OnPathFilterChanged(string value)
    {
        ApplyPathFiltersAndSorting();
    }

    partial void OnSelectedSortFieldChanged(string value)
    {
        ApplyPathFiltersAndSorting();
    }

    partial void OnSortDescendingChanged(bool value)
    {
        ApplyPathFiltersAndSorting();
    }

    partial void OnShowAddedChanged(bool value)
    {
        ApplyPathFiltersAndSorting();
    }

    partial void OnShowRemovedChanged(bool value)
    {
        ApplyPathFiltersAndSorting();
    }

    partial void OnShowChangedChanged(bool value)
    {
        ApplyPathFiltersAndSorting();
    }

    partial void OnShowUnchangedChanged(bool value)
    {
        ApplyPathFiltersAndSorting();
    }

    partial void OnIgnoreObjectPropertyOrderChanged(bool value)
    {
        RecompareIfPossible();
    }

    partial void OnTreatArrayOrderAsSignificantChanged(bool value)
    {
        RecompareIfPossible();
    }

    partial void OnIgnoreWhitespaceDifferencesChanged(bool value)
    {
        RecompareIfPossible();
    }

    partial void OnTreatNullAndEmptyStringAsEqualChanged(bool value)
    {
        RecompareIfPossible();
    }

    private void RecompareIfPossible()
    {
        if (CanCompare && !string.IsNullOrWhiteSpace(ValidationMessage))
        {
            Compare();
        }
    }

    private void ApplyPathFiltersAndSorting()
    {
        IEnumerable<JsonPathDiffEntry> query = _allPathDiffs;

        query = query.Where(ShouldIncludeByKind);

        if (!string.IsNullOrWhiteSpace(PathFilter))
        {
            query = query.Where(x => x.Path.Contains(PathFilter, StringComparison.CurrentCultureIgnoreCase));
        }

        query = SelectedSortField == "Type"
            ? query.OrderBy(x => x.Kind).ThenBy(x => x.Path, StringComparer.Ordinal)
            : query.OrderBy(x => x.Path, StringComparer.Ordinal).ThenBy(x => x.Kind);

        if (SortDescending)
        {
            query = query.Reverse();
        }

        FilteredPathDiffs.Clear();
        foreach (var item in query)
        {
            FilteredPathDiffs.Add(item);
        }
    }

    private bool ShouldIncludeByKind(JsonPathDiffEntry entry)
    {
        return entry.Kind switch
        {
            JsonDiffKind.Added => ShowAdded,
            JsonDiffKind.Removed => ShowRemoved,
            JsonDiffKind.Changed => ShowChanged,
            JsonDiffKind.Unchanged => ShowUnchanged,
            _ => true
        };
    }

    private void BuildLineDiffOutput()
    {
        var builder = new StringBuilder();
        LineDiffItems.Clear();

        foreach (var item in _allLineDiffs)
        {
            var line = item.Kind switch
            {
                JsonDiffKind.Added => $"+ [R{item.RightLineNumber}] {item.RightLine}",
                JsonDiffKind.Removed => $"- [L{item.LeftLineNumber}] {item.LeftLine}",
                JsonDiffKind.Changed => $"~ [L{item.LeftLineNumber}/R{item.RightLineNumber}] {item.LeftLine} -> {item.RightLine}",
                _ => $"  [L{item.LeftLineNumber}/R{item.RightLineNumber}] {item.RightLine}"
            };

            builder.AppendLine(line);
            LineDiffItems.Add(new LineDiffItem(item.Kind, line));
        }

        LineDiffOutput = builder.ToString();
    }

    private void RaiseSummaryPropertiesChanged()
    {
        OnPropertyChanged(nameof(AddedCount));
        OnPropertyChanged(nameof(RemovedCount));
        OnPropertyChanged(nameof(ChangedCount));
        OnPropertyChanged(nameof(UnchangedCount));
        OnPropertyChanged(nameof(TotalDiffCount));
    }

    private async Task<(string? FileContent, string? Extension, string? FileName)> ReadJsonFromPickerAsync()
    {
        try
        {
            var desktopLifetime = Avalonia.Application.Current?.ApplicationLifetime as Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime;
            if (desktopLifetime?.MainWindow is null)
            {
                ValidationMessage = _localizationService.GetString("jsondiff.window_not_found");
                IsError = true;
                IsValid = false;
                return (null, null, null);
            }

            var storageProvider = Avalonia.Controls.TopLevel.GetTopLevel(desktopLifetime.MainWindow)?.StorageProvider;
            if (storageProvider is null)
            {
                ValidationMessage = _localizationService.GetString("jsondiff.storage_not_available");
                IsError = true;
                IsValid = false;
                return (null, null, null);
            }

            var suggestedLocation = await storageProvider.TryGetWellKnownFolderAsync(Avalonia.Platform.Storage.WellKnownFolder.Documents);
            var files = await storageProvider.OpenFilePickerAsync(new Avalonia.Platform.Storage.FilePickerOpenOptions
            {
                Title = _localizationService.GetString("jsondiff.select_file"),
                AllowMultiple = false,
                SuggestedStartLocation = suggestedLocation,
                FileTypeFilter =
                [
                    new Avalonia.Platform.Storage.FilePickerFileType(_localizationService.GetString("jsondiff.all_files"))
                    {
                        Patterns = ["*"]
                    }
                ]
            });

            if (files.Count == 0)
            {
                return (null, null, null);
            }

            var file = files[0];
            var fileContent = await File.ReadAllTextAsync(file.Path.LocalPath, Encoding.UTF8);
            var extension = Path.GetExtension(file.Name);

            return (fileContent, extension, file.Name);
        }
        catch (Exception ex)
        {
            ValidationMessage = $"Dosya yükleme hatası: {ex.Message}";
            IsError = true;
            IsValid = false;
            ErrorHandler.LogException(ex, "ReadJsonFromPicker");
            return (null, null, null);
        }
    }
}

public record LineDiffItem(JsonDiffKind Kind, string Text);
