using DevCrew.Core.Infrastructure.Persistence;
using DevCrew.Core.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DevCrew.Core.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for application settings persistence.
/// </summary>
public class AppSettingsRepository : IAppSettingsRepository
{
    private readonly AppDbContext _dbContext;

    public AppSettingsRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    public async Task<AppSettings> GetOrCreateAsync(CancellationToken cancellationToken = default)
    {
        var settings = await _dbContext.AppSettings
            .FirstOrDefaultAsync(x => x.Id == AppSettings.SingletonId, cancellationToken);

        if (settings != null)
        {
            return settings;
        }

        settings = new AppSettings
        {
            Id = AppSettings.SingletonId,
            LanguageCultureName = AppSettings.DefaultLanguageCultureName,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        _dbContext.AppSettings.Add(settings);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return settings;
    }

    public async Task<bool> UpdateLanguageAsync(string languageCultureName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(languageCultureName))
        {
            throw new ArgumentException("Language culture name cannot be empty", nameof(languageCultureName));
        }

        var settings = await GetOrCreateAsync(cancellationToken);
        if (string.Equals(settings.LanguageCultureName, languageCultureName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        settings.LanguageCultureName = languageCultureName;
        settings.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> UpdateFontSettingsAsync(
        string fontSizePreference,
        string uiFontFamily,
        string headingFontFamily,
        string buttonFontFamily,
        string contentFontFamily,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(fontSizePreference))
            throw new ArgumentException("Font size preference cannot be empty", nameof(fontSizePreference));
        if (string.IsNullOrWhiteSpace(uiFontFamily))
            throw new ArgumentException("UI font family cannot be empty", nameof(uiFontFamily));
        if (string.IsNullOrWhiteSpace(headingFontFamily))
            throw new ArgumentException("Heading font family cannot be empty", nameof(headingFontFamily));
        if (string.IsNullOrWhiteSpace(buttonFontFamily))
            throw new ArgumentException("Button font family cannot be empty", nameof(buttonFontFamily));
        if (string.IsNullOrWhiteSpace(contentFontFamily))
            throw new ArgumentException("Content font family cannot be empty", nameof(contentFontFamily));

        var settings = await GetOrCreateAsync(cancellationToken);
        settings.FontSizePreference = fontSizePreference;
        settings.UiFontFamily = uiFontFamily;
        settings.HeadingFontFamily = headingFontFamily;
        settings.ButtonFontFamily = buttonFontFamily;
        settings.ContentFontFamily = contentFontFamily;
        settings.UpdatedAt = DateTime.UtcNow;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }
}
