using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;
using DevCrew.Desktop.Services;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the GUID creation view
/// Each tab instance maintains its own state
/// </summary>
public partial class CreateGuidViewModel : BaseViewModel, IDisposable
{
    private readonly IGuidService _guidService;
    private readonly IClipboardService _clipboardService;
    private readonly IGuidRepository _guidRepository;
    private readonly ILocalizationService _localizationService;
    private readonly HashSet<GuidItemViewModel> _trackedGuidItems = new();
    private CancellationTokenSource? _loadCancellationTokenSource;

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
    /// Collection of recently generated GUIDs
    /// </summary>
    public ObservableCollection<GuidItemViewModel> RecentGuids { get; } = new();

    /// <summary>
    /// Indicates whether recent GUIDs exist.
    /// </summary>
    public bool HasRecentGuids => RecentGuids.Count > 0;

    /// <summary>
    /// Displayed GUIDs (supports infinite scroll when showing saved items)
    /// </summary>
    public ObservableCollection<GuidItemViewModel> FilteredGuidsByPage { get; } = new();

    /// <summary>
    /// Indicates whether filtered GUIDs exist.
    /// </summary>
    public bool HasFilteredGuids => FilteredGuidsByPage.Count > 0;

    [ObservableProperty]
    private int pageSize = 10;

    [ObservableProperty]
    private bool isLoadingMore;

    [ObservableProperty]
    private bool hasMoreSavedGuids = true;

    private int _savedSkip;

    [ObservableProperty]
    private bool showOnlySavedGuids;

    [ObservableProperty]
    private string searchQuery = string.Empty;

