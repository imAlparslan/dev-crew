using DevCrew.Core.Models;

namespace DevCrew.Core.Services;

/// <summary>
/// Repository interface for GUID history data access operations.
/// Abstracts database operations from business logic.
/// </summary>
public interface IGuidRepository
{
    /// <summary>
    /// Saves a new GUID to the database.
    /// </summary>
    /// <param name="guidValue">The GUID value to save</param>
    /// <param name="notes">Optional notes associated with the GUID</param>
    /// <returns>The created GuidHistory entity with assigned ID</returns>
    Task<GuidHistory> SaveGuidAsync(string guidValue, string? notes = null);

    /// <summary>
    /// Deletes a GUID from the database.
    /// </summary>
    /// <param name="id">The ID of the GUID history record to delete</param>
    /// <returns>True if successfully deleted, false otherwise</returns>
    Task<bool> DeleteGuidAsync(int id);

    /// <summary>
    /// Updates the notes for a saved GUID.
    /// </summary>
    /// <param name="id">The ID of the GUID history record</param>
    /// <param name="notes">The new notes value</param>
    /// <returns>True if successfully updated, false if record not found</returns>
    Task<bool> UpdateGuidNotesAsync(int id, string? notes);

    /// <summary>
    /// Gets paginated GUID history records with optional search filtering.
    /// </summary>
    /// <param name="skip">Number of records to skip</param>
    /// <param name="take">Number of records to take</param>
    /// <param name="searchQuery">Optional search term to filter by GUID value or notes</param>
    /// <returns>List of GuidHistory records matching the criteria</returns>
    Task<List<GuidHistory>> GetGuidsPagedAsync(int skip, int take, string? searchQuery = null);

    /// <summary>
    /// Gets total count of saved GUIDs, optionally filtered by search query.
    /// </summary>
    /// <param name="searchQuery">Optional search term to filter by GUID value or notes</param>
    /// <returns>Total count of GUIDs matching the criteria</returns>
    Task<int> GetGuidCountAsync(string? searchQuery = null);

    /// <summary>
    /// Gets a specific GUID by its ID.
    /// </summary>
    /// <param name="id">The ID of the GUID history record</param>
    /// <returns>The GuidHistory record or null if not found</returns>
    Task<GuidHistory?> GetGuidByIdAsync(int id);
}
