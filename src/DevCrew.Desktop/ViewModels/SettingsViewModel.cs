using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;
using DevCrew.Desktop.Services;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the settings view.
/// </summary>
public class SettingsViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;

    public SettingsViewModel(IErrorHandler errorHandler, ILocalizationService localizationService)
        : base(errorHandler)
    {
        _localizationService = localizationService;
        _localizationService.LanguageChanged += OnLanguageChanged;
        SupportedLanguages = _localizationService.SupportedLanguages;
        SelectedLanguage = SupportedLanguages.FirstOrDefault(x => x.CultureName == _localizationService.CurrentCulture.Name)
            ?? SupportedLanguages.FirstOrDefault();
    }

    public IReadOnlyList<SupportedLanguage> SupportedLanguages { get; }

    private SupportedLanguage? _selectedLanguage;

    public SupportedLanguage? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value) && value is not null)
            {
                _ = _localizationService.SetLanguage(value.CultureName);
            }
        }
    }

    public string Title => _localizationService.GetString("settings.title");

    public string LanguageLabel => _localizationService.GetString("settings.language");

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        var selected = SupportedLanguages.FirstOrDefault(x => x.CultureName == _localizationService.CurrentCulture.Name);
        if (selected is not null && !ReferenceEquals(SelectedLanguage, selected))
        {
            _selectedLanguage = selected;
            OnPropertyChanged(nameof(SelectedLanguage));
        }

        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(LanguageLabel));
    }
}
