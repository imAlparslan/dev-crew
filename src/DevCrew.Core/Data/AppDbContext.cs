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

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure GuidHistory entity
        modelBuilder.Entity<GuidHistory>(entity =>
        {
            entity.ToTable("GuidHistories");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.GuidValue).IsRequired().HasMaxLength(36);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.Notes).HasMaxLength(500);
            entity.HasIndex(e => e.CreatedAt).IsDescending();
        });
    }
}
