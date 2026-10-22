namespace DevCrew.Desktop.Services;

/// <summary>
/// Copies the bundled MCP executable to a stable, user-accessible location.
/// Packaged (MSIX) installs live under a protected folder, so other processes cannot launch executables from there.
/// </summary>
public static class McpExecutableInstaller
{
    public static string GetInstallDirectory()
    {
        var root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        return Path.Combine(root, "DevCrew", "mcp");
    }

    public static bool RequiresStaging(string executablePath)
        => OperatingSystem.IsWindows()
           && executablePath.Contains($"{Path.DirectorySeparatorChar}WindowsApps{Path.DirectorySeparatorChar}", StringComparison.OrdinalIgnoreCase);

    /// <summary>Returns the path clients should use; falls back to the original path if staging fails.</summary>
    public static string EnsureAccessible(string executablePath)
        => EnsureAccessible(executablePath, GetInstallDirectory());

    public static string EnsureAccessible(string executablePath, string installDirectory)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(executablePath);
        ArgumentException.ThrowIfNullOrWhiteSpace(installDirectory);

        if (!RequiresStaging(executablePath))
        {
            return executablePath;
        }

        return TryStage(executablePath, installDirectory) ?? executablePath;
    }

    internal static string? TryStage(string sourcePath, string installDirectory)
    {
        var destinationPath = Path.Combine(installDirectory, Path.GetFileName(sourcePath));

        try
        {
            Directory.CreateDirectory(installDirectory);

            if (IsUpToDate(sourcePath, destinationPath))
            {
                return destinationPath;
            }

            var temporaryPath = destinationPath + ".tmp";
            File.Copy(sourcePath, temporaryPath, overwrite: true);
            File.Move(temporaryPath, destinationPath, overwrite: true);
            return destinationPath;
        }
        catch (IOException) when (File.Exists(destinationPath))
        {
            // The existing copy is in use by a running client; keep using it.
            return destinationPath;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return null;
        }
    }

    private static bool IsUpToDate(string sourcePath, string destinationPath)
    {
        if (!File.Exists(destinationPath))
        {
            return false;
        }

        var source = new FileInfo(sourcePath);
        var destination = new FileInfo(destinationPath);
        return source.Length == destination.Length
               && source.LastWriteTimeUtc <= destination.LastWriteTimeUtc;
    }
}
