using System.ComponentModel;
using DevCrew.Cli.Results;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevCrew.Cli.GuidCommands;

internal sealed class UpdateGuidNotesCommandSettings : CommandSettings
{
    [CommandArgument(0, "<ID>")]
    [Description("Saved GUID record ID to update.")]
    public required int Id { get; init; }

    [CommandOption("-n|--notes <TEXT>")]
    [Description("New notes value.")]
    public string? Notes { get; init; }

    [CommandOption("--clear-notes")]
    [Description("Remove notes from the saved GUID.")]
    public bool ClearNotes { get; init; }

    public override ValidationResult Validate()
    {
        if (!ClearNotes && string.IsNullOrWhiteSpace(Notes))
        {
            return ValidationResult.Error("Provide --notes <TEXT> or --clear-notes.");
        }

        if (ClearNotes && !string.IsNullOrWhiteSpace(Notes))
        {
            return ValidationResult.Error("Use only one notes mode: --notes <TEXT> or --clear-notes.");
        }

        return ValidationResult.Success();
    }
}

internal sealed class UpdateGuidNotesCommand(IAnsiConsole console, IGuidRepository guidRepository)
    : AsyncCommand<UpdateGuidNotesCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IGuidRepository _guidRepository = guidRepository;

    protected override async Task<int> ExecuteAsync(CommandContext context, UpdateGuidNotesCommandSettings settings, CancellationToken cancellationToken)
    {
        var guid = await _guidRepository.GetGuidByIdAsync(settings.Id, cancellationToken);
        if (guid is null)
        {
            _console.MarkupLine($"[red]Error:[/] GUID record not found: {settings.Id}");
            return Result.Error;
        }

        var updatedNotes = settings.ClearNotes ? null : settings.Notes?.Trim();
        var updated = await _guidRepository.UpdateGuidNotesAsync(settings.Id, updatedNotes, cancellationToken);
        if (!updated)
        {
            _console.MarkupLine($"[red]Error:[/] Unable to update GUID record: {settings.Id}");
            return Result.Error;
        }

        _console.MarkupLine($"[green]Updated GUID notes:[/] Id={guid.Id} Guid={guid.GuidValue}");
        _console.MarkupLine($"[green]Notes:[/] {Markup.Escape(updatedNotes ?? "-")}");
        return Result.Success;
    }
}
