using System.Runtime.InteropServices;

namespace DevCrew.Desktop.Services;

public static class McpExecutableLocator
{
    public static string? Locate()
        => Locate(AppContext.BaseDirectory, RuntimeInformation.RuntimeIdentifier);

    public static string? Locate(string applicationBaseDirectory, string runtimeIdentifier)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(applicationBaseDirectory);
        ArgumentException.ThrowIfNullOrWhiteSpace(runtimeIdentifier);

        var candidates = GetInstalledCandidates(applicationBaseDirectory)
            .Concat(GetDevelopmentCandidates(applicationBaseDirectory, runtimeIdentifier))
            .Distinct(StringComparer.OrdinalIgnoreCase);

        return candidates.FirstOrDefault(File.Exists);
    }

    private static IEnumerable<string> GetInstalledCandidates(string applicationBaseDirectory)
    {
        var executableName = OperatingSystem.IsWindows() ? "DevCrew.Mcp.exe" : "DevCrew.Mcp";
        yield return Path.Combine(applicationBaseDirectory, executableName);

        if (OperatingSystem.IsMacOS())
        {
            yield return "/usr/local/bin/devcrew-mcp";
        }
    }

    private static IEnumerable<string> GetDevelopmentCandidates(string applicationBaseDirectory, string runtimeIdentifier)
    {
        var executableName = OperatingSystem.IsWindows() ? "DevCrew.Mcp.exe" : "DevCrew.Mcp";
        var directory = new DirectoryInfo(applicationBaseDirectory);

        for (var depth = 0; directory is not null && depth < 8; depth++, directory = directory.Parent)
        {
            foreach (var configuration in new[] { "Debug", "Release" })
            {
                var projectOutput = Path.Combine(
                    directory.FullName,
                    "src",
                    "DevCrew.Mcp",
                    "bin",
                    configuration,
                    "net10.0");

                yield return Path.Combine(projectOutput, executableName);
                yield return Path.Combine(projectOutput, "publish", executableName);

                yield return Path.Combine(projectOutput, runtimeIdentifier, executableName);
                yield return Path.Combine(projectOutput, runtimeIdentifier, "publish", executableName);
            }
        }
    }
}
