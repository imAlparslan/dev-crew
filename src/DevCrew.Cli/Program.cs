using DevCrew.Cli;
using DevCrew.Cli.GuidCommands;
using DevCrew.Core;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console.Cli;
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

    config.AddGuidCommands();


});
var cancellationTokenSource = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true; // Prevent immediate process termination
    cancellationTokenSource.Cancel();
    Console.WriteLine("Cancellation requested...");
};
return await app.RunAsync(args, cancellationTokenSource.Token);
