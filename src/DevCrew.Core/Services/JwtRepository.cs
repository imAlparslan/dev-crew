using DevCrew.Core.Data;
using DevCrew.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DevCrew.Core.Services;

/// <summary>
/// Repository implementation for JWT history data access.
/// Handles all database operations related to JWT management.
/// </summary>
public class JwtRepository : IJwtRepository
{
    private readonly AppDbContext _dbContext;

    public JwtRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<JwtHistory> SaveJwtAsync(
        string token, 
        string? header = null, 
        string? payload = null, 
        DateTime? expiresAt = null, 
        string? issuer = null, 
        string? audience = null, 
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(token))
            throw new ArgumentException("JWT token cannot be empty", nameof(token));

        var jwtHistory = new JwtHistory
        {
            Token = token,
            DecodedAt = DateTime.UtcNow,
            Header = header,
            Payload = payload,
            ExpiresAt = expiresAt,
            Issuer = issuer,
            Audience = audience,
            Notes = notes
        };

        _dbContext.JwtHistories.Add(jwtHistory);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return jwtHistory;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteJwtAsync(int id, CancellationToken cancellationToken = default)
    {
        _ = await _dbContext.JwtHistories
                    .Where(j => j.Id == id)
                    .ExecuteDeleteAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateJwtNotesAsync(int id, string? notes, CancellationToken cancellationToken = default)
    {
        _ = await _dbContext.JwtHistories
                    .Where(j => j.Id == id)
                    .ExecuteUpdateAsync(setter => setter.SetProperty(j => j.Notes, notes), cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public async Task<List<JwtHistory>> GetJwtsPagedAsync(int skip, int take, string? searchQuery = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.JwtHistories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            // Search in token, issuer, audience, and notes
            query = query.Where(j =>
                j.Token.Contains(searchQuery) ||
                (j.Issuer != null && j.Issuer.Contains(searchQuery)) ||
                (j.Audience != null && j.Audience.Contains(searchQuery)) ||
                (j.Notes != null && j.Notes.Contains(searchQuery)));
        }

        return await query
            .OrderByDescending(j => j.DecodedAt)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<int> GetJwtCountAsync(string? searchQuery = null, CancellationToken cancellationToken = default)
    {
        var query = _dbContext.JwtHistories.AsNoTracking().AsQueryable();

        if (!string.IsNullOrWhiteSpace(searchQuery))
        {
            // Search in token, issuer, audience, and notes
            query = query.Where(j =>
                j.Token.Contains(searchQuery) ||
                (j.Issuer != null && j.Issuer.Contains(searchQuery)) ||
                (j.Audience != null && j.Audience.Contains(searchQuery)) ||
                (j.Notes != null && j.Notes.Contains(searchQuery)));
        }

        return await query.CountAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<JwtHistory?> GetJwtByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.JwtHistories.FindAsync(new object[] { id }, cancellationToken);
    }
}
