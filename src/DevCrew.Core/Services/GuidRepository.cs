using DevCrew.Core.Data;
using DevCrew.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DevCrew.Core.Services;

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
    public async Task<GuidHistory> SaveGuidAsync(string guidValue, string? notes = null)
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
        await _dbContext.SaveChangesAsync();

        return guidHistory;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteGuidAsync(int id)
    {
        var historyItem = await _dbContext.GuidHistories.FindAsync(id);
        if (historyItem == null)
            return false;

        _dbContext.GuidHistories.Remove(historyItem);
        await _dbContext.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateGuidNotesAsync(int id, string? notes)
    {
        var historyItem = await _dbContext.GuidHistories.FindAsync(id);
        if (historyItem == null)
            return false;

        historyItem.Notes = notes;
        await _dbContext.SaveChangesAsync();

        return true;
    }

    /// <inheritdoc/>
    public async Task<List<GuidHistory>> GetGuidsPagedAsync(int skip, int take, string? searchQuery = null)
    {
        var query = _dbContext.GuidHistories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            // Search in both GUID value and notes
            query = query.Where(g => 
                g.GuidValue.Contains(searchQuery) || 
                (g.Notes != null && g.Notes.Contains(searchQuery)));
        }

        return await query
            .OrderByDescending(g => g.CreatedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync();
    }

    /// <inheritdoc/>
    public async Task<int> GetGuidCountAsync(string? searchQuery = null)
    {
        var query = _dbContext.GuidHistories.AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            // Search in both GUID value and notes
            query = query.Where(g => 
                g.GuidValue.Contains(searchQuery) || 
                (g.Notes != null && g.Notes.Contains(searchQuery)));
        }

        return await query.CountAsync();
    }

    /// <inheritdoc/>
    public async Task<GuidHistory?> GetGuidByIdAsync(int id)
    {
        return await _dbContext.GuidHistories.FindAsync(id);
    }
}
