using Spectre.Console.Cli;

namespace DevCrew.Cli.RegexCommands;

internal static class RegexCommandsInjections
{
    public static IConfigurator AddRegexCommands(this IConfigurator config)
    {
        config.AddBranch("regex", regex =>
        {
            regex.AddCommand<RegexMatchCommand>("match");
        });

        return config;
    }
}
