using System;
using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.UI.Avalonia;
using NetSparkleUpdater.SignatureVerifiers;

namespace DevCrew.Desktop.Services;

public class SparkleUpdateService : IUpdateService
{
    private SparkleUpdater _updater;

    public SparkleUpdateService()
    {
        string channel = "stable"; // This could be dynamically set based on user preferences or configuration.
        string fileName = channel switch
        {
            "alpha" => "appcast-alpha.xml",
            "beta" => "appcast-beta.xml",
            _ => "appcast.xml"
        };
        string appCastUrl = $"https://github.com/imAlparslan/dev-crew/releases/latest/download/{fileName}";

        _updater = new SparkleUpdater(appCastUrl, new Ed25519Checker(SecurityMode.Unsafe))
        {
            UIFactory = new UIFactory(),
            RelaunchAfterUpdate = true,
        };

    }
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        var res = await _updater.CheckForUpdatesAtUserRequest();

        var ltsVersion = res.Updates.FirstOrDefault();

        return new UpdateCheckResult(
            IsUpdateAvailable: res.Status == UpdateStatus.UpdateAvailable,
            CurrentVersion: "0.0.0",
            LatestVersion: ltsVersion.Version.ToString() ?? "0.0.0");
    }

    public Task StartUpdateAsync(string latestVersion, CancellationToken cancellationToken = default)
    {
        throw new NotImplementedException();
    }
}
