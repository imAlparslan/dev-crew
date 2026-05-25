using Spectre.Console.Cli;

namespace DevCrew.Cli.JwtCommands;

internal static class JwtCommandsInjections
{
    public static IConfigurator AddJwtCommands(this IConfigurator config)
    {
        config.AddBranch("jwt", jwt =>
        {
            jwt.SetDefaultCommand<DecodeJwtCommand>();
        });

        return config;
    }
}