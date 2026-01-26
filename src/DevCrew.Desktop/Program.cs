using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;

namespace DevCrew.Desktop;

internal class Program
{
    // Komut satırı argümanlarını başa al (Avalonia için gerekli)
    [STAThread]
    public static void Main(string[] args) => BuildAvaloniaApp()
        .StartWithClassicDesktopLifetime(args);

    // Avalonia uygulama oluştur ve yapılandır
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
