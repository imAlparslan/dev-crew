using System.ComponentModel;
using DevCrew.Cli.Results;
using DevCrew.Core.Application.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using TextCopy;

namespace DevCrew.Cli.JsonCommands;

internal sealed class FormatJsonCommandSettings : CommandSettings
{
    [CommandOption("-i|--input <JSON>")]
    [Description("Input JSON string to format.")]
    public string? Input { get; init; }

    [CommandOption("--input-path <PATH>")]
    [Description("Read input JSON from file path.")]
    public string? InputPath { get; init; }

    [CommandOption("-p|--prettify|--pretify")]
    [Description("Format JSON with indentation.")]
    public bool Prettify { get; init; }

    [CommandOption("-m|--minify")]
    [Description("Minify JSON output.")]
    public bool Minify { get; init; }

    [CommandOption("-s|--sort")]
    [Description("Sort object keys alphabetically.")]
    public bool Sort { get; init; }

    [CommandOption("-c|--copy")]
    [Description("Copy formatted output to clipboard.")]
    public bool Copy { get; init; }

    [CommandOption("--save <PATH>")]
    [Description("Save formatted output to the specified file path.")]
    public string? SavePath { get; init; }

    public override ValidationResult Validate()
    {
        var hasInlineInput = !string.IsNullOrWhiteSpace(Input);
        var hasInputPath = !string.IsNullOrWhiteSpace(InputPath);

        if (!hasInlineInput && !hasInputPath)
        {
            return ValidationResult.Error("Provide one input source: --input <JSON> or --input-path <PATH>.");
        }

        if (hasInlineInput && hasInputPath)
        {
            return ValidationResult.Error("Use only one input source: --input <JSON> or --input-path <PATH>.");
        }

        if (!Prettify && !Minify)
        {
            return ValidationResult.Error("Select one operation: --prettify (or --pretify) or --minify.");
        }

        if (Prettify && Minify)
        {
            return ValidationResult.Error("Use only one operation: --prettify (or --pretify) or --minify.");
        }

        return ValidationResult.Success();
    }
}

internal sealed class FormatJsonCommand(
    IAnsiConsole console,
    IJsonFormatterService jsonFormatterService,
    IClipboard clipboardService) : AsyncCommand<FormatJsonCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IJsonFormatterService _jsonFormatterService = jsonFormatterService;
    private readonly IClipboard _clipboardService = clipboardService;

    protected override async Task<int> ExecuteAsync(CommandContext context, FormatJsonCommandSettings settings, CancellationToken cancellationToken)
    {
        var inputResult = await ResolveInputAsync(settings, cancellationToken);
        if (!inputResult.Success)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(inputResult.ErrorMessage ?? "Unable to resolve input JSON.")}");
            return Result.Error;
        }

        var input = inputResult.Input ?? string.Empty;
        var result = settings.Minify
            ? _jsonFormatterService.Minify(input, settings.Sort)
            : _jsonFormatterService.Prettify(input, settings.Sort);

        if (!result.IsValid)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(result.ErrorMessage ?? "Unable to process JSON input.")}");
            return Result.Error;
        }

        var output = result.Output;

        if (settings.Copy)
        {
            try
            {
                await _clipboardService.SetTextAsync(output, cancellationToken);
                _console.MarkupLine("[green]Output copied to clipboard.[/]");
            }
            catch (Exception ex)
            {
                _console.MarkupLine($"[yellow]Warning:[/] Unable to copy output to clipboard: {Markup.Escape(ex.Message)}");
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.SavePath))
        {
            try
            {
                var savePath = settings.SavePath.Trim();
                var directory = Path.GetDirectoryName(savePath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllTextAsync(savePath, output, cancellationToken);
                _console.MarkupLine($"[green]Saved:[/] {Markup.Escape(savePath)}");
            }
            catch (Exception ex)
            {
                _console.MarkupLine($"[red]Error:[/] Unable to save output: {Markup.Escape(ex.Message)}");
                return Result.Error;
            }
        }

        _console.MarkupLine("[green]Output:[/]");
        _console.MarkupLine(Markup.Escape(output));

        return Result.Success;
    }

    private static async Task<(bool Success, string? Input, string? ErrorMessage)> ResolveInputAsync(
        FormatJsonCommandSettings settings,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(settings.Input))
        {
            return (true, settings.Input, null);
        }

        if (string.IsNullOrWhiteSpace(settings.InputPath))
        {
            return (false, null, "Input source is not set.");
        }

        var inputPath = settings.InputPath.Trim();
        try
        {
            if (!File.Exists(inputPath))
            {
                return (false, null, $"Input file not found: {inputPath}");
            }

            var fileContent = await File.ReadAllTextAsync(inputPath, cancellationToken);
            return (true, fileContent, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Unable to read input file: {ex.Message}");
        }
    }
}
