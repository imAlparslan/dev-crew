using CommunityToolkit.Mvvm.ComponentModel;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for a single JWT Builder template item in the list
/// </summary>
public partial class JwtBuilderTemplateItemViewModel : ObservableObject
{
    [ObservableProperty]
    private int id;

    [ObservableProperty]
    private string templateName = string.Empty;

    [ObservableProperty]
    private string algorithm = string.Empty;

    [ObservableProperty]
    private DateTime createdAt;

    [ObservableProperty]
    private DateTime? lastUsedAt;

    public JwtBuilderTemplateItemViewModel(int id, string templateName, string algorithm, DateTime createdAt, DateTime? lastUsedAt = null)
    {
        Id = id;
        TemplateName = templateName;
        Algorithm = algorithm;
        CreatedAt = createdAt;
        LastUsedAt = lastUsedAt;
    }
}
