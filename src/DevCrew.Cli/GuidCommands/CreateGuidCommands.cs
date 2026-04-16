using System.ComponentModel;
using DevCrew.Cli.Results;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Spectre.Console;
using Spectre.Console.Cli;
using TextCopy;

namespace DevCrew.Cli.GuidCommands;

internal class CreateGuidCommandSettings : CommandSettings
{
    [CommandOption("-c|--copy")]
    [Description("Copy the generated GUID to the clipboard.")]
    public bool Copy { get; init; }

    [CommandOption("-s|--save [Name]")]
    [DefaultValue(null)]
    [Description("Name to associate with the saved GUID.")]
    public FlagValue<string?> Save { get; init; } = default!;
}

internal class CreateGuidCommands(IAnsiConsole console,
                               IGuidService guidService,
                               IGuidRepository guidRepository,
                               IClipboard clipboardService) : AsyncCommand<CreateGuidCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IGuidService _guidService = guidService;
    private readonly IGuidRepository _guidRepository = guidRepository;
    private readonly IClipboard _clipboardService = clipboardService;
    protected override async Task<int> ExecuteAsync(CommandContext context, CreateGuidCommandSettings settings, CancellationToken cancellationToken)
    {
        var guid = _guidService.Generate();
        if (settings.Copy)
            await _clipboardService.SetTextAsync(guid, cancellationToken);

        if (settings.Save.IsSet)
            await _guidRepository.SaveGuidAsync(guid, settings.Save.Value, cancellationToken);
        
        _console.MarkupLine($"[green]Generated Guid:[/] {guid}");
        return Result.Success;
    }
}