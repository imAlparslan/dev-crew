using System.ComponentModel;
using System.Text;
using DevCrew.Cli.Results;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevCrew.Cli.GuidCommands;

internal class DeleteGuidCommandSettings : CommandSettings
{
    [CommandOption("-v|--value [Value]")]
    [DefaultValue("")]
    [Description("Value of the GUID to delete.")]
    public required FlagValue<string> Value { get; init; } = null!;

    [CommandOption("-n|--notes [Notes]")]
    [DefaultValue("")]
    [Description("Notes associated with the GUID to delete.")]
    public required FlagValue<string> Notes { get; init; } = null!;
}

internal class DeleteGuidCommand(IAnsiConsole console, IGuidRepository guidRepository) : AsyncCommand<DeleteGuidCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IGuidRepository _guidRepository = guidRepository;
    protected override async Task<int> ExecuteAsync(CommandContext context, DeleteGuidCommandSettings settings, CancellationToken cancellationToken)
    {
        if (!settings.Value.IsSet && !settings.Notes.IsSet)
        {
            _console.MarkupLine("[red]Error:[/] At least one of the options --value or --notes must be provided to delete a GUID.");
            return Result.Error;
        }
        var guids = await _guidRepository.GetGuidByValueAndNotes(settings.Value.Value, settings.Notes.Value, cancellationToken);
        if (guids.Count == 0)
        {
            _console.MarkupLine("[red]Error:[/] No GUIDs found matching the specified criteria.");
            return Result.Error;
        }

        if (guids.Count > 1)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendLine($"{guids.Count} GUIDs found;");
            guids.Take(3).ToList().ForEach(g => stringBuilder.AppendLine($"Value: {g.GuidValue}, Notes: {g.Notes}"));
            if (guids.Count > 3)
                stringBuilder.AppendLine("...and more.");

            stringBuilder.AppendLine("Please specify a more specific value.");

            _console.MarkupLine(stringBuilder.ToString());
            return Result.Error;
        }
    var guidToDelete = guids[0];
        await _guidRepository.DeleteGuidAsync(guidToDelete.Id, cancellationToken);
        _console.MarkupLine($"[green]Deleted GUID:[/] {guidToDelete.GuidValue}");
        return Result.Success;
    }
}