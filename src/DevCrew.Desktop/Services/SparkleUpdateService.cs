using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.UI.Avalonia;
using NetSparkleUpdater.SignatureVerifiers;
using Microsoft.Extensions.Configuration;
using System.Reflection;

namespace DevCrew.Desktop.Services;

public class SparkleUpdateService : IUpdateService
{
    private readonly SparkleUpdater _updater;
    private IReadOnlyList<AppCastItem> _cachedUpdates = Array.Empty<AppCastItem>();
    private AppCastItem? _latestUpdateItem;
    private const string DefaultChannel = "stable";
    private const string DefaultAppCastBaseUrl = "https://raw.githubusercontent.com/imAlparslan/dev-crew/main/Appcasts";

    public SparkleUpdateService(IConfiguration configuration)
    {
        var channel = NormalizeChannel(configuration["Sparkle:Channel"]);
        var fileName = channel switch
        {
            "alpha" => "appcast-alpha.xml",
            "beta" => "appcast-beta.xml",
            _ => "appcast.xml"
        };
        var configuredBaseUrl = configuration["Sparkle:AppCastBaseUrl"];
        var appCastBaseUrl = string.IsNullOrWhiteSpace(configuredBaseUrl)
            ? DefaultAppCastBaseUrl
            : configuredBaseUrl.TrimEnd('/');
        var appCastUrl = $"{appCastBaseUrl}/{fileName}";

        _updater = new SparkleUpdater(appCastUrl, new Ed25519Checker(SecurityMode.Unsafe))
        {
            UIFactory = new UIFactory(),
            RelaunchAfterUpdate = true,
        };

    }
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        var res = await _updater.CheckForUpdatesAtUserRequest();
        cancellationToken.ThrowIfCancellationRequested();

        var updates = res?.Updates?.ToList() ?? new List<AppCastItem>();
        _cachedUpdates = updates;

        var latestItem = updates.FirstOrDefault();
        _latestUpdateItem = latestItem;

        var latestVersion = latestItem?.Version?.ToString() ?? ResolveCurrentVersion();

        return new UpdateCheckResult(
            IsUpdateAvailable: res?.Status == UpdateStatus.UpdateAvailable && latestItem is not null,
            CurrentVersion: ResolveCurrentVersion(),
            LatestVersion: latestVersion);
    }

    public Task StartUpdateAsync(string latestVersion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(latestVersion);

        var selectedItem = _cachedUpdates.FirstOrDefault(item =>
            string.Equals(item.Version?.ToString(), latestVersion, StringComparison.OrdinalIgnoreCase));

        _latestUpdateItem = selectedItem ?? _latestUpdateItem;

        if (_latestUpdateItem is null)
        {
            throw new InvalidOperationException("No downloaded update metadata is available. Check for updates first.");
        }

        cancellationToken.ThrowIfCancellationRequested();

        // NetSparkle handles download and install flow from the selected appcast item.
        _updater.InitAndBeginDownload(_latestUpdateItem);

        return Task.CompletedTask;
    }

    private static string ResolveCurrentVersion()
    {
        var version = Assembly.GetEntryAssembly()?.GetName().Version
            ?? typeof(SparkleUpdateService).Assembly.GetName().Version;

        if (version is null)
        {
            return "0.0.0";
        }

        var build = version.Build < 0 ? 0 : version.Build;
        return $"{version.Major}.{version.Minor}.{build}";
    }

    private static string NormalizeChannel(string? channel)
    {
        if (string.IsNullOrWhiteSpace(channel))
        {
            return DefaultChannel;
        }

        var normalized = channel.Trim().ToLowerInvariant();
        return normalized is "alpha" or "beta" ? normalized : DefaultChannel;
    }
}
