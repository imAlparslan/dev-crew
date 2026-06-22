using System.ComponentModel;
using DevCrew.Cli.Results;
using DevCrew.Core.Domain.Models;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevCrew.Cli.RegexCommands;

internal sealed class RegexListCommandSettings : CommandSettings
{
    [CommandOption("-n|--name <TEXT>")]
    [Description("Filter presets by name.")]
    public string? Name { get; init; }
}

internal sealed class RegexListCommand(IAnsiConsole console, IRegexPresetRepository regexPresetRepository)
    : AsyncCommand<RegexListCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IRegexPresetRepository _regexPresetRepository = regexPresetRepository;

    protected override async Task<int> ExecuteAsync(CommandContext context, RegexListCommandSettings settings, CancellationToken cancellationToken)
    {
        var presets = await _regexPresetRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Name))
        {
            presets = presets
                .Where(preset => preset.Name.Contains(settings.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (presets.Count == 0)
        {
            _console.MarkupLine("[yellow]No regex presets found.[/]");
            return Result.Success;
        }

        var table = new Table();
        table.AddColumn("Id");
        table.AddColumn("Name");
        table.AddColumn("Pattern");
        table.AddColumn("Flags");
        table.AddColumn("Last Used");

        foreach (var preset in presets)
        {
            table.AddRow(
                preset.Id.ToString(),
                Markup.Escape(preset.Name),
                Markup.Escape(preset.Pattern),
                Markup.Escape(RegexPresetCommandFormatting.FormatFlags(preset.IgnoreCase, preset.Multiline)),
                Markup.Escape(preset.LastUsedAt?.ToString("O") ?? "-")
            );
        }

        _console.Write(table);
        return Result.Success;
    }
}

internal sealed class RegexDeleteCommandSettings : CommandSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Preset name to delete.")]
    public required string Name { get; init; }
}

internal sealed class RegexDeleteCommand(IAnsiConsole console, IRegexPresetRepository regexPresetRepository)
    : AsyncCommand<RegexDeleteCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IRegexPresetRepository _regexPresetRepository = regexPresetRepository;

    protected override async Task<int> ExecuteAsync(CommandContext context, RegexDeleteCommandSettings settings, CancellationToken cancellationToken)
    {
        var preset = await ResolvePresetByNameAsync(settings.Name, cancellationToken);
        if (preset is null)
        {
            _console.MarkupLine($"[red]Error:[/] Preset not found: {Markup.Escape(settings.Name)}");
            return Result.Error;
        }

        var deleted = await _regexPresetRepository.DeleteAsync(preset.Id, cancellationToken);
        if (!deleted)
        {
            _console.MarkupLine($"[red]Error:[/] Unable to delete preset: {Markup.Escape(settings.Name)}");
            return Result.Error;
        }

        _console.MarkupLine($"[green]Deleted preset:[/] {Markup.Escape(preset.Name)}");
        return Result.Success;
    }

    private async Task<RegexPreset?> ResolvePresetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        var presets = await _regexPresetRepository.GetAllAsync(cancellationToken);
        return presets.FirstOrDefault(preset => string.Equals(preset.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed class RegexUpdateCommandSettings : CommandSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Preset name to update.")]
    public required string Name { get; init; }

    [CommandOption("-p|--pattern <PATTERN>")]
    [Description("Replace the preset pattern.")]
    public string? Pattern { get; init; }

    [CommandOption("--ignore-case")]
    [Description("Enable case-insensitive matching.")]
    public bool IgnoreCase { get; init; }

    [CommandOption("--case-sensitive")]
    [Description("Disable case-insensitive matching.")]
    public bool CaseSensitive { get; init; }

    [CommandOption("-m|--multiline")]
    [Description("Enable multiline mode.")]
    public bool Multiline { get; init; }

    [CommandOption("--singleline-input")]
    [Description("Disable multiline mode for the preset.")]
    public bool SinglelineInput { get; init; }

    public override ValidationResult Validate()
    {
        if (IgnoreCase && CaseSensitive)
        {
            return ValidationResult.Error("Use only one case mode: --ignore-case or --case-sensitive.");
        }

        if (Multiline && SinglelineInput)
        {
            return ValidationResult.Error("Use only one multiline mode: --multiline or --singleline-input.");
        }

        if (string.IsNullOrWhiteSpace(Pattern) && !IgnoreCase && !CaseSensitive && !Multiline && !SinglelineInput)
        {
            return ValidationResult.Error("Provide at least one change: --pattern, --ignore-case, --case-sensitive, --multiline, or --singleline-input.");
        }

        return ValidationResult.Success();
    }
}

internal sealed class RegexUpdateCommand(IAnsiConsole console, IRegexPresetRepository regexPresetRepository)
    : AsyncCommand<RegexUpdateCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IRegexPresetRepository _regexPresetRepository = regexPresetRepository;

    protected override async Task<int> ExecuteAsync(CommandContext context, RegexUpdateCommandSettings settings, CancellationToken cancellationToken)
    {
        var preset = await ResolvePresetByNameAsync(settings.Name, cancellationToken);
        if (preset is null)
        {
            _console.MarkupLine($"[red]Error:[/] Preset not found: {Markup.Escape(settings.Name)}");
            return Result.Error;
        }

        var updatedPattern = string.IsNullOrWhiteSpace(settings.Pattern)
            ? preset.Pattern
            : settings.Pattern.Trim();

        var updatedIgnoreCase = settings.CaseSensitive
            ? false
            : settings.IgnoreCase || preset.IgnoreCase;

        var updatedMultiline = settings.SinglelineInput
            ? false
            : settings.Multiline || preset.Multiline;

        var updated = await _regexPresetRepository.UpdateAsync(
            preset.Id,
            updatedPattern,
            updatedIgnoreCase,
            updatedMultiline,
            cancellationToken);

        if (updated is null)
        {
            _console.MarkupLine($"[red]Error:[/] Preset not found: {Markup.Escape(settings.Name)}");
            return Result.Error;
        }

        _console.MarkupLine($"[green]Updated preset:[/] {Markup.Escape(updated.Name)}");
        _console.MarkupLine($"[green]Pattern:[/] {Markup.Escape(updated.Pattern)}");
        _console.MarkupLine($"[green]Flags:[/] {Markup.Escape(RegexPresetCommandFormatting.FormatFlags(updated.IgnoreCase, updated.Multiline))}");

        return Result.Success;
    }

    private async Task<RegexPreset?> ResolvePresetByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        var presets = await _regexPresetRepository.GetAllAsync(cancellationToken);
        return presets.FirstOrDefault(preset => string.Equals(preset.Name, normalizedName, StringComparison.OrdinalIgnoreCase));
    }
}

internal static class RegexPresetCommandFormatting
{
    public static string FormatFlags(bool ignoreCase, bool multiline)
    {
        var flags = new List<string>();

        if (ignoreCase)
        {
            flags.Add("ignore-case");
        }

        if (multiline)
        {
            flags.Add("multiline");
        }

        return flags.Count == 0 ? "none" : string.Join(", ", flags);
    }
}