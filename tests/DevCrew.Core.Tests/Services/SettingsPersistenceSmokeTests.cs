using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevCrew.Core.Domain.Models;
using DevCrew.Core.Infrastructure.Persistence;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Services;

public class SettingsPersistenceSmokeTests
{
    [Fact]
    public async Task FirstLaunch_CreatesSingletonAppSettingsRow()
    {
        var dbPath = CreateTempDbPath();
        try
        {
            await using var provider = BuildProvider(dbPath);
            using var scope = provider.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            DatabaseSchemaInitializer.EnsureCompatibilitySchema(dbContext);

            var repository = scope.ServiceProvider.GetRequiredService<IAppSettingsRepository>();
            var settings = await repository.GetOrCreateAsync();

            settings.ShouldNotBeNull();
            settings.Id.ShouldBe(AppSettings.SingletonId);
            settings.LanguageCultureName.ShouldBe(AppSettings.DefaultLanguageCultureName);
            dbContext.AppSettings.Count().ShouldBe(1);
        }
        finally
        {
            await DeleteTempDbIfExistsAsync(dbPath);
        }
    }

    [Fact]
    public async Task LanguageChange_ThenRestart_PreservesLanguagePreference()
    {
        var dbPath = CreateTempDbPath();
        try
        {
            const string selectedLanguage = "fr-FR";

            await using (var provider = BuildProvider(dbPath))
            {
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DatabaseSchemaInitializer.EnsureCompatibilitySchema(dbContext);

                var repository = scope.ServiceProvider.GetRequiredService<IAppSettingsRepository>();
                _ = await repository.GetOrCreateAsync();
                _ = await repository.UpdateLanguageAsync(selectedLanguage);
            }

            await using (var provider = BuildProvider(dbPath))
            {
                using var scope = provider.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                DatabaseSchemaInitializer.EnsureCompatibilitySchema(dbContext);

                var repository = scope.ServiceProvider.GetRequiredService<IAppSettingsRepository>();
                var settingsAfterRestart = await repository.GetOrCreateAsync();

                settingsAfterRestart.LanguageCultureName.ShouldBe(selectedLanguage);
                dbContext.AppSettings.Count().ShouldBe(1);
            }
        }
        finally
        {
            await DeleteTempDbIfExistsAsync(dbPath);
        }
    }

    private static ServiceProvider BuildProvider(string dbPath)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Database:FilePath"] = dbPath
            })
            .Build();

        var services = new ServiceCollection();
        services.AddDevCrewCore(configuration);

        // Disable SQLite pooling in this smoke test to ensure temp DB files are released deterministically.
        var smokeTestConnectionString = $"Data Source={dbPath};Pooling=false;Foreign Keys=true;";
        services.AddDbContext<AppDbContext>(options => options.UseSqlite(smokeTestConnectionString));

        return services.BuildServiceProvider();
    }

    private static string CreateTempDbPath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "devcrew-smoke-tests");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"settings-smoke-{Guid.NewGuid():N}.db");
    }

    private static async Task DeleteTempDbIfExistsAsync(string dbPath)
    {
        const int maxAttempts = 10;
        for (var attempt = 0; attempt < maxAttempts; attempt++)
        {
            if (!File.Exists(dbPath))
            {
                return;
            }

            try
            {
                File.Delete(dbPath);
                return;
            }
            catch (IOException) when (attempt < maxAttempts - 1)
            {
            }
            catch (UnauthorizedAccessException) when (attempt < maxAttempts - 1)
            {
            }

            await Task.Delay(TimeSpan.FromMilliseconds(50));
        }

        // Final attempt should throw and fail loudly if lock never clears.
        File.Delete(dbPath);
    }
}
