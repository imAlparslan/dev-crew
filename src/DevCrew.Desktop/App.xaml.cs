using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using DevCrew.Desktop.ViewModels;
using DevCrew.Desktop.Views;
using DevCrew.Core.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DevCrew.Desktop;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Dependency Injection Container'ını yapılandır
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var mainWindow = new MainWindow
        {
            DataContext = _serviceProvider?.GetRequiredService<MainWindowViewModel>()
        };

        if (ApplicationLifetime != null)
        {
            dynamic lifetime = ApplicationLifetime;
            lifetime.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Core Services
        services.AddScoped<IApplicationService, ApplicationService>();

        // ViewModels
        services.AddScoped<MainWindowViewModel>();
    }
}
