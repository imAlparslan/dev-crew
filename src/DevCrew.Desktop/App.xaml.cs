using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DevCrew.Core;
using DevCrew.Core.Data;
using DevCrew.Core.Services;
using DevCrew.Desktop.ViewModels;
using DevCrew.Desktop.Views;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DevCrew.Desktop.Services;

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

        // Configure Dependency Injection Container
        var services = new ServiceCollection();
        ConfigureServices(services);
        _serviceProvider = services.BuildServiceProvider();

        var localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();
        Resources["Loc"] = localizationService;

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

        // Localization
        var startupCulture = LocalizationService.ResolveOrFallbackCulture(System.Globalization.CultureInfo.CurrentUICulture.Name);
        services.AddSingleton<ILocalizationService>(_ => new LocalizationService(startupCulture));

        // Core Services
        services.AddDevCrewCore(_configuration ?? new ConfigurationBuilder().Build());
        
        // Desktop-specific services
        services.AddScoped<IClipboardService, Services.ClipboardService>();

        // ViewModels
        services.AddScoped<MainWindowViewModel>();
        services.AddSingleton<DashboardViewModel>();
        services.AddTransient<CreateGuidViewModel>();
        services.AddTransient<Func<CreateGuidViewModel>>(sp => () => sp.GetRequiredService<CreateGuidViewModel>());
        services.AddTransient<JwtDecoderViewModel>();
        services.AddTransient<Func<JwtDecoderViewModel>>(sp => () => sp.GetRequiredService<JwtDecoderViewModel>());
        services.AddTransient<JwtBuilderViewModel>();
        services.AddTransient<Func<JwtBuilderViewModel>>(sp => () => sp.GetRequiredService<JwtBuilderViewModel>());
        services.AddTransient<JsonFormatterViewModel>();
        services.AddTransient<Func<JsonFormatterViewModel>>(sp => () => sp.GetRequiredService<JsonFormatterViewModel>());
        services.AddTransient<JsonDiffViewModel>();
        services.AddTransient<Func<JsonDiffViewModel>>(sp => () => sp.GetRequiredService<JsonDiffViewModel>());
        services.AddTransient<Base64EncoderViewModel>();
        services.AddTransient<Func<Base64EncoderViewModel>>(sp => () => sp.GetRequiredService<Base64EncoderViewModel>());
        services.AddTransient<Base64DecoderViewModel>();
        services.AddTransient<Func<Base64DecoderViewModel>>(sp => () => sp.GetRequiredService<Base64DecoderViewModel>());
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<Func<SettingsViewModel>>(sp => () => sp.GetRequiredService<SettingsViewModel>());
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
            // This asynchronous operation is intentionally fire-and-forget
            // as database readiness is not critical for app startup
            Task.Run(async () =>
            {
                try
                {
                    using var warmupScope = _serviceProvider?.CreateScope();
                    var warmupDbContext = warmupScope?.ServiceProvider.GetRequiredService<AppDbContext>();

                    if (warmupDbContext != null)
                    {
                        await warmupDbContext.GuidHistories.AsNoTracking().Take(1).ToListAsync();
                    }
                }
                catch
                {
                    // Warm-up query failure should not prevent app startup
                }
            });
        }
    }
}