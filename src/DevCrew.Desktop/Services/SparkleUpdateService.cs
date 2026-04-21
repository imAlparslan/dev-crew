using NetSparkleUpdater;
using NetSparkleUpdater.Enums;
using NetSparkleUpdater.UI.Avalonia;
using NetSparkleUpdater.SignatureVerifiers;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Net.Http;
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

    // DIAGNOSTIC: remove after debugging
    private readonly string _diagnosticAppCastUrl;
    private readonly string _diagnosticChannel;

    public SparkleUpdateService(IConfiguration configuration)
    {
        // DIAGNOSTIC: remove after debugging
        DiagLog($"BaseDirectory: {AppContext.BaseDirectory}");
        DiagLog($"DOTNET_ENVIRONMENT: {Environment.GetEnvironmentVariable("DOTNET_ENVIRONMENT") ?? "(null)"}");
        DiagLog($"ASPNETCORE_ENVIRONMENT: {Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "(null)"}");
        DiagLog($"Configured Sparkle:Channel: {configuration["Sparkle:Channel"] ?? "(null)"}");

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

        // DIAGNOSTIC: remove after debugging
        _diagnosticAppCastUrl = appCastUrl;
        _diagnosticChannel = channel;
        DiagLog($"Channel: {channel}");
        DiagLog($"AppCast URL: {appCastUrl}");
        DiagLog($"Assembly version: {ResolveCurrentVersion()}");
        DiagLog($"Info.plist version: {TryResolveMacBundleVersion() ?? "(not found)"}");

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
        DiagLog($"Installer temp target: {_updater.TmpDownloadFilePath}/{_updater.TmpDownloadFileNameWithExtension}");

    }
    public async Task<UpdateCheckResult> CheckForUpdatesAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // DIAGNOSTIC: remove after debugging
        DiagLog($"CheckForUpdates — channel={_diagnosticChannel}, url={_diagnosticAppCastUrl}");
        await ProbeAppCastAsync(_diagnosticAppCastUrl, cancellationToken);

        UpdateInfo? res = null;
        try
        {
            // Use quiet mode to avoid NetSparkle's built-in generic error popup.
            // We keep user messaging in our own UI via UpdateCheckResult.
            res = await _updater.CheckForUpdatesQuietly();
            DiagLog($"CheckForUpdates result — Status={res?.Status}, UpdateCount={res?.Updates?.Count ?? 0}");
        }
        catch (Exception ex)
        {
            DiagLog($"CheckForUpdates EXCEPTION — {ex.GetType().Name}: {ex.Message}");
            throw;
        }

        cancellationToken.ThrowIfCancellationRequested();

        var updates = res?.Updates?.ToList() ?? new List<AppCastItem>();
        _cachedUpdates = updates;

        var latestItem = updates.FirstOrDefault();
        _latestUpdateItem = latestItem;

        // DIAGNOSTIC: remove after debugging
        if (latestItem is not null)
        {
            DiagLog($"Latest item — Version={latestItem.Version}, ShortVersionString={ResolveAppCastItemVersion(latestItem)}");
        }
        else
        {
            DiagLog("No update items returned from feed.");
        }

        var currentVersion = ResolveCurrentVersion();
        var latestVersion = ResolveAppCastItemVersion(latestItem) ?? currentVersion;
        var isResolvedVersionNewer = !string.Equals(latestVersion, currentVersion, StringComparison.OrdinalIgnoreCase);

        if (!isResolvedVersionNewer && res?.Status == UpdateStatus.UpdateAvailable)
        {
            // DIAGNOSTIC: remove after debugging
            DiagLog($"Suppressing false update result because currentVersion matches latestVersion ({currentVersion}).");
        }

        return new UpdateCheckResult(
            IsUpdateAvailable: res?.Status == UpdateStatus.UpdateAvailable && latestItem is not null && isResolvedVersionNewer,
            CurrentVersion: currentVersion,
            LatestVersion: latestVersion);
    }

    public Task StartUpdateAsync(string latestVersion, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(latestVersion);

        // DIAGNOSTIC: remove after debugging
        DiagLog($"StartUpdate requested — latestVersion={latestVersion}");

        var selectedItem = _cachedUpdates.FirstOrDefault(item =>
            string.Equals(ResolveAppCastItemVersion(item), latestVersion, StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.Version?.ToString(), latestVersion, StringComparison.OrdinalIgnoreCase));

        if (selectedItem is not null)
        {
            // DIAGNOSTIC: remove after debugging
            DiagLog($"StartUpdate selected item — Version={selectedItem.Version}, ResolvedVersion={ResolveAppCastItemVersion(selectedItem)}, DownloadLink={selectedItem.DownloadLink}");
        }

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

    // DIAGNOSTIC: remove after debugging
    private static async Task ProbeAppCastAsync(string appCastUrl, CancellationToken cancellationToken)
    {
        try
        {
            using var client = new HttpClient();
            using var response = await client.GetAsync(appCastUrl, cancellationToken);
            DiagLog($"Probe appcast HTTP — Status={(int)response.StatusCode} {response.StatusCode}");

            if (!response.IsSuccessStatusCode)
            {
                return;
            }

            var xml = await response.Content.ReadAsStringAsync(cancellationToken);
            var document = XDocument.Parse(xml);
            var sparkleNs = XNamespace.Get("http://www.andymatuschak.org/xml-namespaces/sparkle");
            var channelElement = document.Root?.Element("channel");
            var title = channelElement?.Element("title")?.Value?.Trim();
            var firstItem = channelElement?.Elements("item").FirstOrDefault();
            var enclosure = firstItem?.Element("enclosure");
            var shortVersion = enclosure?.Attribute(sparkleNs + "shortVersionString")?.Value;
            var buildVersion = enclosure?.Attribute(sparkleNs + "version")?.Value;
            var packageUrl = enclosure?.Attribute("url")?.Value;
            var itemCount = channelElement?.Elements("item").Count() ?? 0;

            DiagLog(
                $"Probe appcast parsed — Title={title ?? "(null)"}, ItemCount={itemCount}, FirstShortVersion={shortVersion ?? "(null)"}, FirstVersion={buildVersion ?? "(null)"}, FirstUrl={packageUrl ?? "(null)"}");
        }
        catch (Exception ex)
        {
            DiagLog($"Probe appcast EXCEPTION — {ex.GetType().Name}: {ex.Message}");
        }
    }

    // DIAGNOSTIC: remove after debugging
    private static readonly string DiagLogPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
        "Library", "Logs", "DevCrew", "sparkle-diagnostic.log");

    private static void DiagLog(string message)
    {
        try
        {
            var logDir = Path.GetDirectoryName(DiagLogPath)!;
            Directory.CreateDirectory(logDir);
            File.AppendAllText(DiagLogPath, $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [SparkleUpdateService] {message}{Environment.NewLine}");
        }
        catch
        {
            // ignore logging errors
        }
    }
}
