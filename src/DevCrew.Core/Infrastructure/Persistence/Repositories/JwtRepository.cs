using DevCrew.Core.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace DevCrew.Core.Infrastructure.Persistence.Repositories;

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
        SaveJwtRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (string.IsNullOrWhiteSpace(request.Token))
            throw new ArgumentException("JWT token cannot be empty", nameof(request));

        var jwtHistory = new JwtHistory
        {
            Token = request.Token,
            DecodedAt = DateTime.UtcNow,
            Header = request.Header,
            Payload = request.Payload,
            ExpiresAt = request.ExpiresAt,
            Issuer = request.Issuer,
            Audience = request.Audience,
            Notes = request.Notes
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
        var query = _dbContext.JwtHistories.AsNoTracking();

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
        var query = _dbContext.JwtHistories.AsNoTracking();

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
