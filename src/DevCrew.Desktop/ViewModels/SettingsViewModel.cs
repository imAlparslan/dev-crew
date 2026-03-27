using DevCrew.Core.Services;
using DevCrew.Core.Services.Repositories;
using DevCrew.Core.ViewModels;
using DevCrew.Desktop.Services;

namespace DevCrew.Desktop.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly IAppSettingsRepository _appSettingsRepository;
    private readonly IFontService _fontService;
    private bool _isInitializing;

    public SettingsViewModel(
        IErrorHandler errorHandler,
        ILocalizationService localizationService,
        IAppSettingsRepository appSettingsRepository,
        IFontService fontService)
        : base(errorHandler)
    {
        _localizationService = localizationService;
        _appSettingsRepository = appSettingsRepository;
        _fontService = fontService;
        _localizationService.LanguageChanged += OnLanguageChanged;
        SupportedLanguages = _localizationService.SupportedLanguages;

        _isInitializing = true;

        SelectedLanguage = SupportedLanguages.FirstOrDefault(x => x.CultureName == _localizationService.CurrentCulture.Name)
            ?? SupportedLanguages.FirstOrDefault();

        SelectedFontSize = _fontService.CurrentFontSizePreference;

        SelectedUiFont = _fontService.AvailableUiFonts
            .FirstOrDefault(x => x.Key == _fontService.CurrentUiFontFamily)
            ?? _fontService.AvailableUiFonts.FirstOrDefault();

        SelectedContentFont = _fontService.AvailableContentFonts
            .FirstOrDefault(x => x.Key == _fontService.CurrentContentFontFamily)
            ?? _fontService.AvailableContentFonts.FirstOrDefault();

        _isInitializing = false;
    }

    // Language
    public IReadOnlyList<SupportedLanguage> SupportedLanguages { get; }

    private SupportedLanguage? _selectedLanguage;
    public SupportedLanguage? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value) && value is not null)
            {
                if (_localizationService.SetLanguage(value.CultureName) && !_isInitializing)
                {
                    _ = PersistLanguagePreferenceAsync(value.CultureName);
                }
            }
        }
    }

    // Font size
    public IReadOnlyList<string> FontSizeOptions => _fontService.FontSizeOptions;

    private string? _selectedFontSize;
    public string? SelectedFontSize
    {
        get => _selectedFontSize;
        set
        {
            if (SetProperty(ref _selectedFontSize, value) && value is not null && !_isInitializing)
            {
                ApplyAndPersistFontSettings();
            }
        }
    }

    // UI font
    public IReadOnlyList<FontOption> AvailableUiFonts => _fontService.AvailableUiFonts;

    private FontOption? _selectedUiFont;
    public FontOption? SelectedUiFont
    {
        get => _selectedUiFont;
        set
        {
            if (SetProperty(ref _selectedUiFont, value) && value is not null && !_isInitializing)
            {
                ApplyAndPersistFontSettings();
            }
        }
    }

    // Content/code font
    public IReadOnlyList<FontOption> AvailableContentFonts => _fontService.AvailableContentFonts;

    private FontOption? _selectedContentFont;
    public FontOption? SelectedContentFont
    {
        get => _selectedContentFont;
        set
        {
            if (SetProperty(ref _selectedContentFont, value) && value is not null && !_isInitializing)
            {
                ApplyAndPersistFontSettings();
            }
        }
    }

    // Localized labels
    public string Title => _localizationService.GetString("settings.title");
    public string LanguageLabel => _localizationService.GetString("settings.language");
    public string FontSectionTitle => _localizationService.GetString("settings.font_section");
    public string FontSizeLabel => _localizationService.GetString("settings.font_size");
    public string UiFontLabel => _localizationService.GetString("settings.ui_font");
    public string CodeFontLabel => _localizationService.GetString("settings.code_font");

    public string FontSizeSmallLabel => _localizationService.GetString("settings.font_size_small");
    public string FontSizeMediumLabel => _localizationService.GetString("settings.font_size_medium");
    public string FontSizeLargeLabel => _localizationService.GetString("settings.font_size_large");

    private void ApplyAndPersistFontSettings()
    {
        var size = SelectedFontSize ?? _fontService.CurrentFontSizePreference;
        var ui = SelectedUiFont?.Key ?? _fontService.CurrentUiFontFamily;
        var code = SelectedContentFont?.Key ?? _fontService.CurrentContentFontFamily;

        _fontService.ApplyFontSettings(size, ui, code);
        _ = PersistFontSettingsAsync(size, ui, code);
    }

    private async Task PersistLanguagePreferenceAsync(string cultureName)
    {
        _ = await ErrorHandler.TryExecuteAsync(
            () => _appSettingsRepository.UpdateLanguageAsync(cultureName),
            "Persist language preference");
    }

    private async Task PersistFontSettingsAsync(string fontSizePreference, string uiFontFamily, string contentFontFamily)
    {
        _ = await ErrorHandler.TryExecuteAsync(
            () => _appSettingsRepository.UpdateFontSettingsAsync(fontSizePreference, uiFontFamily, contentFontFamily),
            "Persist font settings");
    }

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
        OnPropertyChanged(nameof(FontSectionTitle));
        OnPropertyChanged(nameof(FontSizeLabel));
        OnPropertyChanged(nameof(UiFontLabel));
        OnPropertyChanged(nameof(CodeFontLabel));
        OnPropertyChanged(nameof(FontSizeSmallLabel));
        OnPropertyChanged(nameof(FontSizeMediumLabel));
        OnPropertyChanged(nameof(FontSizeLargeLabel));
    }
}

