using DevCrew.Core.Domain.Models;

namespace DevCrew.Core.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository interface for saved regex preset persistence.
/// </summary>
public interface IRegexPresetRepository
{
    /// <summary>
    /// Saves a new regex preset.
    /// </summary>
    Task<RegexPreset> SaveAsync(RegexPreset preset, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns all saved regex presets ordered by name.
    /// </summary>
    Task<List<RegexPreset>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a saved regex preset by ID.
    /// </summary>
    Task<RegexPreset?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last-used timestamp for a preset.
    /// </summary>
    Task<bool> UpdateLastUsedAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true when a preset with the same name already exists.
    /// </summary>
    Task<bool> NameExistsAsync(string name, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a preset by ID.
    /// </summary>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing preset's pattern and options.
    /// </summary>
    Task<RegexPreset?> UpdateAsync(int id, string pattern, bool ignoreCase, bool multiline, CancellationToken cancellationToken = default);
}