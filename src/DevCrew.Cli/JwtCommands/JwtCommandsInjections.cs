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
            jwt.AddCommand<JwtListTemplatesCommand>("list-templates");
            jwt.AddCommand<JwtUpdateTemplateCommand>("update-template");
            jwt.AddCommand<JwtDeleteTemplateCommand>("delete-template");
        });

        return config;
    }
}