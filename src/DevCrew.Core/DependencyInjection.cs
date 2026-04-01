using DevCrew.Core.Infrastructure.Persistence;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace DevCrew.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddDevCrewCore(this IServiceCollection services, IConfiguration configuration)
    {
        // Add logging
        services.AddLogging();

        // Create database options - use FilePath from config if available
        var databaseOptions = new DatabaseOptions
        {
            FilePath = configuration["Database:FilePath"]
        };
        var dbPath = databaseOptions.GetDatabasePath();

        // SQLite connection string - simple configuration, pragmas set at runtime
        var connectionString = $"Data Source={dbPath};Pooling=true;Foreign Keys=true;";

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite(connectionString));

        // Register services
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddScoped<IGuidRepository, GuidRepository>();
        services.AddScoped<IJwtRepository, JwtRepository>();
        services.AddScoped<IJwtBuilderTemplateRepository, JwtBuilderTemplateRepository>();
        services.AddScoped<IAppSettingsRepository, AppSettingsRepository>();
        services.AddSingleton<IErrorHandler, ErrorHandler>();
        services.AddSingleton<IGuidService, GuidService>();
        services.AddSingleton<IJwtService, JwtService>();
        services.AddSingleton<IJsonFormatterService, JsonFormatterService>();
        services.AddSingleton<IJsonDiffService, JsonDiffService>();
        services.AddSingleton<IBase64EncoderService, Base64EncoderService>();
        services.AddSingleton<IRegexService, RegexService>();

        return services;
    }
}
