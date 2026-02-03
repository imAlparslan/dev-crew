namespace DevCrew.Core.Data;

/// <summary>
/// Configuration options for the DevCrew database.
/// </summary>
public class DatabaseOptions
{
    /// <summary>
    /// Configuration section name in appsettings.json
    /// </summary>
    public const string SectionName = "Database";

    /// <summary>
    /// Database file path. If null or empty, uses default LocalApplicationData location.
    /// </summary>
    public string? FilePath { get; set; }

    /// <summary>
    /// Gets the full database connection path.
    /// </summary>
    /// <returns>The resolved database file path</returns>
    public string GetDatabasePath()
    {
        if (!string.IsNullOrWhiteSpace(FilePath))
        {
            return FilePath;
        }

        // Default location: LocalApplicationData/DevCrew/devcrew.db
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevCrew",
            "devcrew.db"
        );

        // Ensure directory exists
        var directory = Path.GetDirectoryName(defaultPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        return defaultPath;
    }
}
