using Spectre.Console.Cli;

namespace DevCrew.Cli.JwtCommands;

internal static class JwtCommandsInjections
{
    public static IConfigurator AddJwtCommands(this IConfigurator config)
    {
        config.AddBranch("jwt", jwt =>
        {
            jwt.AddCommand<JwtDecodeCommand>("decode");
            jwt.AddCommand<JwtEncodeCommand>("encode");
        });

        return config;
    }
}