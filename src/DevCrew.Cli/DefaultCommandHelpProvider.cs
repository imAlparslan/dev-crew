using Spectre.Console.Cli;
using Spectre.Console.Cli.Help;
using Spectre.Console.Rendering;

namespace DevCrew.Cli;

/// <summary>
/// Spectre omits the options of a branch's default command from the branch help.
/// This provider lists them (e.g. <c>crew guid -h</c> shows <c>-c</c> and <c>-s</c>).
/// </summary>
internal sealed class DefaultCommandHelpProvider(ICommandAppSettings settings) : HelpProvider(settings)
{
    public override IEnumerable<IRenderable> GetOptions(ICommandModel model, ICommandInfo? command)
        => base.GetOptions(model, command?.DefaultCommand ?? command);
}
