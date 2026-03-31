using DevCrew.Core.Domain.Models;

namespace DevCrew.Core.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository interface for JWT Builder template data access operations.
/// Abstracts database operations from business logic.
/// </summary>
public interface IJwtBuilderTemplateRepository
{
    /// <summary>
    /// Saves a new JWT Builder template to the database.
    /// </summary>
    /// <param name="template">The template to save</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created template entity with assigned ID</returns>
    Task<JwtBuilderTemplate> SaveAsync(JwtBuilderTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing JWT Builder template in the database.
    /// </summary>
    /// <param name="template">The template with updated values</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully updated, false if record not found</returns>
    Task<bool> UpdateAsync(JwtBuilderTemplate template, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a JWT Builder template from the database.
    /// </summary>
    /// <param name="id">The ID of the template to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully deleted, false otherwise</returns>
    Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all JWT Builder templates (lightweight query for dropdown/list).
    /// Returns only essential fields for listing purposes.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of all templates ordered by name</returns>
    Task<List<JwtBuilderTemplate>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single JWT Builder template by ID with all details.
    /// </summary>
    /// <param name="id">The template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The template if found, null otherwise</returns>
    Task<JwtBuilderTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the last used timestamp for a template.
    /// Used to track when a template was last loaded.
    /// </summary>
    /// <param name="id">The template ID</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully updated, false if record not found</returns>
    Task<bool> UpdateLastUsedAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Checks if a template name already exists in the database.
    /// Used for validation before saving new templates.
    /// </summary>
    /// <param name="templateName">The template name to check</param>
    /// <param name="excludeId">Optional ID to exclude from check (for updates)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if the name exists, false otherwise</returns>
    Task<bool> TemplateNameExistsAsync(string templateName, int? excludeId = null, CancellationToken cancellationToken = default);
}
