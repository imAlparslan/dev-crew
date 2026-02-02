using DevCrew.Core.Data;
using DevCrew.Core.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace DevCrew.Core;

public static class DependencyInjection
{
    public static IServiceCollection AddDevCrewCore(this IServiceCollection services)
    {
        // Register DbContext with SQLite
        var dbPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "DevCrew",
            "devcrew.db"
        );

        // Ensure directory exists
        var directory = Path.GetDirectoryName(dbPath);
        if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
        {
            Directory.CreateDirectory(directory);
        }

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlite($"Data Source={dbPath}"));

        // Register services
        services.AddScoped<IApplicationService, ApplicationService>();
        services.AddSingleton<IGuidService, GuidService>();
        services.AddSingleton<IClipboardService, ClipboardService>();
        services.AddSingleton<IJwtService, JwtService>();

        return services;
    }
}
