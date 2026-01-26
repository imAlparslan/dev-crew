using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

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
            .LogToTrace();
}
