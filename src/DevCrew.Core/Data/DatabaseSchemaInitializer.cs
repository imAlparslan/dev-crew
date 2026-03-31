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

    private const string AddHeadingFontFamilyColumnSql = @"
ALTER TABLE AppSettings
ADD COLUMN HeadingFontFamily TEXT NOT NULL DEFAULT 'Inter';";

    private const string AddButtonFontFamilyColumnSql = @"
ALTER TABLE AppSettings
ADD COLUMN ButtonFontFamily TEXT NOT NULL DEFAULT 'Inter';";

    public static void EnsureCompatibilitySchema(AppDbContext dbContext)
    {
        if (dbContext == null)
        {
            throw new ArgumentNullException(nameof(dbContext));
        }

        dbContext.Database.EnsureCreated();
        dbContext.Database.ExecuteSqlRaw(EnsureAppSettingsTableSql);

        if (!ColumnExists(dbContext, "AppSettings", "HeadingFontFamily"))
        {
            dbContext.Database.ExecuteSqlRaw(AddHeadingFontFamilyColumnSql);
        }

        if (!ColumnExists(dbContext, "AppSettings", "ButtonFontFamily"))
        {
            dbContext.Database.ExecuteSqlRaw(AddButtonFontFamilyColumnSql);
        }
    }

    private static bool ColumnExists(AppDbContext dbContext, string tableName, string columnName)
    {
        var connection = dbContext.Database.GetDbConnection();
        var shouldClose = connection.State != System.Data.ConnectionState.Open;

        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText =
                $"SELECT 1 FROM pragma_table_info('{tableName}') WHERE name = '{columnName}' LIMIT 1;";

            var result = command.ExecuteScalar();
            return result != null && result != DBNull.Value;
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }
}
