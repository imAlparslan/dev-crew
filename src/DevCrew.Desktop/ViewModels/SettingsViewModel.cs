using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using DevCrew.Desktop.Services;
using Avalonia.Media;

namespace DevCrew.Desktop.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private readonly ILocalizationService _localizationService;
    private readonly IAppSettingsRepository _appSettingsRepository;
    private readonly IFontService _fontService;
    private readonly IUninstallService _uninstallService;
    private bool _isInitializing;
    private bool _isPromptingUninstall;

    public SettingsViewModel(
        IErrorHandler errorHandler,
        ILocalizationService localizationService,
        IAppSettingsRepository appSettingsRepository,
        IFontService fontService,
        IUninstallService uninstallService)
        : base(errorHandler)
    {
        _localizationService = localizationService;
        _appSettingsRepository = appSettingsRepository;
        _fontService = fontService;
        _uninstallService = uninstallService;
        ApplyFontSettingsCommand = new AsyncRelayCommand(ApplyFontSettingsAsync, CanApplyFontSettings);
        PromptUninstallCommand = new RelayCommand(() => IsPromptingUninstall = true);
        CancelUninstallCommand = new RelayCommand(() => IsPromptingUninstall = false);
        ConfirmUninstallCommand = new AsyncRelayCommand(ConfirmUninstallAsync);
        _localizationService.LanguageChanged += OnLanguageChanged;
        SupportedLanguages = _localizationService.SupportedLanguages;

        _isInitializing = true;

        SelectedLanguage = SupportedLanguages.FirstOrDefault(x => x.CultureName == _localizationService.CurrentCulture.Name)
            ?? SupportedLanguages.FirstOrDefault();

        SelectedFontSize = _fontService.CurrentFontSizePreference;

        SelectedUiFont = _fontService.AvailableUiFonts
            .FirstOrDefault(x => x.Key == _fontService.CurrentUiFontFamily)
            ?? _fontService.AvailableUiFonts.FirstOrDefault(x => x.Key == "SystemDefault")
            ?? _fontService.AvailableUiFonts.FirstOrDefault();

        SelectedHeadingFont = _fontService.AvailableUiFonts
            .FirstOrDefault(x => x.Key == _fontService.CurrentHeadingFontFamily)
            ?? SelectedUiFont
            ?? _fontService.AvailableUiFonts.FirstOrDefault(x => x.Key == "SystemDefault")
            ?? _fontService.AvailableUiFonts.FirstOrDefault();

        SelectedButtonFont = _fontService.AvailableUiFonts
            .FirstOrDefault(x => x.Key == _fontService.CurrentButtonFontFamily)
            ?? SelectedUiFont
            ?? _fontService.AvailableUiFonts.FirstOrDefault(x => x.Key == "SystemDefault")
            ?? _fontService.AvailableUiFonts.FirstOrDefault();

        _isInitializing = false;
        RefreshFontPreviewState();
    }

    // Uninstall
    public bool IsUninstallSupported => _uninstallService.IsSupported;

    public bool IsPromptingUninstall
    {
        get => _isPromptingUninstall;
        set => SetProperty(ref _isPromptingUninstall, value);
    }

    public IRelayCommand PromptUninstallCommand { get; }
    public IRelayCommand CancelUninstallCommand { get; }
    public IAsyncRelayCommand ConfirmUninstallCommand { get; }

    private async Task ConfirmUninstallAsync()
    {
        ClearError();
        IsLoading = true;
        try
        {
            await _uninstallService.UninstallAsync();
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsPromptingUninstall = false;
        }
        finally
        {
            IsLoading = false;
        }
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

    public IAsyncRelayCommand ApplyFontSettingsCommand { get; }

    private string? _selectedFontSize;
    public string? SelectedFontSize
    {
        get => _selectedFontSize;
        set
        {
            if (SetProperty(ref _selectedFontSize, value) && value is not null && !_isInitializing)
            {
                RefreshFontPreviewState();
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
                RefreshFontPreviewState();
            }
        }
    }

    private FontOption? _selectedHeadingFont;
    public FontOption? SelectedHeadingFont
    {
        get => _selectedHeadingFont;
        set
        {
            if (SetProperty(ref _selectedHeadingFont, value) && value is not null && !_isInitializing)
            {
                RefreshFontPreviewState();
            }
        }
    }

    private FontOption? _selectedButtonFont;
    public FontOption? SelectedButtonFont
    {
        get => _selectedButtonFont;
        set
        {
            if (SetProperty(ref _selectedButtonFont, value) && value is not null && !_isInitializing)
            {
                RefreshFontPreviewState();
            }
        }
    }

    // Localized labels
    public string Title => _localizationService.GetString("settings.title");
    public string LanguageLabel => _localizationService.GetString("settings.language");
    public string FontSectionTitle => _localizationService.GetString("settings.font_section");
    public string FontSizeLabel => _localizationService.GetString("settings.font_size");
    public string UiFontLabel => _localizationService.GetString("settings.ui_font");
    public string HeadingFontLabel => _localizationService.GetString("settings.heading_font");
    public string ButtonFontLabel => _localizationService.GetString("settings.button_font");
    public string ApplyFontSettingsLabel => _localizationService.GetString("settings.apply");

    public string FontSizeSmallLabel => _localizationService.GetString("settings.font_size_small");
    public string FontSizeMediumLabel => _localizationService.GetString("settings.font_size_medium");
    public string FontSizeLargeLabel => _localizationService.GetString("settings.font_size_large");

    // Preview section
    public string PreviewLabel => _localizationService.GetString("settings.preview_label");
    public string PreviewHeadingText => _localizationService.GetString("settings.preview_heading_text");
    public string PreviewUiText => _localizationService.GetString("settings.preview_ui_text");
    public string PreviewButtonText => _localizationService.GetString("settings.preview_button_text");

    // Uninstall labels
    public string DangerZoneSectionTitle => _localizationService.GetString("settings.danger_zone");
    public string UninstallButtonLabel => _localizationService.GetString("settings.uninstall_button");
    public string UninstallWarningLabel => _localizationService.GetString("settings.uninstall_warning");
    public string UninstallConfirmButtonLabel => _localizationService.GetString("settings.uninstall_confirm_button");
    public string UninstallCancelButtonLabel => _localizationService.GetString("settings.uninstall_cancel_button");

    public double PreviewFontSizeScale => GetFontSizeScale(SelectedFontSize ?? _fontService.CurrentFontSizePreference);
    public double PreviewHeadingFontSize => 22 * PreviewFontSizeScale;
    public double PreviewButtonFontSize => 14 * PreviewFontSizeScale;

    public FontFamily PreviewUiFont =>
        new(SelectedUiFont?.FontFamilyValue ?? ResolveUiFontFamilyValue(_fontService.CurrentUiFontFamily));

    public FontFamily PreviewHeadingFont =>
        new(SelectedHeadingFont?.FontFamilyValue ?? ResolveUiFontFamilyValue(_fontService.CurrentHeadingFontFamily));

    public FontFamily PreviewButtonFont =>
        new(SelectedButtonFont?.FontFamilyValue ?? ResolveUiFontFamilyValue(_fontService.CurrentButtonFontFamily));

    public bool HasPendingFontSettings =>
        !string.Equals(SelectedFontSize, _fontService.CurrentFontSizePreference, StringComparison.Ordinal) ||
        !string.Equals(SelectedUiFont?.Key, _fontService.CurrentUiFontFamily, StringComparison.Ordinal) ||
        !string.Equals(SelectedHeadingFont?.Key, _fontService.CurrentHeadingFontFamily, StringComparison.Ordinal) ||
        !string.Equals(SelectedButtonFont?.Key, _fontService.CurrentButtonFontFamily, StringComparison.Ordinal);

    private bool CanApplyFontSettings() => HasPendingFontSettings && !IsLoading;

    private void RefreshFontPreviewState()
    {
        OnPropertyChanged(nameof(PreviewFontSizeScale));
        OnPropertyChanged(nameof(PreviewHeadingFontSize));
        OnPropertyChanged(nameof(PreviewButtonFontSize));
        OnPropertyChanged(nameof(PreviewUiFont));
        OnPropertyChanged(nameof(PreviewHeadingFont));
        OnPropertyChanged(nameof(PreviewButtonFont));
        OnPropertyChanged(nameof(HasPendingFontSettings));
        ApplyFontSettingsCommand.NotifyCanExecuteChanged();
    }

    private async Task ApplyFontSettingsAsync()
    {
        var size = SelectedFontSize ?? _fontService.CurrentFontSizePreference;
        var ui = SelectedUiFont?.Key ?? _fontService.CurrentUiFontFamily;
        var heading = SelectedHeadingFont?.Key ?? _fontService.CurrentHeadingFontFamily;
        var button = SelectedButtonFont?.Key ?? _fontService.CurrentButtonFontFamily;
        var code = _fontService.CurrentContentFontFamily;

        ClearError();
        IsLoading = true;
        ApplyFontSettingsCommand.NotifyCanExecuteChanged();

        try
        {
            var succeeded = await ErrorHandler.TryExecuteAsync(
                async () =>
                {
                    await _appSettingsRepository.UpdateFontSettingsAsync(
                        size,
                        ui,
                        heading,
                        button,
                        code);

                    _fontService.ApplyFontSettings(size, ui, heading, button, code);
                },
                "Apply font settings");

            if (!succeeded)
            {
                ErrorMessage = _localizationService.GetString("common.error_unknown");
                return;
            }

            RefreshFontPreviewState();
        }
        finally
        {
            IsLoading = false;
            ApplyFontSettingsCommand.NotifyCanExecuteChanged();
        }
    }

    private double GetFontSizeScale(string fontSizePreference) => fontSizePreference switch
    {
        "Small" => 0.85,
        "Large" => 1.2,
        _ => 1.0
    };

    private string ResolveUiFontFamilyValue(string key)
    {
        var option = _fontService.AvailableUiFonts.FirstOrDefault(x => x.Key == key);
        return option?.FontFamilyValue ?? key;
    }

    private async Task PersistLanguagePreferenceAsync(string cultureName)
    {
        _ = await ErrorHandler.TryExecuteAsync(
            () => _appSettingsRepository.UpdateLanguageAsync(cultureName),
            "Persist language preference");
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
        OnPropertyChanged(nameof(HeadingFontLabel));
        OnPropertyChanged(nameof(ButtonFontLabel));
        OnPropertyChanged(nameof(ApplyFontSettingsLabel));
        OnPropertyChanged(nameof(FontSizeSmallLabel));
        OnPropertyChanged(nameof(FontSizeMediumLabel));
        OnPropertyChanged(nameof(FontSizeLargeLabel));
        OnPropertyChanged(nameof(PreviewLabel));
        OnPropertyChanged(nameof(PreviewHeadingText));
        OnPropertyChanged(nameof(PreviewUiText));
        OnPropertyChanged(nameof(PreviewButtonText));
        OnPropertyChanged(nameof(DangerZoneSectionTitle));
        OnPropertyChanged(nameof(UninstallButtonLabel));
        OnPropertyChanged(nameof(UninstallWarningLabel));
        OnPropertyChanged(nameof(UninstallConfirmButtonLabel));
        OnPropertyChanged(nameof(UninstallCancelButtonLabel));
    }
}

