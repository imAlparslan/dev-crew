using Spectre.Console.Cli;

namespace DevCrew.Cli.Base64Commands;

internal static class Base64CommandsInjections
{
    public static IConfigurator AddBase64Commands(this IConfigurator config)
    {
        config.AddBranch("base64", base64 =>
        {
            base64.AddCommand<Base64EncodeCommand>("encode");
            base64.AddCommand<Base64DecodeCommand>("decode");
        });

        return config;
    }
}
