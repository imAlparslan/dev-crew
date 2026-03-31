using DevCrew.Core.Domain.Models;

namespace DevCrew.Core.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository contract for persisted application settings.
/// </summary>
public interface IAppSettingsRepository
{
    /// <summary>
    /// Gets the singleton settings row, creating it when missing.
    /// </summary>
    Task<AppSettings> GetOrCreateAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates only the language preference.
    /// </summary>
    Task<bool> UpdateLanguageAsync(string languageCultureName, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates font-related preferences.
    /// </summary>
    Task<bool> UpdateFontSettingsAsync(
        string fontSizePreference,
        string uiFontFamily,
        string headingFontFamily,
        string buttonFontFamily,
        string contentFontFamily,
        CancellationToken cancellationToken = default);
}
