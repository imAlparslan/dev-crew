using DevCrew.Core;
using DevCrew.Core.Infrastructure.Persistence;
using DevCrew.Mcp.Tools;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

var builder = Host.CreateApplicationBuilder(args);

// Configure all logs to go to stderr (stdout is used for the MCP protocol messages).
builder.Logging.AddConsole(o => o.LogToStandardErrorThreshold = LogLevel.Trace);

// Register core app services (database, repositories, and business services).
builder.Services.AddDevCrewCore(builder.Configuration);

// Add the MCP services: the transport to use (stdio) and the tools to register.
builder.Services
    .AddMcpServer()
    .WithStdioServerTransport()
    .WithTools<GuidTools>()
    .WithTools<Base64Tools>()
    .WithTools<JsonTools>()
    .WithTools<JwtTools>()
    .WithTools<RegexTools>();

var app = builder.Build();
await using (var scope = app.Services.CreateAsyncScope())
{
    DatabaseSchemaInitializer.EnsureCompatibilitySchema(
        scope.ServiceProvider.GetRequiredService<AppDbContext>());
}

await app.RunAsync();
