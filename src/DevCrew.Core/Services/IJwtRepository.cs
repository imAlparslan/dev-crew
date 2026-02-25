using DevCrew.Core.Models;

namespace DevCrew.Core.Services;

/// <summary>
/// Repository interface for JWT history data access operations.
/// Abstracts database operations from business logic.
/// </summary>
public interface IJwtRepository
{
    /// <summary>
    /// Saves a decoded JWT to the database.
    /// </summary>
    /// <param name="token">The JWT token string</param>
    /// <param name="header">Decoded header JSON</param>
    /// <param name="payload">Decoded payload JSON</param>
    /// <param name="expiresAt">Token expiration time</param>
    /// <param name="issuer">Token issuer</param>
    /// <param name="audience">Token audience</param>
    /// <param name="notes">Optional notes</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The created JwtHistory entity with assigned ID</returns>
    Task<JwtHistory> SaveJwtAsync(
        string token, 
        string? header = null, 
        string? payload = null, 
        DateTime? expiresAt = null, 
        string? issuer = null, 
        string? audience = null, 
        string? notes = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a JWT from the database.
    /// </summary>
    /// <param name="id">The ID of the JWT history record to delete</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully deleted, false otherwise</returns>
    Task<bool> DeleteJwtAsync(int id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates the notes for a saved JWT.
    /// </summary>
    /// <param name="id">The ID of the JWT history record</param>
    /// <param name="notes">The new notes value</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True if successfully updated, false if record not found</returns>
    Task<bool> UpdateJwtNotesAsync(int id, string? notes, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets paginated JWT history records with optional search filtering.
    /// </summary>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="searchQuery">Optional search term to filter by token, issuer, or audience</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of JwtHistory records matching the criteria</returns>
    Task<List<JwtHistory>> GetJwtsPagedAsync(int skip, int take, string? searchQuery = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets total count of saved JWTs, optionally filtered by search query.
    /// </summary>
    /// <param name="searchQuery">Optional search term to filter by token, issuer, or audience</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Total count of JWTs matching the criteria</returns>
    Task<int> GetJwtCountAsync(string? searchQuery = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a specific JWT by its ID.
    /// </summary>
    /// <param name="id">The ID of the JWT history record</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>The JwtHistory record or null if not found</returns>
    Task<JwtHistory?> GetJwtByIdAsync(int id, CancellationToken cancellationToken = default);
}
