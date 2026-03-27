using Microsoft.EntityFrameworkCore;

namespace DevCrew.Core.Data;

/// <summary>
/// Ensures database schema compatibility across versions.
/// </summary>
public static class DatabaseSchemaInitializer
{
    private const string EnsureAppSettingsTableSql = @"
CREATE TABLE IF NOT EXISTS AppSettings (
    Id INTEGER NOT NULL CONSTRAINT PK_AppSettings PRIMARY KEY,
    LanguageCultureName TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);";

    /// <summary>
    /// Ensures the database exists and applies compatibility schema updates.
    /// </summary>
    public static void EnsureCompatibilitySchema(AppDbContext dbContext)
    {
        if (dbContext == null)
        {
            throw new ArgumentNullException(nameof(dbContext));
        }

        dbContext.Database.EnsureCreated();
        dbContext.Database.ExecuteSqlRaw(EnsureAppSettingsTableSql);
    }
}
