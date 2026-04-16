using Avalonia;

namespace DevCrew.Desktop;

internal static class Program
{
    // Handle command line arguments (required for Avalonia)
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Create and configure the Avalonia application
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont();
}
