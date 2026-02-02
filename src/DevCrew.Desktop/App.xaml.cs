using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DevCrew.Core;
using DevCrew.Core.Data;
using DevCrew.Desktop.ViewModels;
using DevCrew.Desktop.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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

        // Initialize database
        InitializeDatabase();

        var mainWindowViewModel = _serviceProvider?.GetRequiredService<MainWindowViewModel>();

        var mainWindow = new MainWindow
        {
            DataContext = mainWindowViewModel
        };

        if (mainWindowViewModel != null)
        {
            _ = mainWindowViewModel.InitializeAsync();
        }

        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = mainWindow;
        }

        base.OnFrameworkInitializationCompleted();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Logging
#if DEBUG
        services.AddLogging(configure =>
        {
            configure.AddConsole();
            configure.AddDebug();
            configure.SetMinimumLevel(LogLevel.Debug);
        });
#else
        services.AddLogging(configure =>
        {
            configure.SetMinimumLevel(LogLevel.Warning);
        });
#endif

        // Core Services
        services.AddDevCrewCore();

        // ViewModels
        services.AddScoped<MainWindowViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddTransient<CreateGuidViewModel>();
        services.AddTransient<Func<CreateGuidViewModel>>(sp => () => sp.GetRequiredService<CreateGuidViewModel>());
        services.AddTransient<JwtDecoderViewModel>();
        services.AddTransient<Func<JwtDecoderViewModel>>(sp => () => sp.GetRequiredService<JwtDecoderViewModel>());
    }

    private void InitializeDatabase()
    {
        using var scope = _serviceProvider?.CreateScope();
        var dbContext = scope?.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext != null)
        {
            // Create database if it doesn't exist
            dbContext.Database.EnsureCreated();
        }
    }
}
