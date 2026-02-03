using DevCrew.Core.Models;
using Microsoft.EntityFrameworkCore;

namespace DevCrew.Core.Data;

/// <summary>
/// Application database context for DevCrew
/// </summary>
public class AppDbContext : DbContext
{
    /// <summary>
    /// Initializes a new instance of the <see cref="AppDbContext"/> class.
    /// </summary>
    /// <param name="options">Database context options.</param>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    /// <summary>
    /// Gets or sets the GUID history records
    /// </summary>
    public DbSet<GuidHistory> GuidHistories { get; set; }

    /// <summary>
    /// Gets or sets the JWT history records
    /// </summary>
    public DbSet<JwtHistory> JwtHistories { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure GuidHistory entity
        modelBuilder.Entity<GuidHistory>(entity =>
        {
            entity.ToTable("GuidHistories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GuidValue).IsRequired().HasMaxLength(EntityConfiguration.GuidValueMaxLength);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(EntityConfiguration.NotesMaxLength);
            entity.HasIndex(e => e.CreatedAt).IsDescending();
        });

        // Configure JwtHistory entity
        modelBuilder.Entity<JwtHistory>(entity =>
        {
            entity.ToTable("JwtHistories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Token).IsRequired().HasMaxLength(EntityConfiguration.JwtTokenMaxLength);
            entity.Property(e => e.DecodedAt).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(EntityConfiguration.NotesMaxLength);
            entity.HasIndex(e => e.DecodedAt).IsDescending();
        });
    }
}
