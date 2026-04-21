using System.Reflection;
namespace DevCrew.Desktop.Services;

public sealed class MockUpdateService : IUpdateService
{
    private const string MockLatestVersion = "1.1.0";
    private string _lastStartedVersion = string.Empty;

    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        // Simulate network latency and always return an available update for UI testing.
        await Task.Delay(900, cancellationToken);

        var latestVersion = string.IsNullOrWhiteSpace(_lastStartedVersion)
            ? MockLatestVersion
            : _lastStartedVersion;

        return new UpdateCheckResult(
            IsUpdateAvailable: true,
            CurrentVersion: ResolveCurrentVersion(),
            LatestVersion: latestVersion);
    }

    public async Task StartUpdateAsync(string latestVersion, CancellationToken cancellationToken = default)
    {
        _lastStartedVersion = latestVersion;

        // Placeholder for installer integration.
        await Task.Delay(500, cancellationToken);
    }

    private static string ResolveCurrentVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version
            ?? typeof(MockUpdateService).Assembly.GetName().Version;

        if (version is null)
        {
            return "0.0.0";
        }

        var build = version.Build < 0 ? 0 : version.Build;
        return $"{version.Major}.{version.Minor}.{build}";
    }
}
