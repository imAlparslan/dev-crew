using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

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

    public Task UninstallAsync()
    {
        if (!IsSupported)
            throw new PlatformNotSupportedException("Uninstall is only supported on macOS.");

        // Delete user data directory (no elevation required)
        if (Directory.Exists(UserDataDirectory))
            Directory.Delete(UserDataDirectory, recursive: true);

        ScheduleDetachedUninstall();
        return Task.CompletedTask;
    }

    private static void ScheduleDetachedUninstall()
    {
        var currentProcessId = Environment.ProcessId;
        var scriptPath = Path.Combine(Path.GetTempPath(), $"devcrew-uninstall-{Guid.NewGuid():N}.sh");

        // Forgetting package receipts can fail on some systems after files are already removed.
        // Keep uninstall successful by treating receipt cleanup as best effort.
        var uninstallCommand = $"rm -rf '{AppBundlePath}'; rm -f '{CliBinaryPath}'; pkgutil --forget {PackageId} || true";

        var scriptContent = new StringBuilder()
            .AppendLine("#!/bin/bash")
            .AppendLine("set -euo pipefail")
            .AppendLine($"while kill -0 {currentProcessId} >/dev/null 2>&1; do sleep 0.2; done")
            .AppendLine("osascript <<'APPLESCRIPT'")
            .AppendLine($"do shell script \"{uninstallCommand.Replace("\"", "\\\"")}\" with administrator privileges")
            .AppendLine("APPLESCRIPT")
            .AppendLine("rm -f \"$0\"")
            .ToString();

        File.WriteAllText(scriptPath, scriptContent, Encoding.UTF8);

        var chmod = Process.Start(new ProcessStartInfo("chmod")
        {
            ArgumentList = { "+x", scriptPath },
            UseShellExecute = false,
            CreateNoWindow = true
        }) ?? throw new InvalidOperationException("Failed to mark uninstall helper script as executable.");

        chmod.WaitForExit();
        if (chmod.ExitCode != 0)
            throw new InvalidOperationException("Unable to prepare uninstall helper script.");

        var launcher = Process.Start(new ProcessStartInfo("/bin/bash")
        {
            ArgumentList = { "-c", $"nohup '{scriptPath}' >/tmp/devcrew-uninstall.log 2>&1 &" },
            UseShellExecute = false,
            CreateNoWindow = true
        });

        if (launcher is null)
            throw new InvalidOperationException("Failed to launch uninstall helper process.");
    }
}
