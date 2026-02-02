using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Data;
using DevCrew.Core.Models;
using DevCrew.Core.Services;
using Microsoft.EntityFrameworkCore;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the GUID creation view
/// Each tab instance maintains its own state
/// </summary>
public partial class CreateGuidViewModel : ObservableObject
{
    private readonly IGuidService _guidService;
    private readonly IClipboardService _clipboardService;
    private readonly AppDbContext _dbContext;
    private readonly HashSet<GuidItemViewModel> _trackedGuidItems = new();

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
    /// Displayed GUIDs (supports infinite scroll when showing saved items)
    /// </summary>
    public ObservableCollection<GuidItemViewModel> FilteredGuidsByPage { get; } = new();

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
    /// <param name="guidService">GUID generation service.</param>
    /// <param name="clipboardService">Clipboard access service.</param>
    /// <param name="dbContext">Database context.</param>
    public CreateGuidViewModel(IGuidService guidService, IClipboardService clipboardService, AppDbContext dbContext)
    {
        _guidService = guidService;
        _clipboardService = clipboardService;
        _dbContext = dbContext;
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
            RecentGuids.RemoveAt(RecentGuids.Count - 1);
        }

        UpdateFilteredGuids();
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
    private async Task CopyGuidItem(GuidItemViewModel guidItem)
    {
        try
        {
            if (!string.IsNullOrWhiteSpace(guidItem.GuidValue))
            {
                await _clipboardService.TrySetTextAsync(guidItem.GuidValue);
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

    [RelayCommand]
    private async Task SaveGuid(GuidItemViewModel guidItem)
    {
        try
        {
            if (guidItem.IsSaved)
                return;

            var guidHistory = new GuidHistory
            {
                GuidValue = guidItem.GuidValue,
                CreatedAt = DateTime.UtcNow,
                Notes = guidItem.Notes
            };
            _dbContext.GuidHistories.Add(guidHistory);
            await _dbContext.SaveChangesAsync();

            guidItem.IsSaved = true;
            guidItem.DatabaseId = guidHistory.Id;
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
            System.Diagnostics.Debug.WriteLine($"Save GUID error: {ex.Message}");
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
                var historyItem = await _dbContext.GuidHistories
                    .FirstOrDefaultAsync(g => g.Id == guidItem.DatabaseId.Value);

                if (historyItem != null)
                {
                    _dbContext.GuidHistories.Remove(historyItem);
                    await _dbContext.SaveChangesAsync();
                }
            }

            // Remove from recent list
            RecentGuids.Remove(guidItem);

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
            System.Diagnostics.Debug.WriteLine($"Delete GUID error: {ex.Message}");
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

        try
        {
            IsLoadingMore = true;

            if (reset)
            {
                _savedSkip = 0;
                HasMoreSavedGuids = true;
                FilteredGuidsByPage.Clear();
            }

            var savedGuids = await _dbContext.GuidHistories
                .OrderByDescending(g => g.CreatedAt)
                .Skip(_savedSkip)
                .Take(PageSize)
                .ToListAsync();

            // Apply search filter
            if (!string.IsNullOrWhiteSpace(SearchQuery))
            {
                savedGuids = savedGuids.Where(g =>
                    g.Notes != null && g.Notes.Contains(SearchQuery, StringComparison.CurrentCultureIgnoreCase)).ToList();
            }

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
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Load saved GUIDs page error: {ex.Message}");
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
            var savedGuids = await _dbContext.GuidHistories
                .OrderByDescending(g => g.CreatedAt)
                .Take(10)
                .ToListAsync();

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
            System.Diagnostics.Debug.WriteLine($"Load and filter saved GUIDs error: {ex.Message}");
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
            var historyItem = await _dbContext.GuidHistories
                .FirstOrDefaultAsync(g => g.Id == databaseId);

            if (historyItem == null)
                return;

            historyItem.Notes = notes;
            await _dbContext.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Update GUID notes error: {ex.Message}");
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
}
