using System.ComponentModel;
using System.Text;
using DevCrew.Cli.Results;
using DevCrew.Core.Application.Services;
using Spectre.Console;
using Spectre.Console.Cli;
using TextCopy;

namespace DevCrew.Cli.Base64Commands;

internal sealed class Base64EncodeCommandSettings : CommandSettings
{
    [CommandOption("-i|--input <TEXT>")]
    [Description("Inline text input to encode as Base64.")]
    public string? Input { get; init; }

    [CommandOption("--input-path <PATH>")]
    [Description("Read input bytes from file path.")]
    public string? InputPath { get; init; }

    [CommandOption("-c|--copy")]
    [Description("Copy encoded Base64 output to clipboard.")]
    public bool Copy { get; init; }

    [CommandOption("--save <PATH>")]
    [Description("Save encoded Base64 output to file.")]
    public string? SavePath { get; init; }

    public override ValidationResult Validate()
    {
        var hasInlineInput = !string.IsNullOrWhiteSpace(Input);
        var hasInputPath = !string.IsNullOrWhiteSpace(InputPath);

        if (!hasInlineInput && !hasInputPath)
        {
            return ValidationResult.Error("Provide one input source: --input <TEXT> or --input-path <PATH>.");
        }

        if (hasInlineInput && hasInputPath)
        {
            return ValidationResult.Error("Use only one input source: --input <TEXT> or --input-path <PATH>.");
        }

        return ValidationResult.Success();
    }
}

internal sealed class Base64EncodeCommand(
    IAnsiConsole console,
    IBase64EncoderService base64EncoderService,
    IClipboard clipboardService) : AsyncCommand<Base64EncodeCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IBase64EncoderService _base64EncoderService = base64EncoderService;
    private readonly IClipboard _clipboardService = clipboardService;

    protected override async Task<int> ExecuteAsync(CommandContext context, Base64EncodeCommandSettings settings, CancellationToken cancellationToken)
    {
        var inputResult = await ResolveInputBytesAsync(settings, cancellationToken);
        if (!inputResult.Success)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(inputResult.ErrorMessage ?? "Unable to resolve input.")}");
            return Result.Error;
        }

        var result = _base64EncoderService.Encode(inputResult.Bytes ?? []);
        if (!result.IsSuccess)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(result.ErrorMessage ?? "Unable to encode input.")}");
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

                await File.WriteAllTextAsync(savePath, output, Encoding.UTF8, cancellationToken);
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

    private static async Task<(bool Success, byte[]? Bytes, string? ErrorMessage)> ResolveInputBytesAsync(
        Base64EncodeCommandSettings settings,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(settings.Input))
        {
            return (true, Encoding.UTF8.GetBytes(settings.Input), null);
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

            var bytes = await File.ReadAllBytesAsync(inputPath, cancellationToken);
            return (true, bytes, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Unable to read input file: {ex.Message}");
        }
    }
}
