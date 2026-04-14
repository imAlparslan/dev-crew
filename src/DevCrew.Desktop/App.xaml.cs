using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using DevCrew.Core.Infrastructure.Persistence;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using DevCrew.Desktop.DependencyInjection;
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

        // Initialize database before resolving services that depend on persisted settings.
        InitializeDatabase();

        // Apply persisted runtime settings with a single settings query.
        ApplyPersistedRuntimeSettings();

        var localizationService = _serviceProvider.GetRequiredService<ILocalizationService>();
        Resources["Loc"] = localizationService;

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

    private static IConfiguration LoadConfiguration()
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
        services.AddDesktopServices(_configuration);
    }

    private void ApplyPersistedRuntimeSettings()
    {
        try
        {
            using var scope = _serviceProvider?.CreateScope();
            if (scope == null) return;

            var repo = scope.ServiceProvider.GetRequiredService<IAppSettingsRepository>();
            var settings = repo.GetOrCreateAsync().GetAwaiter().GetResult();
            var localization = _serviceProvider!.GetRequiredService<ILocalizationService>();
            var fontService = _serviceProvider!.GetRequiredService<IFontService>();

            localization.SetLanguage(settings.LanguageCultureName);
            fontService.ApplyFontSettings(
                settings.FontSizePreference,
                settings.UiFontFamily,
                settings.HeadingFontFamily,
                settings.ButtonFontFamily,
                settings.ContentFontFamily);
        }
        catch
        {
            // Apply defaults when DB access fails at startup.
            _serviceProvider?.GetRequiredService<IFontService>()
                .ApplyFontSettings("Medium", "Inter", "Inter", "Inter", "Consolas");
        }
    }

    private void InitializeDatabase()
    {
        using var scope = _serviceProvider?.CreateScope();
        var dbContext = scope?.ServiceProvider.GetRequiredService<AppDbContext>();

        if (dbContext != null)
        {
            DatabaseSchemaInitializer.EnsureCompatibilitySchema(dbContext);

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