    /// <summary>
    /// Initializes a new instance of the <see cref="CreateGuidViewModel"/> class.
    /// </summary>
    /// <param name="errorHandler">Error handler for centralized logging.</param>
    /// <param name="guidService">GUID generation service.</param>
    /// <param name="clipboardService">Clipboard access service.</param>
    /// <param name="guidRepository">Repository for GUID data access.</param>
    /// <param name="localizationService">Localization service for user-facing text.</param>
    public CreateGuidViewModel(
        IErrorHandler errorHandler,
        IGuidService guidService, 
        IClipboardService clipboardService, 
        IGuidRepository guidRepository,
        ILocalizationService localizationService)
        : base(errorHandler)
    {
        _guidService = guidService;
        _clipboardService = clipboardService;
        _guidRepository = guidRepository;
        _localizationService = localizationService;

        RecentGuids.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasRecentGuids));
        FilteredGuidsByPage.CollectionChanged += (_, __) => OnPropertyChanged(nameof(HasFilteredGuids));
    }

    [RelayCommand]
    private void GenerateGuid()
    {
        CurrentGuid = _guidService.Generate();
        var guidItem = new GuidItemViewModel(CurrentGuid);
        AttachGuidItem(guidItem);
        RecentGuids.Insert(0, guidItem);

        // Keep only the last 10 GUIDs
        while (RecentGuids.Count > 10)
        {
            var removedItem = RecentGuids[RecentGuids.Count - 1];
            RecentGuids.RemoveAt(RecentGuids.Count - 1);
            DetachGuidItem(removedItem);
        }

        UpdateFilteredGuids();
    }

    [RelayCommand]
    private async Task CopyToClipboard()
    {
        if (!string.IsNullOrWhiteSpace(CurrentGuid))
        {
            IsGuidCopied = await _clipboardService.TrySetTextAsync(CurrentGuid);
        }
    }

    [RelayCommand]
    private async Task CopyGuidItem(GuidItemViewModel guidItem)
    {
        if (!string.IsNullOrWhiteSpace(guidItem.GuidValue))
        {
            await _clipboardService.TrySetTextAsync(guidItem.GuidValue);
        }
    }

    [RelayCommand]
    private async Task LoadSavedGuids()
    {
        await LoadSavedGuidsAsync();
    }

    [RelayCommand]
    private async Task LoadMoreSavedGuids()
    {
        await LoadMoreSavedGuidsAsync();
    }

    [RelayCommand]
    private void Clear()
    {
        CurrentGuid = string.Empty;
    }

    [RelayCommand]
    private async Task SaveGuid(GuidItemViewModel guidItem)
    {
        try
        {
            if (guidItem.IsSaved)
                return;

            var savedGuidHistory = await _guidRepository.SaveGuidAsync(guidItem.GuidValue, guidItem.Notes);

            guidItem.IsSaved = true;
            guidItem.DatabaseId = savedGuidHistory.Id;
            if (ShowOnlySavedGuids)
            {
                await LoadSavedGuidsPageAsync(reset: true);
            }
            else
            {
                UpdateFilteredGuids();
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, "Save GUID");
            ErrorMessage = $"{_localizationService.GetString("createguid.save")}: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task DeleteGuid(GuidItemViewModel guidItem)
    {
        try
        {
            // Remove from database if saved
            if (guidItem.IsSaved && guidItem.DatabaseId.HasValue)
            {
                await _guidRepository.DeleteGuidAsync(guidItem.DatabaseId.Value);
            }

            // Remove from recent list
            RecentGuids.Remove(guidItem);
            DetachGuidItem(guidItem);

            // If showing only saved guids, reload from database to reflect changes
            if (ShowOnlySavedGuids)
            {
                await LoadSavedGuidsPageAsync(reset: true);
            }
            else
            {
                UpdateFilteredGuids();
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, "Delete GUID");
            ErrorMessage = $"{_localizationService.GetString("createguid.delete")}: {ex.Message}";
        }
    }

    private void UpdateFilteredGuids()
    {
        if (ShowOnlySavedGuids)
        {
            _ = LoadSavedGuidsPageAsync(reset: true);
        }
        else
        {
            FilteredGuidsByPage.Clear();

            var filtered = RecentGuids.AsEnumerable();
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                filtered = filtered.Where(item =>
                    item.Notes != null && item.Notes.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase));
            }

            foreach (var item in filtered)
            {
                AttachGuidItem(item);
                FilteredGuidsByPage.Add(item);
            }
        }
    }

    partial void OnSearchQueryChanged(string value)
    {
        UpdateFilteredGuids();
    }

    partial void OnShowOnlySavedGuidsChanged(bool value)
    {
        // When filter is toggled, reload from database if needed
        if (value)
        {
            _ = LoadSavedGuidsPageAsync(reset: true);
        }
        else
        {
            UpdateFilteredGuids();
        }
    }

    /// <summary>
    /// Loads the next page of saved GUIDs when infinite scrolling.
    /// </summary>
    public async Task LoadMoreSavedGuidsAsync()
    {
        await LoadSavedGuidsPageAsync(reset: false);
    }

    private async Task LoadSavedGuidsPageAsync(bool reset)
    {
        if (!ShowOnlySavedGuids)
            return;

        if (IsLoadingMore)
            return;

        if (!HasMoreSavedGuids && !reset)
            return;

        // Cancel any existing load operation
        _loadCancellationTokenSource?.Cancel();
        _loadCancellationTokenSource?.Dispose();
        _loadCancellationTokenSource = new CancellationTokenSource();
        
        var cancellationToken = _loadCancellationTokenSource.Token;

        try
        {
            IsLoadingMore = true;

            if (reset)
            {
                _savedSkip = 0;
                HasMoreSavedGuids = true;
                
                // Detach items before clearing when resetting saved GUIDs view
                foreach (var item in FilteredGuidsByPage.ToList())
                {
                    DetachGuidItem(item);
                }
                FilteredGuidsByPage.Clear();
            }

            // Fetch paginated GUIDs from repository with optional search filter
            var savedGuids = await _guidRepository.GetGuidsPagedAsync(_savedSkip, PageSize, SearchQuery, cancellationToken);

            foreach (var dbGuid in savedGuids)
            {
                var guidItem = new GuidItemViewModel(
                    guidValue: dbGuid.GuidValue,
                    isSaved: true,
                    databaseId: dbGuid.Id,
                    notes: dbGuid.Notes
                );
                AttachGuidItem(guidItem);
                FilteredGuidsByPage.Add(guidItem);
            }

            _savedSkip += savedGuids.Count;
            HasMoreSavedGuids = savedGuids.Count == PageSize;
        }
        catch (OperationCanceledException)
        {
            // Operation was cancelled, this is expected
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, "Load saved GUIDs page");
            ErrorMessage = $"{_localizationService.GetString("createguid.recent_title")}: {ex.Message}";
        }
        finally
        {
            IsLoadingMore = false;
        }
    }

    partial void OnCurrentGuidChanged(string value)
    {
        IsGuidCopied = false;
    }

    /// <summary>
    /// Load saved GUIDs from the database
    /// </summary>
    public async Task LoadSavedGuidsAsync()
    {
        try
        {
            var savedGuids = await _guidRepository.GetGuidsPagedAsync(0, 10);

            foreach (var guid in savedGuids)
            {
                var guidItem = new GuidItemViewModel(
                    guidValue: guid.GuidValue,
                    isSaved: true,
                    databaseId: guid.Id,
                    notes: guid.Notes
                );
                AttachGuidItem(guidItem);
                RecentGuids.Add(guidItem);
            }

            // Update filtered view after loading saved GUIDs
            UpdateFilteredGuids();
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, "Load and filter saved GUIDs");
            ErrorMessage = $"{_localizationService.GetString("createguid.saved_only")}: {ex.Message}";
        }
    }

    private void AttachGuidItem(GuidItemViewModel item)
    {
        if (_trackedGuidItems.Add(item))
        {
            item.PropertyChanged += OnGuidItemPropertyChanged;
        }
    }

    private async void OnGuidItemPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(GuidItemViewModel.Notes))
            return;

        if (sender is not GuidItemViewModel item)
            return;

        if (!item.IsSaved || !item.DatabaseId.HasValue)
            return;

        await UpdateSavedGuidNotesAsync(item.DatabaseId.Value, item.Notes);
        SyncGuidNotesAcrossCollections(item);
    }

    private async Task UpdateSavedGuidNotesAsync(int databaseId, string? notes)
    {
        try
        {
            await _guidRepository.UpdateGuidNotesAsync(databaseId, notes);
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, "Update GUID notes");
            ErrorMessage = $"{_localizationService.GetString("createguid.notes_watermark")}: {ex.Message}";
        }
    }

    private void SyncGuidNotesAcrossCollections(GuidItemViewModel source)
    {
        if (!source.DatabaseId.HasValue)
            return;

        var databaseId = source.DatabaseId.Value;

        foreach (var item in RecentGuids)
        {
            if (ReferenceEquals(item, source))
                continue;

            if (item.DatabaseId == databaseId)
            {
                item.Notes = source.Notes;
            }
        }

        foreach (var item in FilteredGuidsByPage)
        {
            if (ReferenceEquals(item, source))
                continue;

            if (item.DatabaseId == databaseId)
            {
                item.Notes = source.Notes;
            }
        }
    }

    private void DetachGuidItem(GuidItemViewModel item)
    {
        if (_trackedGuidItems.Remove(item))
        {
            item.PropertyChanged -= OnGuidItemPropertyChanged;
        }
    }

    /// <summary>
    /// Disposes the ViewModel and cleans up event handlers to prevent memory leaks.
    /// </summary>
    public void Dispose()
    {
        // Cancel and dispose any ongoing load operations
        _loadCancellationTokenSource?.Cancel();
        _loadCancellationTokenSource?.Dispose();
        
        // Detach all event handlers
        foreach (var item in _trackedGuidItems.ToList())
        {
            item.PropertyChanged -= OnGuidItemPropertyChanged;
        }
        _trackedGuidItems.Clear();

        GC.SuppressFinalize(this);
    }
}
