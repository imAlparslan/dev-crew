using Avalonia;
using System.Diagnostics;

namespace DevCrew.Desktop;

internal class Program
{
    // Handle command line arguments (required for Avalonia)
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Create and configure the Avalonia application
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
#if DEBUG
            .LogToTrace(level: Avalonia.Logging.LogEventLevel.Debug)
#else
            .LogToTrace(level: Avalonia.Logging.LogEventLevel.Warning)
#endif
            ;
}
