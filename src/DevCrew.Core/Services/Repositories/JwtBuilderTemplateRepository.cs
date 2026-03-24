using DevCrew.Core.Data;
using DevCrew.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DevCrew.Core.Services.Repositories;

/// <summary>
/// Repository implementation for JWT Builder template data access.
/// Handles all database operations related to JWT template management.
/// </summary>
public class JwtBuilderTemplateRepository : IJwtBuilderTemplateRepository
{
    private readonly AppDbContext _dbContext;

    public JwtBuilderTemplateRepository(AppDbContext dbContext)
    {
        _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
    }

    /// <inheritdoc/>
    public async Task<JwtBuilderTemplate> SaveAsync(JwtBuilderTemplate template, CancellationToken cancellationToken = default)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        if (string.IsNullOrWhiteSpace(template.TemplateName))
            throw new ArgumentException("Template name cannot be empty", nameof(template));

        template.CreatedAt = DateTime.UtcNow;

        _dbContext.JwtBuilderTemplates.Add(template);
        await _dbContext.SaveChangesAsync(cancellationToken);

        return template;
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateAsync(JwtBuilderTemplate template, CancellationToken cancellationToken = default)
    {
        if (template == null)
            throw new ArgumentNullException(nameof(template));

        var existing = await _dbContext.JwtBuilderTemplates.FindAsync(new object[] { template.Id }, cancellationToken);
        if (existing == null)
            return false;

        // Update all editable fields
        existing.TemplateName = template.TemplateName;
        existing.Algorithm = template.Algorithm;
        existing.Secret = template.Secret;
        existing.PublicKey = template.PublicKey;
        existing.Issuer = template.Issuer;
        existing.Audience = template.Audience;
        existing.Subject = template.Subject;
        existing.ExpirationMinutes = template.ExpirationMinutes;
        existing.IncludeExpiration = template.IncludeExpiration;
        existing.CustomClaimsJson = template.CustomClaimsJson;
        existing.Notes = template.Notes;

        await _dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        _ = await _dbContext.JwtBuilderTemplates
                    .Where(t => t.Id == id)
                    .ExecuteDeleteAsync(cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public async Task<List<JwtBuilderTemplate>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbContext.JwtBuilderTemplates
            .AsNoTracking()
            .OrderBy(t => t.TemplateName)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<JwtBuilderTemplate?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return await _dbContext.JwtBuilderTemplates
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);
    }

    /// <inheritdoc/>
    public async Task<bool> UpdateLastUsedAsync(int id, CancellationToken cancellationToken = default)
    {
        _ = await _dbContext.JwtBuilderTemplates
                    .Where(t => t.Id == id)
                    .ExecuteUpdateAsync(
                        setter => setter.SetProperty(t => t.LastUsedAt, DateTime.UtcNow),
                        cancellationToken);

        return true;
    }

    /// <inheritdoc/>
    public async Task<bool> TemplateNameExistsAsync(string templateName, int? excludeId = null, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateName))
            return false;

        var query = _dbContext.JwtBuilderTemplates.AsNoTracking().AsQueryable();

        query = query.Where(t => t.TemplateName == templateName);

        if (excludeId.HasValue)
        {
            query = query.Where(t => t.Id != excludeId.Value);
        }

        return await query.AnyAsync(cancellationToken);
    }
}
