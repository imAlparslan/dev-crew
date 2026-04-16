using CommunityToolkit.Mvvm.ComponentModel;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for a saved regex preset item in the selector.
/// </summary>
public partial class RegexPresetItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string name = string.Empty;

    [ObservableProperty]
    private string pattern = string.Empty;

    [ObservableProperty]
    private bool ignoreCase;

    [ObservableProperty]
    private bool multiline;

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime? lastUsedAt;

    /// <summary>
    /// Gets a display string showing pattern preview with options flags.
    /// </summary>
    public string DisplayPattern
    {
        get
        {
            var preview = Pattern.Length > 40 ? string.Concat(Pattern.AsSpan(0, 37), "...") : Pattern;
            var flags = new List<string>();
            if (IgnoreCase)
                flags.Add("i");
            if (Multiline)
                flags.Add("m");

            var flagStr = flags.Count > 0 ? $" [{string.Join("", flags)}]" : string.Empty;
            return $"{Name} · {preview}{flagStr}";
        }
    }

    public RegexPresetItemViewModel(
        int id,
        string name,
        string pattern,
        bool ignoreCase,
        bool multiline,
        DateTime createdAt,
        DateTime? lastUsedAt = null)
    {
        Id = id;
        Name = name;
        Pattern = pattern;
        IgnoreCase = ignoreCase;
        Multiline = multiline;
        CreatedAt = createdAt;
        LastUsedAt = lastUsedAt;
    }
}
