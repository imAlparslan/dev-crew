using System.ComponentModel;
using System.Text.RegularExpressions;
using DevCrew.Cli.Results;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevCrew.Cli.GuidCommands;

internal class ListGuidCommandSettings : CommandSettings
{
    [CommandOption("-c|--count")]
    [DefaultValue(5)]
    public int Count { get; init; } = 5;

    [CommandOption("-s|--search")]
    [DefaultValue(null)]
    public string? Search { get; init; } = null;
}

internal class ListGuidCommands(IAnsiConsole console, IGuidRepository guidRepository) : AsyncCommand<ListGuidCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IGuidRepository _guidRepository = guidRepository;

    protected override async Task<int> ExecuteAsync(CommandContext context, ListGuidCommandSettings settings, CancellationToken cancellationToken)
    {
        var guids = await _guidRepository.GetGuidsPagedAsync(skip: 0, take: settings.Count, searchQuery: settings.Search, cancellationToken);
        foreach (var guid in guids)
        {
            var guidText = Regex.Replace(guid.GuidValue, settings.Search ?? string.Empty, match => $"[yellow]{match.Value}[/]");
            var noteText = Regex.Replace(guid.Notes ?? string.Empty, settings.Search ?? string.Empty, match => $"[yellow]{match.Value}[/]");
            _console.MarkupLine($"[green]Guid:[/] {guidText} [green]Notes:[/] {noteText}");
        }
        return Result.Success;
    }
}