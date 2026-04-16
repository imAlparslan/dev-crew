using DevCrew.Core.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DevCrew.Core.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for saved regex preset data access.
/// </summary>
public class RegexPresetRepository : IRegexPresetRepository
{
    private readonly AppDbContext _dbContext;

    public RegexPresetRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<RegexPreset> SaveAsync(RegexPreset preset, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(preset);

        if (string.IsNullOrWhiteSpace(preset.Name))
        {
            throw new ArgumentException("Preset name cannot be empty", nameof(preset));
        }

        if (string.IsNullOrWhiteSpace(preset.Pattern))
        {
            throw new ArgumentException("Regex pattern cannot be empty", nameof(preset));
        }

        var timestamp = DateTime.UtcNow;
        preset.Name = preset.Name.Trim();
        preset.CreatedAt = timestamp;
        preset.LastUsedAt = timestamp;

        _dbContext.RegexPresets.Add(preset);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return preset;
    }

    /// <inheritdoc/>
    public Task<List<RegexPreset>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.RegexPresets
            .AsNoTracking()
            .OrderBy(preset => preset.Name)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public Task<RegexPreset?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.RegexPresets
            .AsNoTracking()
            .FirstOrDefaultAsync(preset => preset.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateLastUsedAsync(int id, CancellationToken cancellationToken = default)
    {
        _ = await _dbContext.RegexPresets
            .Where(preset => preset.Id == id)
            .ExecuteUpdateAsync(
                setter => setter.SetProperty(preset => preset.LastUsedAt, DateTime.UtcNow),
                cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var result = await _dbContext.RegexPresets
            .Where(preset => preset.Id == id)
            .ExecuteDeleteAsync(cancellationToken);

        return result > 0;
    }

    /// <inheritdoc/>
    public async Task<RegexPreset?> UpdateAsync(int id, string pattern, bool ignoreCase, bool multiline, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            throw new ArgumentException("Regex pattern cannot be empty", nameof(pattern));
        }

        var preset = await _dbContext.RegexPresets.FirstOrDefaultAsync(p => p.Id == id, cancellationToken);
        if (preset == null)
        {
            return null;
        }

        preset.Pattern = pattern.Trim();
        preset.IgnoreCase = ignoreCase;
        preset.Multiline = multiline;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return preset;
    }

    /// <inheritdoc/>
    public Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Task.FromResult(false);
        }

        var normalizedName = name.Trim().ToUpperInvariant();

        return _dbContext.RegexPresets
            .AsNoTracking()
            .AnyAsync(preset => preset.Name.ToUpper() == normalizedName, cancellationToken);
    }
}
