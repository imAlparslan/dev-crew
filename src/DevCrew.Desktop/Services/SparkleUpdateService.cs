using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.UI.Avalonia;
using NetSparkleUpdater.SignatureVerifiers;
using Microsoft.Extensions.Configuration;
using System.Reflection;
using System.Xml.Linq;

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

        var channel = ResolveChannel(configuration["Sparkle:Channel"]);
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

        // Keep downloaded installers in a stable user-writable path on macOS to avoid temp-path issues.
        _updater.TmpDownloadFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "DevCrewUpdates");
        _updater.TmpDownloadFileNameWithExtension = "DevCrew-update.pkg";
        _updater.CheckServerFileName = true;

    }
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();


        UpdateInfo? res = null;
        try
        {
            // Use quiet mode to avoid NetSparkle's built-in generic error popup.
            // We keep user messaging in our own UI via UpdateCheckResult.
            res = await _updater.CheckForUpdatesQuietly();
        }
        catch (Exception ex)
        {
            throw;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var updates = res?.Updates?.ToList() ?? new List<AppCastItem>();
        _cachedUpdates = updates;

        var latestItem = updates.FirstOrDefault();
        _latestUpdateItem = latestItem;


        var currentVersion = ResolveCurrentVersion();
        var latestVersion = ResolveAppCastItemVersion(latestItem) ?? currentVersion;
        var isResolvedVersionNewer = !string.Equals(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase);

        return new UpdateCheckResult(
            IsUpdateAvailable: res?.Status == UpdateStatus.UpdateAvailable && latestItem is not null && isResolvedVersionNewer,
            CurrentVersion: currentVersion,
            LatestVersion: latestVersion);
    }

    public Task StartUpdateAsync(string latestVersion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(latestVersion);

        var selectedItem = _cachedUpdates.FirstOrDefault(item =>
            string.Equals(ResolveAppCastItemVersion(item), latestVersion, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.Version?.ToString(), latestVersion, StringComparison.OrdinalIgnoreCase));

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
        var entryAssembly = Assembly.GetEntryAssembly() ?? typeof(SparkleUpdateService).Assembly;
        var informationalVersion = entryAssembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
            ?.InformationalVersion;

        if (!string.IsNullOrWhiteSpace(informationalVersion))
        {
            var normalized = informationalVersion.Split('+')[0].Trim();
            if (!string.IsNullOrWhiteSpace(normalized))
            {
                return normalized;
            }
        }

        var bundleVersion = TryResolveMacBundleVersion();
        if (!string.IsNullOrWhiteSpace(bundleVersion))
        {
            return bundleVersion;
        }

        var version = entryAssembly.GetName().Version;

        if (version is null)
        {
            return "0.0.0";
        }

        var build = version.Build < 0 ? 0 : version.Build;
        return $"{version.Major}.{version.Minor}.{build}";
    }

    private static string? ResolveAppCastItemVersion(AppCastItem? item)
    {
        if (item is null)
        {
            return null;
        }

        // NetSparkle appcast parser may map sparkle:short_version directly.
        var shortVersion = item.GetType()
            .GetProperty("ShortVersion", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(item)
            ?.ToString();

        if (!string.IsNullOrWhiteSpace(shortVersion))
        {
            return shortVersion;
        }

        var shortVersionString = item.GetType()
            .GetProperty("ShortVersionString", BindingFlags.Public | BindingFlags.Instance)
            ?.GetValue(item)
            ?.ToString();

        if (!string.IsNullOrWhiteSpace(shortVersionString))
        {
            return shortVersionString;
        }

        return item.Version?.ToString();
    }

    private static string? TryResolveMacBundleVersion()
    {
        try
        {
            var baseDir = AppContext.BaseDirectory;
            var macOsDir = new DirectoryInfo(baseDir);
            var contentsDir = macOsDir.Parent;

            if (contentsDir is null || !string.Equals(contentsDir.Name, "Contents", StringComparison.OrdinalIgnoreCase))
            {
                return null;
            }

            var infoPlistPath = Path.Combine(contentsDir.FullName, "Info.plist");
            if (!File.Exists(infoPlistPath))
            {
                return null;
            }

            var document = XDocument.Load(infoPlistPath);
            var dictElement = document.Root?.Element("dict");
            if (dictElement is null)
            {
                return null;
            }

            var children = dictElement.Elements().ToList();
            for (var i = 0; i < children.Count - 1; i++)
            {
                var keyElement = children[i];
                if (!string.Equals(keyElement.Name.LocalName, "key", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!string.Equals(keyElement.Value, "CFBundleShortVersionString", StringComparison.Ordinal))
                {
                    continue;
                }

                var valueElement = children[i + 1];
                if (!string.Equals(valueElement.Name.LocalName, "string", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }

                var value = valueElement.Value?.Trim();
                return string.IsNullOrWhiteSpace(value) ? null : value;
            }
        }
        catch
        {
            // Ignore plist parsing errors and continue with assembly fallback.
        }

        return null;
    }

    private static string ResolveChannel(string? configuredChannel)
    {
        var normalizedConfiguredChannel = NormalizeChannel(configuredChannel);
        var inferredFromVersion = InferChannelFromVersion(ResolveCurrentVersion());

        // Keep explicit config values; only infer for prerelease builds when config is stable/default.
        return normalizedConfiguredChannel == DefaultChannel && inferredFromVersion != DefaultChannel
            ? inferredFromVersion
            : normalizedConfiguredChannel;
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

    private static string InferChannelFromVersion(string version)
    {
        if (version.Contains("-alpha", StringComparison.OrdinalIgnoreCase))
        {
            return "alpha";
        }

        if (version.Contains("-beta", StringComparison.OrdinalIgnoreCase))
        {
            return "beta";
        }

        return DefaultChannel;
    }

}
