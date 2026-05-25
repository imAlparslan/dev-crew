using Spectre.Console.Cli;

namespace DevCrew.Cli.JsonCommands;

internal static class JsonCommandsInjections
{
    public static IConfigurator AddJsonCommands(this IConfigurator config)
    {
        config.AddBranch("json", json =>
        {
            json.AddCommand<FormatJsonCommand>("format");
        });

        return config;
    }
}
