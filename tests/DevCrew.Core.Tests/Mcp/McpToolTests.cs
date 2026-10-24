using System;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Infrastructure.Persistence;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using DevCrew.Mcp.Tools;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Mcp;

public sealed class McpToolTests
{
    [Fact]
    public async Task GuidTools_CreateListUpdateAndDeleteSavedRecords()
    {
        using var database = new TestDatabase();
        var scopeFactory = database.Provider.GetRequiredService<IServiceScopeFactory>();
        var tools = new global::GuidTools(scopeFactory, new GuidService(scopeFactory));

        var createJson = JsonSerializer.Serialize(await tools.CreateGuid(save: true, notes: "mcp test"));
        using var createDocument = JsonDocument.Parse(createJson);
        var data = createDocument.RootElement.GetProperty("data");
        var id = data.GetProperty("id").GetInt32();
        var guid = data.GetProperty("guid").GetString()!;

        var listJson = JsonSerializer.Serialize(await tools.ListGuids(search: "mcp test"));
        listJson.ShouldContain(guid);

        var updateJson = JsonSerializer.Serialize(await tools.UpdateGuidNotes(id, notes: "updated"));
        updateJson.ShouldContain("\"notes\":\"updated\"");

        var deleteJson = JsonSerializer.Serialize(await tools.DeleteGuid(value: guid));
        deleteJson.ShouldContain("\"success\":true");
    }

    [Fact]
    public void Base64AndJsonTools_ReturnStructuredResults()
    {
        var base64Tools = new Base64Tools(new Base64EncoderService());
        var encoded = JsonSerializer.Serialize(base64Tools.EncodeBase64("hello"));
        encoded.ShouldContain("\"success\":true");
        encoded.ShouldContain("\"output\":\"aGVsbG8=\"");

        var decoded = JsonSerializer.Serialize(base64Tools.DecodeBase64("aGVsbG8="));
        decoded.ShouldContain("\"outputText\":\"hello\"");
        decoded.ShouldContain("\"isUtf8\":true");

        var jsonTools = new JsonTools(new JsonFormatterService(), new JsonDiffService());
        var formatted = JsonSerializer.Serialize(jsonTools.FormatJson("{\"b\":2,\"a\":1}", "prettify", sortKeys: true));
        formatted.ShouldContain("\"success\":true");
        formatted.ShouldContain("\"output\"");

        var invalid = JsonSerializer.Serialize(jsonTools.ValidateJson("{"));
        invalid.ShouldContain("\"success\":false");

        var diff = JsonSerializer.Serialize(jsonTools.CompareJson("{\"value\":1}", "{\"value\":2}"));
        diff.ShouldContain("\"success\":true");
        diff.ShouldContain("\"changedCount\":1");
    }

    [Fact]
    public async Task RegexTools_SupportCreateReadUpdateMatchAndDelete()
    {
        using var database = new TestDatabase();
        var tools = new RegexTools(database.Provider.GetRequiredService<IServiceScopeFactory>(), new RegexService());

        var createJson = JsonSerializer.Serialize(await tools.CreateRegexPreset("digits", @"\d+", ignoreCase: true));
        createJson.ShouldContain("\"success\":true");
        using var createDocument = JsonDocument.Parse(createJson);
        var id = createDocument.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var listJson = JsonSerializer.Serialize(await tools.ListRegexPresets(name: "DIG"));
        listJson.ShouldContain("\"name\":\"digits\"");

        var updateJson = JsonSerializer.Serialize(await tools.UpdateRegexPreset(id, pattern: "[a-z]+", multiline: true));
        using (var updateDocument = JsonDocument.Parse(updateJson))
        {
            var updated = updateDocument.RootElement.GetProperty("data");
            updated.GetProperty("pattern").GetString().ShouldBe("[a-z]+");
            updated.GetProperty("multiline").GetBoolean().ShouldBeTrue();
        }

        var matchJson = JsonSerializer.Serialize(await tools.MatchRegex("abc", presetId: id));
        matchJson.ShouldContain("\"matchCount\":1");

        var deleteJson = JsonSerializer.Serialize(await tools.DeleteRegexPreset(id));
        deleteJson.ShouldContain("\"success\":true");
        var missingJson = JsonSerializer.Serialize(await tools.DeleteRegexPreset(id));
        missingJson.ShouldContain("\"success\":false");
    }

    [Fact]
    public async Task JwtTools_ManageTemplatesWithoutReturningSecrets()
    {
        using var database = new TestDatabase();
        var tools = new JwtTools(database.Provider.GetRequiredService<IServiceScopeFactory>(), new JwtService());
        const string secret = "mcp-test-secret-not-for-output-32-bytes-minimum";

        var createJson = JsonSerializer.Serialize(await tools.CreateJwtTemplate(
            "test-template",
            "HS256",
            secret,
            includeExpiration: false,
            claimsJson: "{\"role\":\"tester\"}"));
        createJson.ShouldContain("\"success\":true");
        createJson.ShouldNotContain(secret);
        using var createDocument = JsonDocument.Parse(createJson);
        var id = createDocument.RootElement.GetProperty("data").GetProperty("id").GetInt32();

        var readJson = JsonSerializer.Serialize(await tools.GetJwtTemplate(id));
        readJson.ShouldContain("\"templateName\":\"test-template\"");
        readJson.ShouldNotContain(secret);

        var buildJson = JsonSerializer.Serialize(await tools.BuildJwt(templateId: id, includeExpiration: false));
        buildJson.ShouldContain("\"success\":true");
        buildJson.ShouldContain("\"token\":");
        buildJson.ShouldNotContain(secret);
        using var buildDocument = JsonDocument.Parse(buildJson);
        var token = buildDocument.RootElement.GetProperty("data").GetProperty("token").GetString()!;
        var decodeJson = JsonSerializer.Serialize(tools.DecodeJwt(token, secret));
        decodeJson.ShouldContain("\"signatureValid\":true");

        var updateJson = JsonSerializer.Serialize(await tools.UpdateJwtTemplate(id, notes: "updated"));
        updateJson.ShouldContain("\"notes\":\"updated\"");
        updateJson.ShouldNotContain(secret);

        var deleteJson = JsonSerializer.Serialize(await tools.DeleteJwtTemplate(id));
        deleteJson.ShouldContain("\"success\":true");
        var missingJson = JsonSerializer.Serialize(await tools.GetJwtTemplate(id));
        missingJson.ShouldContain("\"success\":false");
    }

    private sealed class TestDatabase : IDisposable
    {
        private readonly string _databasePath = Path.Combine(Path.GetTempPath(), $"devcrew_mcp_{Guid.NewGuid():N}.db");

        public TestDatabase()
        {
            var services = new ServiceCollection();
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlite($"Data Source={_databasePath};Pooling=False"));
            services.AddScoped<IGuidRepository, GuidRepository>();
            services.AddScoped<IJwtBuilderTemplateRepository, JwtBuilderTemplateRepository>();
            services.AddScoped<IRegexPresetRepository, RegexPresetRepository>();
            Provider = services.BuildServiceProvider();

            using var scope = Provider.CreateScope();
            DatabaseSchemaInitializer.EnsureCompatibilitySchema(scope.ServiceProvider.GetRequiredService<AppDbContext>());
        }

        public ServiceProvider Provider { get; }

        public void Dispose()
        {
            Provider.Dispose();
            if (File.Exists(_databasePath))
            {
                File.Delete(_databasePath);
            }
        }
    }
}
