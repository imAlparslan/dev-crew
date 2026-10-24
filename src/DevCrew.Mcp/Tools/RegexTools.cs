using System.ComponentModel;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Domain.Models;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace DevCrew.Mcp.Tools;

internal sealed class RegexTools(
    IServiceScopeFactory scopeFactory,
    IRegexService regexService)
{
    private const int MaxListCount = 100;
    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IRegexService _regexService = regexService;

    [McpServerTool]
    [Description("Matches a regular expression against text. When preset_id is provided, the preset pattern and options are used.")]
    public async Task<object> MatchRegex(
        [Description("Input text to search.")] string input,
        [Description("Regex pattern, required unless preset_id is supplied.")] string? pattern = null,
        [Description("Saved preset ID to load its pattern and matching options.")] int? presetId = null,
        [Description("Ignore case when matching.")] bool ignoreCase = false,
        [Description("Enable multiline matching.")] bool multiline = false,
        CancellationToken cancellationToken = default)
    {
        if (presetId is null && string.IsNullOrWhiteSpace(pattern))
        {
            return McpToolResponse.Failure("Provide a pattern or a preset_id.", "regex_match");
        }

        if (presetId is not null)
        {
            await using var scope = _scopeFactory.CreateAsyncScope();
            var repository = scope.ServiceProvider.GetRequiredService<IRegexPresetRepository>();
            var preset = await repository.GetByIdAsync(presetId.Value, cancellationToken);
            if (preset is null)
            {
                return McpToolResponse.Failure($"Regex preset not found: {presetId.Value}", "regex_match");
            }

            pattern = preset.Pattern;
            ignoreCase = preset.IgnoreCase;
            multiline = preset.Multiline;
            await repository.UpdateLastUsedAsync(preset.Id, cancellationToken);
        }

        var result = _regexService.FindMatches(pattern!, input, ignoreCase, multiline);
        return result.IsValid
            ? McpToolResponse.Success(result, "regex_match")
            : McpToolResponse.Failure(result.ErrorMessage ?? "Invalid regular expression.", "regex_match",
                new { result.ErrorKey, result.ErrorArgs });
    }

    [McpServerTool]
    [Description("Lists saved regex presets with optional name filtering.")]
    public async Task<object> ListRegexPresets(
        [Description("Maximum number of records to return (maximum 100).")] int count = 50,
        [Description("Optional text to search in preset names.")] string? name = null,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return McpToolResponse.Failure("Count must be greater than 0.", "regex_list_presets");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRegexPresetRepository>();
        var presets = await repository.GetAllAsync(cancellationToken);
        var filtered = string.IsNullOrWhiteSpace(name)
            ? presets
            : presets.Where(preset => preset.Name.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        var boundedCount = Math.Min(count, MaxListCount);
        var items = filtered.Take(boundedCount).Select(ToPresetSummary).ToList();

        return McpToolResponse.Success(
            new { count = items.Count, items },
            "regex_list_presets",
            new { requestedCount = count, wasCapped = count > MaxListCount });
    }

    [McpServerTool]
    [Description("Creates a saved regex preset.")]
    public async Task<object> CreateRegexPreset(
        [Description("Unique display name for the preset.")] string name,
        [Description("Regular expression pattern.")] string pattern,
        [Description("Ignore case when matching.")] bool ignoreCase = false,
        [Description("Enable multiline matching.")] bool multiline = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(pattern))
        {
            return McpToolResponse.Failure("Preset name and pattern are required.", "regex_create_preset");
        }

        var validation = _regexService.FindMatches(pattern, string.Empty, ignoreCase, multiline);
        if (!validation.IsValid)
        {
            return McpToolResponse.Failure(validation.ErrorMessage ?? "Invalid regular expression.",
                "regex_create_preset", new { validation.ErrorKey, validation.ErrorArgs });
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRegexPresetRepository>();
        if (await repository.NameExistsAsync(name, cancellationToken))
        {
            return McpToolResponse.Failure($"Regex preset name already exists: {name.Trim()}", "regex_create_preset");
        }

        var preset = await repository.SaveAsync(new RegexPreset
        {
            Name = name.Trim(),
            Pattern = pattern.Trim(),
            IgnoreCase = ignoreCase,
            Multiline = multiline
        }, cancellationToken);

        return McpToolResponse.Success(ToPresetSummary(preset), "regex_create_preset");
    }

    [McpServerTool]
    [Description("Updates the pattern and/or matching options for a saved regex preset.")]
    public async Task<object> UpdateRegexPreset(
        [Description("Saved preset ID.")] int id,
        [Description("Replacement regex pattern.")] string? pattern = null,
        [Description("New case-insensitive option.")] bool? ignoreCase = null,
        [Description("New multiline option.")] bool? multiline = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(pattern) && ignoreCase is null && multiline is null)
        {
            return McpToolResponse.Failure("Provide at least one preset field to update.", "regex_update_preset");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRegexPresetRepository>();
        var current = await repository.GetByIdAsync(id, cancellationToken);
        if (current is null)
        {
            return McpToolResponse.Failure($"Regex preset not found: {id}", "regex_update_preset");
        }

        var updatedPattern = string.IsNullOrWhiteSpace(pattern) ? current.Pattern : pattern.Trim();
        var updatedIgnoreCase = ignoreCase ?? current.IgnoreCase;
        var updatedMultiline = multiline ?? current.Multiline;
        var validation = _regexService.FindMatches(updatedPattern, string.Empty, updatedIgnoreCase, updatedMultiline);
        if (!validation.IsValid)
        {
            return McpToolResponse.Failure(validation.ErrorMessage ?? "Invalid regular expression.",
                "regex_update_preset", new { validation.ErrorKey, validation.ErrorArgs });
        }

        var updated = await repository.UpdateAsync(id, updatedPattern, updatedIgnoreCase, updatedMultiline, cancellationToken);
        return updated is null
            ? McpToolResponse.Failure($"Regex preset not found: {id}", "regex_update_preset")
            : McpToolResponse.Success(ToPresetSummary(updated), "regex_update_preset");
    }

    [McpServerTool]
    [Description("Deletes a saved regex preset by ID.")]
    public async Task<object> DeleteRegexPreset(
        [Description("Saved preset ID.")] int id,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IRegexPresetRepository>();
        var deleted = await repository.DeleteAsync(id, cancellationToken);
        return deleted
            ? McpToolResponse.Success(new { id }, "regex_delete_preset")
            : McpToolResponse.Failure($"Regex preset not found: {id}", "regex_delete_preset");
    }

    private static object ToPresetSummary(RegexPreset preset) => new
    {
        id = preset.Id,
        name = preset.Name,
        pattern = preset.Pattern,
        ignoreCase = preset.IgnoreCase,
        multiline = preset.Multiline,
        createdAt = preset.CreatedAt,
        lastUsedAt = preset.LastUsedAt
    };
}
