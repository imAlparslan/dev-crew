using DevCrew.Core.Data;
using DevCrew.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DevCrew.Core.Services.Repositories;

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
}
