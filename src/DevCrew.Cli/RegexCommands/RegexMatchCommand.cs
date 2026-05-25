using System.ComponentModel;
using System.Text;
using DevCrew.Cli.Results;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Domain.Models;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Spectre.Console;
using Spectre.Console.Cli;
using TextCopy;

namespace DevCrew.Cli.RegexCommands;

internal sealed class RegexMatchCommandSettings : CommandSettings
{
    [CommandOption("-t|--template <NAME>")]
    [Description("Use a saved regex pattern template by name.")]
    public string? Template { get; init; }

    [CommandOption("-p|--pattern <PATTERN>")]
    [Description("Regex pattern to run.")]
    public string? Pattern { get; init; }

    [CommandOption("-i|--input <TEXT>")]
    [Description("Inline input text.")]
    public string? Input { get; init; }

    [CommandOption("--input-path <PATH>")]
    [Description("Read input text from file path.")]
    public string? InputPath { get; init; }

    [CommandOption("--ignore-case")]
    [Description("Enable case-insensitive matching.")]
    public bool IgnoreCase { get; init; }

    [CommandOption("-m|--multiline")]
    [Description("Enable multiline mode.")]
    public bool Multiline { get; init; }

    [CommandOption("-c|--copy")]
    [Description("Copy full regex report to clipboard.")]
    public bool Copy { get; init; }

    [CommandOption("--save <PATH>")]
    [Description("Save full regex report to file.")]
    public string? SavePath { get; init; }

    [CommandOption("--save-template <NAME>")]
    [Description("Save the effective regex pattern as a reusable template.")]
    public string? SaveTemplate { get; init; }

    public override ValidationResult Validate()
    {
        if (string.IsNullOrWhiteSpace(Pattern) && string.IsNullOrWhiteSpace(Template))
        {
            return ValidationResult.Error("Provide --pattern <PATTERN> or --template <NAME>.");
        }

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

internal sealed class RegexMatchCommand(
    IAnsiConsole console,
    IRegexService regexService,
    IRegexPresetRepository regexPresetRepository,
    IClipboard clipboardService) : AsyncCommand<RegexMatchCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IRegexService _regexService = regexService;
    private readonly IRegexPresetRepository _regexPresetRepository = regexPresetRepository;
    private readonly IClipboard _clipboardService = clipboardService;

    protected override async Task<int> ExecuteAsync(CommandContext context, RegexMatchCommandSettings settings, CancellationToken cancellationToken)
    {
        var patternResolution = await ResolvePatternAsync(settings, cancellationToken);
        if (!patternResolution.Success)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(patternResolution.ErrorMessage ?? "Unable to resolve regex pattern.")}");
            return Result.Error;
        }

        var effectivePattern = patternResolution.Pattern ?? string.Empty;
        var effectiveIgnoreCase = patternResolution.IgnoreCase || settings.IgnoreCase;
        var effectiveMultiline = patternResolution.Multiline || settings.Multiline;

