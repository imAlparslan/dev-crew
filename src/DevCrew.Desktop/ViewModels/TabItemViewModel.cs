using CommunityToolkit.Mvvm.ComponentModel;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for tab item
/// </summary>
public partial class TabItemViewModel : ObservableObject
{
    [ObservableProperty]
    private string header = string.Empty;

    [ObservableProperty]
    private object? content;

    [ObservableProperty]
    private bool isClosable = true;

    [ObservableProperty]
    private string? icon;

    [ObservableProperty]
    private string? tooltip;

    public string Id { get; set; } = string.Empty;
}
