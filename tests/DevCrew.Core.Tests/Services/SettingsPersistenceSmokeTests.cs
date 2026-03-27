using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DevCrew.Core.Data;
using DevCrew.Core.Models;
using DevCrew.Core.Services.Repositories;
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
            DeleteTempDbIfExists(dbPath);
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
            DeleteTempDbIfExists(dbPath);
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
        return services.BuildServiceProvider();
    }

    private static string CreateTempDbPath()
    {
        var directory = Path.Combine(Path.GetTempPath(), "devcrew-smoke-tests");
        Directory.CreateDirectory(directory);
        return Path.Combine(directory, $"settings-smoke-{Guid.NewGuid():N}.db");
    }

    private static void DeleteTempDbIfExists(string dbPath)
    {
        if (File.Exists(dbPath))
        {
            File.Delete(dbPath);
        }
    }
}
