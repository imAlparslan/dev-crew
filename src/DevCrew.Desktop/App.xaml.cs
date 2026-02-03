using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DevCrew.Core;
using DevCrew.Core.Data;
using DevCrew.Desktop.ViewModels;
using DevCrew.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevCrew.Desktop;

public partial class App : Application
{
    private IServiceProvider? _serviceProvider;
    private IConfiguration? _configuration;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        // Load configuration
        _configuration = LoadConfiguration();

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

    private IConfiguration LoadConfiguration()
    {
        var env = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";
        var basePath = AppContext.BaseDirectory;
        var configBuilder = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
            .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: false)
            .AddEnvironmentVariables("DEVCREW_");

        return configBuilder.Build();
    }

    private void ConfigureServices(IServiceCollection services)
    {
        // Register configuration
        if (_configuration != null)
        {
            services.AddSingleton(_configuration);
        }

        // Core Services
        services.AddDevCrewCore(_configuration ?? new ConfigurationBuilder().Build());

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

            // Warm up the database connection to avoid first query delay
            try
            {

                _ = dbContext.GuidHistories.AsNoTracking().Take(1).ToListAsync();

            }
            catch
            {
                // Warm-up query failure should not prevent app startup
            }
        }
    }
}
