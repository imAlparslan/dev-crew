using DevCrew.Core.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DevCrew.Core.Infrastructure.Persistence.Repositories;

/// <summary>
/// Repository implementation for GUID history data access.
/// Handles all database operations related to GUID management.
/// </summary>
public class GuidRepository : IGuidRepository
{
    private readonly AppDbContext _dbContext;

    public GuidRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<GuidHistory> SaveGuidAsync(string guidValue, string? notes = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(guidValue))
            throw new ArgumentException("GUID value cannot be empty", nameof(guidValue));

        var guidHistory = new GuidHistory
        {
            GuidValue = guidValue,
            CreatedAt = DateTime.UtcNow,
            Notes = notes
        };

        _dbContext.GuidHistories.Add(guidHistory);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return guidHistory;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteGuidAsync(int id, CancellationToken cancellationToken = default)
    {
        var affectedRows = await _dbContext.GuidHistories
                    .Where(g => g.Id == id)
                    .ExecuteDeleteAsync(cancellationToken);

        return affectedRows > 0;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateGuidNotesAsync(int id, string? notes, CancellationToken cancellationToken = default)
    {
        var affectedRows = await _dbContext.GuidHistories
                    .Where(g => g.Id == id)
                    .ExecuteUpdateAsync(setter => setter.SetProperty(g => g.Notes, notes), cancellationToken);

        return affectedRows > 0;
    }

    /// <inheritdoc/>
    public async Task<List<GuidHistory>> GetGuidsPagedAsync(
        int skip,
        int take,
        string? searchQuery = null,
        CancellationToken cancellationToken = default,
        bool prefixSearch = false)
    {
        var query = _dbContext.GuidHistories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var normalizedQuery = searchQuery.Trim();

            if (prefixSearch)
            {
                query = query.Where(g =>
                    g.GuidValue.StartsWith(normalizedQuery) ||
                    (g.Notes != null && g.Notes.StartsWith(normalizedQuery)));
            }
            else
            {
                query = query.Where(g =>
                    g.GuidValue.Contains(normalizedQuery) ||
                    (g.Notes != null && g.Notes.Contains(normalizedQuery)));
            }
        }

        return await query
            .OrderByDescending(g => g.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetGuidCountAsync(
        string? searchQuery = null,
        CancellationToken cancellationToken = default,
        bool prefixSearch = false)
    {
        var query = _dbContext.GuidHistories.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            var normalizedQuery = searchQuery.Trim();

            if (prefixSearch)
            {
                query = query.Where(g =>
                    g.GuidValue.StartsWith(normalizedQuery) ||
                    (g.Notes != null && g.Notes.StartsWith(normalizedQuery)));
            }
            else
            {
                query = query.Where(g =>
                    g.GuidValue.Contains(normalizedQuery) ||
                    (g.Notes != null && g.Notes.Contains(normalizedQuery)));
            }
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<GuidHistory?> GetGuidByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.GuidHistories.FindAsync(new object[] { id }, cancellationToken);
    }

    public Task<List<GuidHistory>> GetGuidByValueAndNotes(
        string? value,
        string? notes,
        CancellationToken cancellationToken = default,
        int? maxResults = null)
    {
        if (string.IsNullOrWhiteSpace(value) && string.IsNullOrWhiteSpace(notes))
            throw new ArgumentException("At least one of value or notes must be provided.");

        var query = _dbContext.GuidHistories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(value))
            query = query.Where(g => g.GuidValue.StartsWith(value));

        if (!string.IsNullOrWhiteSpace(notes))
            query = query.Where(g => g.Notes != null && g.Notes.StartsWith(notes));

        if (maxResults.HasValue && maxResults.Value > 0)
            query = query.Take(maxResults.Value);

        return query.ToListAsync(cancellationToken);
    }
}
