using Spectre.Console.Cli;

namespace DevCrew.Cli.GuidCommands;

internal static class GuidCommandsInjections
{
    public static IConfigurator AddGuidCommands(this IConfigurator config)
    {
        config.AddBranch("guid", guid =>
        {
            guid.SetDefaultCommand<CreateGuidCommands>();
            guid.AddCommand<ListGuidCommands>("list");
            guid.AddCommand<DeleteGuidCommand>("delete");

        });

        return config;
    }
}
