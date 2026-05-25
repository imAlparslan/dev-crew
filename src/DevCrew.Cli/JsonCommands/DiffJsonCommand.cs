using System.ComponentModel;
using DevCrew.Cli.Results;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Shared.Enums;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevCrew.Cli.JsonCommands;

internal sealed class DiffJsonCommandSettings : CommandSettings
{
    [CommandOption("-l|--left-input <JSON>")]
    [Description("Left JSON input string.")]
    public string? LeftInput { get; init; }

    [CommandOption("--left-input-path <PATH>")]
    [Description("Read left JSON input from file path.")]
    public string? LeftInputPath { get; init; }

    [CommandOption("-r|--right-input <JSON>")]
    [Description("Right JSON input string.")]
    public string? RightInput { get; init; }

    [CommandOption("--right-input-path <PATH>")]
    [Description("Read right JSON input from file path.")]
    public string? RightInputPath { get; init; }

    public override ValidationResult Validate()
    {
        var hasLeftInline = !string.IsNullOrWhiteSpace(LeftInput);
        var hasLeftPath = !string.IsNullOrWhiteSpace(LeftInputPath);
        var hasRightInline = !string.IsNullOrWhiteSpace(RightInput);
        var hasRightPath = !string.IsNullOrWhiteSpace(RightInputPath);

        if (!hasLeftInline && !hasLeftPath)
        {
            return ValidationResult.Error("Provide left input: --left-input <JSON> or --left-input-path <PATH>.");
        }

        if (hasLeftInline && hasLeftPath)
        {
            return ValidationResult.Error("Use only one left input source: --left-input <JSON> or --left-input-path <PATH>.");
        }

        if (!hasRightInline && !hasRightPath)
        {
            return ValidationResult.Error("Provide right input: --right-input <JSON> or --right-input-path <PATH>.");
        }

        if (hasRightInline && hasRightPath)
        {
            return ValidationResult.Error("Use only one right input source: --right-input <JSON> or --right-input-path <PATH>.");
        }

        return ValidationResult.Success();
    }
}

internal sealed class DiffJsonCommand(IAnsiConsole console, IJsonDiffService jsonDiffService) : AsyncCommand<DiffJsonCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IJsonDiffService _jsonDiffService = jsonDiffService;

    protected override async Task<int> ExecuteAsync(CommandContext context, DiffJsonCommandSettings settings, CancellationToken cancellationToken)
    {
        var leftResult = await ResolveInputAsync(settings.LeftInput, settings.LeftInputPath, "left", cancellationToken);
        if (!leftResult.Success)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(leftResult.ErrorMessage ?? "Unable to resolve left input.")}");
            return Result.Error;
        }

        var rightResult = await ResolveInputAsync(settings.RightInput, settings.RightInputPath, "right", cancellationToken);
        if (!rightResult.Success)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(rightResult.ErrorMessage ?? "Unable to resolve right input.")}");
            return Result.Error;
        }

        var diffResult = _jsonDiffService.Compare(leftResult.Input ?? string.Empty, rightResult.Input ?? string.Empty);
        if (!diffResult.IsValid)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(diffResult.ErrorMessage ?? "Unable to compare JSON inputs.")}");
            return Result.Error;
        }

        _console.MarkupLine("[green]JSON Diff Completed[/]");
        _console.MarkupLine($"[green]Added:[/] {diffResult.Summary.AddedCount}");
        _console.MarkupLine($"[green]Removed:[/] {diffResult.Summary.RemovedCount}");
        _console.MarkupLine($"[green]Changed:[/] {diffResult.Summary.ChangedCount}");
        _console.MarkupLine($"[green]Unchanged:[/] {diffResult.Summary.UnchangedCount}");
        _console.MarkupLine($"[green]Total Differences:[/] {diffResult.Summary.TotalDifferences}");

        if (diffResult.PathDiffs.Count == 0)
        {
            _console.MarkupLine("[green]No path-level differences found.[/]");
            return Result.Success;
        }

        _console.MarkupLine("[green]Path Differences:[/]");
        foreach (var entry in diffResult.PathDiffs)
        {
            var kind = entry.Kind.ToString().ToUpperInvariant();
            var color = ResolveKindColor(entry.Kind);
            var leftValue = string.IsNullOrWhiteSpace(entry.LeftValue) ? "-" : entry.LeftValue;
            var rightValue = string.IsNullOrWhiteSpace(entry.RightValue) ? "-" : entry.RightValue;

            _console.MarkupLine($"[{color}]{Markup.Escape(kind)}[/] {Markup.Escape(entry.Path)} | left={Markup.Escape(leftValue)} | right={Markup.Escape(rightValue)}");
        }

        return Result.Success;
    }

    private static async Task<(bool Success, string? Input, string? ErrorMessage)> ResolveInputAsync(
        string? inlineInput,
        string? inputPath,
        string side,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(inlineInput))
        {
            return (true, inlineInput, null);
        }

        if (string.IsNullOrWhiteSpace(inputPath))
        {
            return (false, null, $"{side} input source is not set.");
        }

        var trimmedPath = inputPath.Trim();
        try
        {
            if (!File.Exists(trimmedPath))
            {
                return (false, null, $"{side} input file not found: {trimmedPath}");
            }

            var fileContent = await File.ReadAllTextAsync(trimmedPath, cancellationToken);
            return (true, fileContent, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Unable to read {side} input file: {ex.Message}");
        }
    }

    private static string ResolveKindColor(JsonDiffKind kind)
    {
        return kind switch
        {
            JsonDiffKind.Added => "green",
            JsonDiffKind.Removed => "red",
            JsonDiffKind.Changed => "yellow",
            JsonDiffKind.Unchanged => "grey",
            _ => "white"
        };
    }
}
