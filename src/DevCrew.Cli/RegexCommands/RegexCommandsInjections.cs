using Spectre.Console.Cli;

namespace DevCrew.Cli.RegexCommands;

internal static class RegexCommandsInjections
{
    public static IConfigurator AddRegexCommands(this IConfigurator config)
    {
        config.AddBranch("regex", regex =>
        {
            regex.AddCommand<RegexListCommand>("list");
            regex.AddCommand<RegexMatchCommand>("match");
            regex.AddCommand<RegexUpdateCommand>("update");
            regex.AddCommand<RegexDeleteCommand>("delete");
        });

        return config;
    }
}
