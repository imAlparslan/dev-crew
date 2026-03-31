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

    /// <summary>
    /// Gets or sets the JWT Builder templates
    /// </summary>
    public DbSet<JwtBuilderTemplate> JwtBuilderTemplates { get; set; }

    /// <summary>
    /// Gets or sets application-wide user settings
    /// </summary>
    public DbSet<AppSettings> AppSettings { get; set; }

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

        // Configure JwtBuilderTemplate entity
        modelBuilder.Entity<JwtBuilderTemplate>(entity =>
        {
            entity.ToTable("JwtBuilderTemplates");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.TemplateName).IsRequired().HasMaxLength(EntityConfiguration.TemplateNameMaxLength);
            entity.Property(e => e.Algorithm).IsRequired().HasMaxLength(10);
            entity.Property(e => e.Secret).IsRequired().HasMaxLength(EntityConfiguration.JwtTokenMaxLength);
            entity.Property(e => e.PublicKey).HasMaxLength(EntityConfiguration.JwtTokenMaxLength);
            entity.Property(e => e.Issuer).HasMaxLength(EntityConfiguration.StringFieldMaxLength);
            entity.Property(e => e.Audience).HasMaxLength(EntityConfiguration.StringFieldMaxLength);
            entity.Property(e => e.Subject).HasMaxLength(EntityConfiguration.StringFieldMaxLength);
            entity.Property(e => e.ExpirationMinutes).IsRequired();
            entity.Property(e => e.IncludeExpiration).IsRequired();
            entity.Property(e => e.CustomClaimsJson).HasMaxLength(EntityConfiguration.JwtPayloadMaxLength);
            entity.Property(e => e.Notes).HasMaxLength(EntityConfiguration.NotesMaxLength);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.HasIndex(e => e.CreatedAt).IsDescending();
            entity.HasIndex(e => e.TemplateName);
        });

        // Configure AppSettings entity
        modelBuilder.Entity<AppSettings>(entity =>
        {
            entity.ToTable("AppSettings");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id).ValueGeneratedNever();
            entity.Property(e => e.LanguageCultureName).IsRequired().HasMaxLength(EntityConfiguration.CultureNameMaxLength);
            entity.Property(e => e.FontSizePreference).IsRequired().HasMaxLength(10).HasDefaultValue(Models.AppSettings.DefaultFontSizePreference);
            entity.Property(e => e.UiFontFamily).IsRequired().HasMaxLength(EntityConfiguration.FontFamilyKeyMaxLength).HasDefaultValue(Models.AppSettings.DefaultUiFontFamily);
            entity.Property(e => e.HeadingFontFamily).IsRequired().HasMaxLength(EntityConfiguration.FontFamilyKeyMaxLength).HasDefaultValue(Models.AppSettings.DefaultHeadingFontFamily);
            entity.Property(e => e.ButtonFontFamily).IsRequired().HasMaxLength(EntityConfiguration.FontFamilyKeyMaxLength).HasDefaultValue(Models.AppSettings.DefaultButtonFontFamily);
            entity.Property(e => e.ContentFontFamily).IsRequired().HasMaxLength(EntityConfiguration.FontFamilyKeyMaxLength).HasDefaultValue(Models.AppSettings.DefaultContentFontFamily);
            entity.Property(e => e.CreatedAt).IsRequired();
            entity.Property(e => e.UpdatedAt).IsRequired();
        });
    }
}
