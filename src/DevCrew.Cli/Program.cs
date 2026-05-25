using DevCrew.Cli;
using DevCrew.Cli.Base64Commands;
using DevCrew.Cli.GuidCommands;
using DevCrew.Cli.JsonCommands;
using DevCrew.Cli.JwtCommands;
using DevCrew.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
using System.Reflection;
using TextCopy;

var services = new ServiceCollection();

var cfg = new ConfigurationBuilder()
    .AddJsonFile("appsettings.json", optional: true)
    .AddEnvironmentVariables()
    .Build();

services.InjectClipboard();
services.AddDevCrewCore(cfg);

var register = new TypeRegistrar(services);

var app = new CommandApp(register);

app.Configure(config =>
{
    config.SetApplicationName("crew");
    config.SetApplicationVersion(ResolveCurrentVersion());

    config.AddBase64Commands();
    config.AddGuidCommands();
    config.AddJsonCommands();
    config.AddJwtCommands();


});
using var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // Prevent immediate process termination
    cancellationTokenSource.Cancel();
    Console.WriteLine("Cancellation requested...");
};

return await app.RunAsync(args, cancellationTokenSource.Token);

static string ResolveCurrentVersion()
{
    var entryAssembly = Assembly.GetEntryAssembly();
    if (entryAssembly is null)
    {
        return "0.0.0";
    }

    var informationalVersion = entryAssembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion;

    if (!string.IsNullOrWhiteSpace(informationalVersion))
    {
        return informationalVersion.Split('+')[0].Trim();
    }

    var version = entryAssembly.GetName().Version;
    if (version is null)
    {
        return "0.0.0";
    }

    var build = version.Build < 0 ? 0 : version.Build;
    return $"{version.Major}.{version.Minor}.{build}";
}