        var inputResult = await ResolveInputAsync(settings, cancellationToken);
        if (!inputResult.Success)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(inputResult.ErrorMessage ?? "Unable to resolve input text.")}");
            return Result.Error;
        }

        var result = _regexService.FindMatches(
            effectivePattern,
            inputResult.Input ?? string.Empty,
            ignoreCase: effectiveIgnoreCase,
            multiline: effectiveMultiline);

        if (!result.IsValid)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(result.ErrorMessage ?? "Regex matching failed.")}");
            return Result.Error;
        }

        var report = BuildReport(result);

        if (settings.Copy)
        {
            try
            {
                await _clipboardService.SetTextAsync(report, cancellationToken);
                _console.MarkupLine("[green]Report copied to clipboard.[/]");
            }
            catch (Exception ex)
            {
                _console.MarkupLine($"[yellow]Warning:[/] Unable to copy report to clipboard: {Markup.Escape(ex.Message)}");
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

                await File.WriteAllTextAsync(savePath, report, Encoding.UTF8, cancellationToken);
                _console.MarkupLine($"[green]Saved:[/] {Markup.Escape(savePath)}");
            }
            catch (Exception ex)
            {
                _console.MarkupLine($"[red]Error:[/] Unable to save report: {Markup.Escape(ex.Message)}");
                return Result.Error;
            }
        }

        if (!string.IsNullOrWhiteSpace(settings.SaveTemplate))
        {
            var templateSaveResult = await SavePresetAsync(settings.SaveTemplate.Trim(), effectivePattern, effectiveIgnoreCase, effectiveMultiline, cancellationToken);
            if (!templateSaveResult.Success)
            {
                _console.MarkupLine($"[red]Error:[/] {Markup.Escape(templateSaveResult.ErrorMessage ?? "Unable to save regex template.")}");
                return Result.Error;
            }

            _console.MarkupLine($"[green]Template saved:[/] {Markup.Escape(settings.SaveTemplate.Trim())}");
        }

        _console.MarkupLine("[green]Regex Match Result:[/]");
        _console.MarkupLine(Markup.Escape(report));

        return Result.Success;
    }

    private async Task<(bool Success, string? Pattern, bool IgnoreCase, bool Multiline, string? ErrorMessage)> ResolvePatternAsync(
        RegexMatchCommandSettings settings,
        CancellationToken cancellationToken)
    {
        if (!string.IsNullOrWhiteSpace(settings.Pattern))
        {
            return (true, settings.Pattern, false, false, null);
        }

        if (string.IsNullOrWhiteSpace(settings.Template))
        {
            return (false, null, false, false, "Pattern source is not set.");
        }

        var templateName = settings.Template.Trim();
        var presets = await _regexPresetRepository.GetAllAsync(cancellationToken);
        var matches = presets
            .Where(preset => string.Equals(preset.Name, templateName, StringComparison.OrdinalIgnoreCase))
            .ToList();

        if (matches.Count == 0)
        {
            return (false, null, false, false, $"Template not found: {templateName}");
        }

        if (matches.Count > 1)
        {
            return (false, null, false, false, $"Multiple templates found with the same name: {templateName}. Use a unique template name.");
        }

        var selected = matches[0];
        _ = await _regexPresetRepository.UpdateLastUsedAsync(selected.Id, cancellationToken);
        return (true, selected.Pattern, selected.IgnoreCase, selected.Multiline, null);
    }

    private async Task<(bool Success, string? ErrorMessage)> SavePresetAsync(
        string templateName,
        string pattern,
        bool ignoreCase,
        bool multiline,
        CancellationToken cancellationToken)
    {
        var exists = await _regexPresetRepository.NameExistsAsync(templateName, cancellationToken);
        if (exists)
        {
            return (false, $"Template name already exists: {templateName}");
        }

        var preset = new RegexPreset
        {
            Name = templateName,
            Pattern = pattern,
            IgnoreCase = ignoreCase,
            Multiline = multiline
        };

        _ = await _regexPresetRepository.SaveAsync(preset, cancellationToken);
        return (true, null);
    }

    private static async Task<(bool Success, string? Input, string? ErrorMessage)> ResolveInputAsync(
        RegexMatchCommandSettings settings,
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

            var fileContent = await File.ReadAllTextAsync(inputPath, Encoding.UTF8, cancellationToken);
            return (true, fileContent, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Unable to read input file: {ex.Message}");
        }
    }

    private static string BuildReport(DevCrew.Core.Domain.Results.RegexMatchResult result)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"Pattern: {result.Pattern}");
        builder.AppendLine($"IgnoreCase: {result.IgnoreCase}");
        builder.AppendLine($"Multiline: {result.Multiline}");
        builder.AppendLine($"InputLength: {result.InputLength}");
        builder.AppendLine($"MatchCount: {result.MatchCount}");

        if (result.MatchCount == 0)
        {
            builder.AppendLine("Matches: none");
            return builder.ToString().TrimEnd();
        }

        for (var i = 0; i < result.Matches.Count; i++)
        {
            var match = result.Matches[i];
            builder.AppendLine($"Match[{i}] Index={match.Index} Length={match.Length} Value={match.Value}");

            if (match.Captures.Count == 0)
            {
                continue;
            }

            for (var j = 0; j < match.Captures.Count; j++)
            {
                var capture = match.Captures[j];
                var captureType = capture.IsNamed ? "named" : "group";
                builder.AppendLine($"  Capture[{j}] ({captureType}) Name={capture.Name} Index={capture.Index} Length={capture.Length} Value={capture.Value}");
            }
        }

        return builder.ToString().TrimEnd();
    }
}
