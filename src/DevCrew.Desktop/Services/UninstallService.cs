using System.Diagnostics;
using System.Runtime.InteropServices;

namespace DevCrew.Desktop.Services;

public sealed class UninstallService : IUninstallService
{
    private const string PackageId = "com.devcrew.macos";
    private const string AppBundlePath = "/Applications/DevCrew.app";
    private const string CliBinaryPath = "/usr/local/bin/crew";

    private static readonly string UserDataDirectory = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "DevCrew");

    public bool IsSupported => RuntimeInformation.IsOSPlatform(OSPlatform.OSX);

    public async Task UninstallAsync()
    {
        if (!IsSupported)
            throw new PlatformNotSupportedException("Uninstall is only supported on macOS.");

        // Delete user data directory (no elevation required)
        if (Directory.Exists(UserDataDirectory))
            Directory.Delete(UserDataDirectory, recursive: true);

        // Remove app bundle, CLI binary, and PKG receipt in a single elevated shell script
        var script = $"do shell script " +
                     $"\"rm -rf '{AppBundlePath}' && " +
                     $"rm -f '{CliBinaryPath}' && " +
                     $"pkgutil --forget {PackageId}\" " +
                     $"with administrator privileges";

        var psi = new ProcessStartInfo("osascript")
        {
            ArgumentList = { "-e", script },
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true
        };

        using var proc = Process.Start(psi)
            ?? throw new InvalidOperationException("Failed to start osascript process.");

        await proc.WaitForExitAsync();

        if (proc.ExitCode != 0)
        {
            var err = await proc.StandardError.ReadToEndAsync();
            throw new InvalidOperationException(
                $"Uninstall failed (exit {proc.ExitCode}). " +
                (string.IsNullOrWhiteSpace(err) ? "The operation was cancelled or denied." : err.Trim()));
        }

        Environment.Exit(0);
    }
}
