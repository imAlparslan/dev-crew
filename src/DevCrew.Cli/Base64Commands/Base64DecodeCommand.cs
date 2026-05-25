using System.ComponentModel;
using System.Text;
using DevCrew.Cli.Results;
using DevCrew.Core.Application.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using TextCopy;

namespace DevCrew.Cli.Base64Commands;

internal sealed class Base64DecodeCommandSettings : CommandSettings
{
    [CommandOption("-i|--input <BASE64>")]
    [Description("Inline Base64 input string to decode.")]
    public string? Input { get; init; }

    [CommandOption("--input-path <PATH>")]
    [Description("Read Base64 input from file path.")]
    public string? InputPath { get; init; }

    [CommandOption("--output-path <PATH>")]
    [Description("Save decoded bytes to file. If omitted, output is printed as UTF-8 text.")]
    public string? OutputPath { get; init; }

    [CommandOption("-c|--copy")]
    [Description("Copy decoded UTF-8 text output to clipboard (only when --output-path is not used).")]
    public bool Copy { get; init; }

    public override ValidationResult Validate()
    {
        var hasInlineInput = !string.IsNullOrWhiteSpace(Input);
        var hasInputPath = !string.IsNullOrWhiteSpace(InputPath);

        if (!hasInlineInput && !hasInputPath)
        {
            return ValidationResult.Error("Provide one input source: --input <BASE64> or --input-path <PATH>.");
        }

        if (hasInlineInput && hasInputPath)
        {
            return ValidationResult.Error("Use only one input source: --input <BASE64> or --input-path <PATH>.");
        }

        if (!string.IsNullOrWhiteSpace(OutputPath) && Copy)
        {
            return ValidationResult.Error("--copy cannot be used together with --output-path.");
        }

        return ValidationResult.Success();
    }
}

internal sealed class Base64DecodeCommand(
    IAnsiConsole console,
    IBase64EncoderService base64EncoderService,
    IClipboard clipboardService) : AsyncCommand<Base64DecodeCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IBase64EncoderService _base64EncoderService = base64EncoderService;
    private readonly IClipboard _clipboardService = clipboardService;

    protected override async Task<int> ExecuteAsync(CommandContext context, Base64DecodeCommandSettings settings, CancellationToken cancellationToken)
    {
        var inputResult = await ResolveInputStringAsync(settings, cancellationToken);
        if (!inputResult.Success)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(inputResult.ErrorMessage ?? "Unable to resolve input.")}");
            return Result.Error;
        }

        var result = _base64EncoderService.Decode(inputResult.Input);
        if (!result.IsSuccess || result.Output is null)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(result.ErrorMessage ?? "Unable to decode input.")}");
            return Result.Error;
        }

        if (!string.IsNullOrWhiteSpace(settings.OutputPath))
        {
            try
            {
                var outputPath = settings.OutputPath.Trim();
                var directory = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrWhiteSpace(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                await File.WriteAllBytesAsync(outputPath, result.Output, cancellationToken);
                _console.MarkupLine($"[green]Saved:[/] {Markup.Escape(outputPath)}");
                return Result.Success;
            }
            catch (Exception ex)
            {
                _console.MarkupLine($"[red]Error:[/] Unable to save decoded bytes: {Markup.Escape(ex.Message)}");
                return Result.Error;
            }
        }

        var textOutput = Encoding.UTF8.GetString(result.Output);

        if (settings.Copy)
        {
            try
            {
                await _clipboardService.SetTextAsync(textOutput, cancellationToken);
                _console.MarkupLine("[green]Output copied to clipboard.[/]");
            }
            catch (Exception ex)
            {
                _console.MarkupLine($"[yellow]Warning:[/] Unable to copy output to clipboard: {Markup.Escape(ex.Message)}");
            }
        }

        _console.MarkupLine("[green]Output:[/]");
        _console.MarkupLine(Markup.Escape(textOutput));

        return Result.Success;
    }

    private static async Task<(bool Success, string Input, string? ErrorMessage)> ResolveInputStringAsync(
        Base64DecodeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(settings.Input))
        {
            return (true, settings.Input, null);
        }

        if (string.IsNullOrWhiteSpace(settings.InputPath))
        {
            return (false, string.Empty, "Input source is not set.");
        }

        var inputPath = settings.InputPath.Trim();
        try
        {
            if (!File.Exists(inputPath))
            {
                return (false, string.Empty, $"Input file not found: {inputPath}");
            }

            var fileContent = await File.ReadAllTextAsync(inputPath, Encoding.UTF8, cancellationToken);
            return (true, fileContent, null);
        }
        catch (Exception ex)
        {
            return (false, string.Empty, $"Unable to read input file: {ex.Message}");
        }
    }
}
