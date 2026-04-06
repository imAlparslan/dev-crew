using DevCrew.Core;
using DevCrew.Core.Application.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Spectre.Console;

var configuration = BuildConfiguration();
var services = BuildServices(configuration);

return await ExecuteAsync(args, services);

static IConfiguration BuildConfiguration()
{
	var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Production";

	return new ConfigurationBuilder()
		.SetBasePath(AppContext.BaseDirectory)
		.AddJsonFile("appsettings.json", optional: true, reloadOnChange: false)
		.AddJsonFile($"appsettings.{environment}.json", optional: true, reloadOnChange: false)
		.AddEnvironmentVariables("DEVCREW_")
		.Build();
}

static ServiceProvider BuildServices(IConfiguration configuration)
{
	var services = new ServiceCollection();
	services.AddSingleton(configuration);
	services.AddDevCrewCore(configuration);

	return services.BuildServiceProvider();
}

static async Task<int> ExecuteAsync(string[] args, ServiceProvider serviceProvider)
{
	var command = args.FirstOrDefault()?.Trim().ToLowerInvariant();

	if (string.IsNullOrWhiteSpace(command) || command is "help" or "--help" or "-h")
	{
		ShowHelp();
		return 0;
	}

	if (command is "version" or "--version" or "-v")
	{
		var version = typeof(Program).Assembly.GetName().Version?.ToString() ?? "unknown";
		AnsiConsole.MarkupLine($"[green]DevCrew CLI[/] version: [bold]{version}[/]");
		return 0;
	}

	if (command == "health")
	{
		await using var scope = serviceProvider.CreateAsyncScope();

		_ = scope.ServiceProvider.GetRequiredService<IGuidService>();
		_ = scope.ServiceProvider.GetRequiredService<IJwtService>();
		_ = scope.ServiceProvider.GetRequiredService<IJsonFormatterService>();

		AnsiConsole.MarkupLine("[green]Health check passed[/]: Core services are resolvable.");
		return 0;
	}

	AnsiConsole.MarkupLine($"[red]Unknown command[/]: {command}");
	ShowHelp();
	return 1;
}

static void ShowHelp()
{
	var table = new Table().RoundedBorder();
	table.AddColumn("Command");
	table.AddColumn("Description");

	table.AddRow("health", "Checks Core DI/service resolution");
	table.AddRow("version", "Prints CLI version");
	table.AddRow("help", "Shows this help");

	AnsiConsole.Write(table);
}
