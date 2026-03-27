using Microsoft.EntityFrameworkCore;

namespace DevCrew.Core.Data;

public static class DatabaseSchemaInitializer
{
    private const string EnsureAppSettingsTableSql = @"
CREATE TABLE IF NOT EXISTS AppSettings (
    Id INTEGER NOT NULL CONSTRAINT PK_AppSettings PRIMARY KEY,
    LanguageCultureName TEXT NOT NULL,
    CreatedAt TEXT NOT NULL,
    UpdatedAt TEXT NOT NULL
);";

    private const string AddFontSizePreferenceColumnSql =
        "ALTER TABLE AppSettings ADD COLUMN FontSizePreference TEXT NOT NULL DEFAULT 'Medium';";

    private const string AddUiFontFamilyColumnSql =
        "ALTER TABLE AppSettings ADD COLUMN UiFontFamily TEXT NOT NULL DEFAULT 'Inter';";

    private const string AddContentFontFamilyColumnSql =
        "ALTER TABLE AppSettings ADD COLUMN ContentFontFamily TEXT NOT NULL DEFAULT 'Consolas';";

    public static void EnsureCompatibilitySchema(AppDbContext dbContext)
    {
        if (dbContext == null)
        {
            throw new ArgumentNullException(nameof(dbContext));
        }

        dbContext.Database.EnsureCreated();
        dbContext.Database.ExecuteSqlRaw(EnsureAppSettingsTableSql);

        TryAddColumn(dbContext, AddFontSizePreferenceColumnSql);
        TryAddColumn(dbContext, AddUiFontFamilyColumnSql);
        TryAddColumn(dbContext, AddContentFontFamilyColumnSql);
    }

    private static void TryAddColumn(AppDbContext dbContext, string sql)
    {
        try
        {
            dbContext.Database.ExecuteSqlRaw(sql);
        }
        catch
        {
            // Column already exists — safe to ignore.
        }
    }
}
