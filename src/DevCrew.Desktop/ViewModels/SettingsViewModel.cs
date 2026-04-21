using Avalonia.Media;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using DevCrew.Desktop.Services;
using System.IO;
using System.Reflection;
using System.Xml.Linq;

namespace DevCrew.Desktop.ViewModels;

public class SettingsViewModel : BaseViewModel
{
    private enum UpdateStatusState
    {
        Idle,
        Checking,
        UpToDate,
        UpdateAvailable,
        UpdateStarted,
        Failed
    }

    private readonly ILocalizationService _localizationService;
    private readonly IAppSettingsRepository _appSettingsRepository;
    private readonly IFontService _fontService;
    private readonly IUninstallService _uninstallService;
    private readonly IUpdateService _updateService;
    private readonly bool _isInitializing;

    private bool _isPromptingUninstall;
    private bool _isCheckingForUpdates;
    private bool _isUpdating;
    private bool _isUpdateAvailable;
    private string _currentVersion = string.Empty;
    private string? _latestVersion;
    private string _updateStatusMessage = string.Empty;
    private UpdateStatusState _updateStatus = UpdateStatusState.Idle;

    public SettingsViewModel(
        IErrorHandler errorHandler,
        ILocalizationService localizationService,
        IAppSettingsRepository appSettingsRepository,
        IFontService fontService,
        IUninstallService uninstallService,
        IUpdateService updateService)
        : base(errorHandler)
    {
        _localizationService = localizationService;
        _appSettingsRepository = appSettingsRepository;
        _fontService = fontService;
        _uninstallService = uninstallService;
        _updateService = updateService;

        ApplyFontSettingsCommand = new AsyncRelayCommand(ApplyFontSettingsAsync, CanApplyFontSettings);
        PromptUninstallCommand = new RelayCommand(() => IsPromptingUninstall = true);
        CancelUninstallCommand = new RelayCommand(() => IsPromptingUninstall = false);
        ConfirmUninstallCommand = new AsyncRelayCommand(ConfirmUninstallAsync);
        CheckForUpdatesCommand = new AsyncRelayCommand(CheckForUpdatesAsync, CanCheckForUpdates);
        StartUpdateCommand = new AsyncRelayCommand(StartUpdateAsync, CanStartUpdate);

        _localizationService.LanguageChanged += OnLanguageChanged;
        SupportedLanguages = _localizationService.SupportedLanguages;

        _isInitializing = true;

        SelectedLanguage = SupportedLanguages.FirstOrDefault(x => x.CultureName == _localizationService.CurrentCulture.Name)
            ?? (SupportedLanguages.Count > 0 ? SupportedLanguages[0] : null);

        SelectedFontSize = _fontService.CurrentFontSizePreference;

        SelectedUiFont = _fontService.AvailableUiFonts
            .FirstOrDefault(x => x.Key == _fontService.CurrentUiFontFamily)
            ?? _fontService.AvailableUiFonts.FirstOrDefault(x => x.Key == "SystemDefault")
            ?? (_fontService.AvailableUiFonts.Count > 0 ? _fontService.AvailableUiFonts[0] : null);

        SelectedHeadingFont = _fontService.AvailableUiFonts
            .FirstOrDefault(x => x.Key == _fontService.CurrentHeadingFontFamily)
            ?? SelectedUiFont
            ?? _fontService.AvailableUiFonts.FirstOrDefault(x => x.Key == "SystemDefault")
            ?? (_fontService.AvailableUiFonts.Count > 0 ? _fontService.AvailableUiFonts[0] : null);

        SelectedButtonFont = _fontService.AvailableUiFonts
            .FirstOrDefault(x => x.Key == _fontService.CurrentButtonFontFamily)
            ?? SelectedUiFont
            ?? _fontService.AvailableUiFonts.FirstOrDefault(x => x.Key == "SystemDefault")
            ?? (_fontService.AvailableUiFonts.Count > 0 ? _fontService.AvailableUiFonts[0] : null);

        CurrentVersion = ResolveCurrentVersion();
        RefreshUpdateStatus(UpdateStatusState.Idle);

        _isInitializing = false;
        RefreshFontPreviewState();
        RefreshUpdateCommandsState();
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

    // Update
    public IAsyncRelayCommand CheckForUpdatesCommand { get; }
    public IAsyncRelayCommand StartUpdateCommand { get; }

    public bool IsCheckingForUpdates
    {
        get => _isCheckingForUpdates;
        private set
        {
            if (SetProperty(ref _isCheckingForUpdates, value))
            {
                OnPropertyChanged(nameof(IsUpdateOperationInProgress));
            }
        }
    }

    public bool IsUpdating
    {
        get => _isUpdating;
        private set
        {
            if (SetProperty(ref _isUpdating, value))
            {
                OnPropertyChanged(nameof(IsUpdateOperationInProgress));
            }
        }
    }

    public bool IsUpdateOperationInProgress => IsCheckingForUpdates || IsUpdating;

    public bool IsUpdateAvailable
    {
        get => _isUpdateAvailable;
        private set => SetProperty(ref _isUpdateAvailable, value);
    }

    public string CurrentVersion
    {
        get => _currentVersion;
        private set => SetProperty(ref _currentVersion, value);
    }

    public string? LatestVersion
    {
        get => _latestVersion;
        private set
        {
            if (SetProperty(ref _latestVersion, value))
            {
                OnPropertyChanged(nameof(LatestVersionDisplay));
            }
        }
    }

    public string LatestVersionDisplay => string.IsNullOrWhiteSpace(LatestVersion)
        ? _localizationService.GetString("settings.update.latest_version_unknown")
        : LatestVersion;

    public string UpdateStatusMessage
    {
        get => _updateStatusMessage;
        private set => SetProperty(ref _updateStatusMessage, value);
    }

    // Language
    public IReadOnlyList<SupportedLanguage> SupportedLanguages { get; }

    private SupportedLanguage? _selectedLanguage;
    public SupportedLanguage? SelectedLanguage
    {
        get => _selectedLanguage;
        set
        {
            if (SetProperty(ref _selectedLanguage, value) && value is not null && _localizationService.SetLanguage(value.CultureName) && !_isInitializing)
            {
                _ = PersistLanguagePreferenceAsync(value.CultureName);
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

    // Update labels
    public string UpdateSectionTitle => _localizationService.GetString("settings.update.section_title");
    public string CurrentVersionLabel => _localizationService.GetString("settings.update.current_version");
    public string LatestVersionLabel => _localizationService.GetString("settings.update.latest_version");
    public string CheckForUpdatesLabel => _localizationService.GetString("settings.update.check_button");
    public string UpdateNowLabel => _localizationService.GetString("settings.update.update_button");

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

    private bool CanCheckForUpdates() => !IsUpdateOperationInProgress;

    private bool CanStartUpdate() => IsUpdateAvailable && !IsUpdateOperationInProgress;

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

    private void RefreshUpdateCommandsState()
    {
        CheckForUpdatesCommand.NotifyCanExecuteChanged();
        StartUpdateCommand.NotifyCanExecuteChanged();
    }

    private void RefreshUpdateStatus(UpdateStatusState state)
    {
        _updateStatus = state;

        UpdateStatusMessage = state switch
        {
            UpdateStatusState.Checking => _localizationService.GetString("settings.update.status_checking"),
            UpdateStatusState.UpToDate => _localizationService.GetString("settings.update.status_up_to_date"),
            UpdateStatusState.UpdateAvailable => string.Format(
                _localizationService.GetString("settings.update.status_available"),
                LatestVersionDisplay),
            UpdateStatusState.UpdateStarted => string.Format(
                _localizationService.GetString("settings.update.status_started"),
                LatestVersionDisplay),
            UpdateStatusState.Failed => _localizationService.GetString("settings.update.status_failed"),
            _ => _localizationService.GetString("settings.update.status_idle")
        };
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

    private async Task CheckForUpdatesAsync()
    {
        ClearError();
        IsCheckingForUpdates = true;
        RefreshUpdateStatus(UpdateStatusState.Checking);
        RefreshUpdateCommandsState();

        try
        {
            var result = await _updateService.CheckForUpdatesAsync();

            CurrentVersion = result.CurrentVersion;
            LatestVersion = result.LatestVersion;
            IsUpdateAvailable = result.IsUpdateAvailable;

            RefreshUpdateStatus(IsUpdateAvailable
                ? UpdateStatusState.UpdateAvailable
                : UpdateStatusState.UpToDate);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            IsUpdateAvailable = false;
            RefreshUpdateStatus(UpdateStatusState.Failed);
        }
        finally
        {
            IsCheckingForUpdates = false;
            RefreshUpdateCommandsState();
        }
    }

    private async Task StartUpdateAsync()
    {
        if (!IsUpdateAvailable || string.IsNullOrWhiteSpace(LatestVersion))
        {
            return;
        }

        ClearError();
        IsUpdating = true;
        RefreshUpdateCommandsState();

        try
        {
            await _updateService.StartUpdateAsync(LatestVersion);
            RefreshUpdateStatus(UpdateStatusState.UpdateStarted);
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            RefreshUpdateStatus(UpdateStatusState.Failed);
        }
        finally
        {
            IsUpdating = false;
            RefreshUpdateCommandsState();
        }
    }

    private static double GetFontSizeScale(string fontSizePreference) => fontSizePreference switch
    {
        "Small" => 0.85,
        "Large" => 1.2,
        _ => 1.0
    };

    private static string ResolveCurrentVersion()
    {
        var entryAssembly = Assembly.GetEntryAssembly() ?? typeof(SettingsViewModel).Assembly;
        var informationalVersion = entryAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var normalized = informationalVersion.Split('+')[0].Trim();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        var bundleVersion = TryResolveMacBundleVersion();
        if (!string.IsNullOrWhiteSpace(bundleVersion))
        {
            return bundleVersion;
        }

        var version = entryAssembly.GetName().Version;

        if (version is null)
        {
            return "0.0.0";
        }

        var build = version.Build < 0 ? 0 : version.Build;
        return $"{version.Major}.{version.Minor}.{build}";
    }

    private static string? TryResolveMacBundleVersion()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var macOsDir = new DirectoryInfo(baseDir);
            var contentsDir = macOsDir.Parent;

            if (contentsDir is null || !string.Equals(contentsDir.Name, "Contents", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var infoPlistPath = Path.Combine(contentsDir.FullName, "Info.plist");
            if (!File.Exists(infoPlistPath))
            {
                return null;
            }

            var document = XDocument.Load(infoPlistPath);
            var dictElement = document.Root?.Element("dict");
            if (dictElement is null)
            {
                return null;
            }

            var children = dictElement.Elements().ToList();
            for (var i = 0; i < children.Count - 1; i++)
            {
                var keyElement = children[i];
                if (!string.Equals(keyElement.Name.LocalName, "key", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(keyElement.Value, "CFBundleShortVersionString", StringComparison.Ordinal))
                {
                    continue;
                }

                var valueElement = children[i + 1];
                if (!string.Equals(valueElement.Name.LocalName, "string", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var value = valueElement.Value?.Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch
        {
            // Ignore plist parsing errors and continue with assembly fallback.
        }

        return null;
    }

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

        OnPropertyChanged(nameof(UpdateSectionTitle));
        OnPropertyChanged(nameof(CurrentVersionLabel));
        OnPropertyChanged(nameof(LatestVersionLabel));
        OnPropertyChanged(nameof(CheckForUpdatesLabel));
        OnPropertyChanged(nameof(UpdateNowLabel));
        OnPropertyChanged(nameof(LatestVersionDisplay));
        RefreshUpdateStatus(_updateStatus);

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
