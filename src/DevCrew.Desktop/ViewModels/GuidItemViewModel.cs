using CommunityToolkit.Mvvm.ComponentModel;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for a single GUID item in the recent list
/// </summary>
public partial class GuidItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string guidValue = string.Empty;

    [ObservableProperty]
    private bool isSaved;

    [ObservableProperty]
    private int? databaseId;

    [ObservableProperty]
    private string? notes;

    public GuidItemViewModel(string guidValue, bool isSaved = false, int? databaseId = null, string? notes = null)
    {
        GuidValue = guidValue;
        IsSaved = isSaved;
        DatabaseId = databaseId;
        Notes = notes;
    }
}
