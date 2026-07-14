using System.ComponentModel;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

internal sealed class GuidTools(IServiceScopeFactory scopeFactory, IGuidService guidService)
{
    private const int MaxListCount = 100;
    private const string SearchModePrefix = "prefix";
    private const string SearchModeContains = "contains";

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IGuidService _guidService = guidService;

    private static object SuccessResponse(object? data = null, object? meta = null)
        => new
        {
            success = true,
            error = (string?)null,
            data,
            meta
        };

    private static object ErrorResponse(string error, object? data = null, object? meta = null)
        => new
        {
            success = false,
            error,
            data,
            meta
        };

    [McpServerTool]
    [Description("Generates a GUID and optionally saves it with notes.")]
    public async Task<object> CreateGuid(
        [Description("When true, saves the generated GUID to storage.")] bool save = false,
        [Description("Optional notes to associate with the saved GUID.")] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var guid = _guidService.Generate();

        if (!save)
        {
            return SuccessResponse(
                data: new
                {
                    guid,
                    saved = false,
                    id = (int?)null,
                    notes = (string?)null
                },
                meta: new
                {
                    operation = "create_guid"
                });
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGuidRepository>();

        var saved = await repository.SaveGuidAsync(guid, notes, cancellationToken);
        return SuccessResponse(
            data: new
            {
                guid,
                saved = true,
                id = saved.Id,
                notes = saved.Notes
            },
            meta: new
            {
                operation = "create_guid"
            });
    }

    [McpServerTool]
    [Description("Lists saved GUID records with optional search filtering.")]
    public async Task<object> ListGuids(
        [Description("Maximum number of records to return.")] int count = 5,
        [Description("Optional search text to filter by GUID value or notes.")] string? search = null,
        [Description("Search mode: 'prefix' (faster, index-friendly) or 'contains'.")] string searchMode = SearchModePrefix,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return ErrorResponse("Count must be greater than 0.");
        }

        var normalizedSearchMode = string.IsNullOrWhiteSpace(searchMode)
            ? SearchModePrefix
            : searchMode.Trim().ToLowerInvariant();

        if (normalizedSearchMode is not SearchModePrefix and not SearchModeContains)
        {
            return ErrorResponse(
                "Invalid search_mode. Use 'prefix' or 'contains'.",
                meta: new
                {
                    searchMode,
                    supportedModes = new[] { SearchModePrefix, SearchModeContains }
                });
        }

        var usePrefixSearch = normalizedSearchMode == SearchModePrefix;
        var boundedCount = Math.Min(count, MaxListCount);

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGuidRepository>();

        var items = await repository.GetGuidsPagedAsync(
            skip: 0,
            take: boundedCount,
            searchQuery: search,
            cancellationToken: cancellationToken,
            prefixSearch: usePrefixSearch);

        return SuccessResponse(
            data: new
            {
                count = items.Count,
                items = items.Select(x => new
                {
                    id = x.Id,
                    guid = x.GuidValue,
                    createdAt = x.CreatedAt,
                    notes = x.Notes
                })
            },
            meta: new
            {
                operation = "list_guids",
                requestedCount = count,
                returnedCount = items.Count,
                cappedCount = boundedCount,
                wasCapped = count > MaxListCount,
                searchMode = normalizedSearchMode
            });
    }

    [McpServerTool]
    [Description("Updates notes for a saved GUID record.")]
    public async Task<object> UpdateGuidNotes(
        [Description("The saved GUID record ID.")] int id,
        [Description("New notes value.")] string? notes = null,
        [Description("When true, clears notes instead of setting a new value.")] bool clearNotes = false,
        CancellationToken cancellationToken = default)
    {
        if (!clearNotes && string.IsNullOrWhiteSpace(notes))
        {
            return ErrorResponse("Provide notes or set clear_notes=true.");
        }

        if (clearNotes && !string.IsNullOrWhiteSpace(notes))
        {
            return ErrorResponse("Use only one mode: notes or clear_notes=true.");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGuidRepository>();

        var updatedNotes = clearNotes ? null : notes?.Trim();
        var updated = await repository.UpdateGuidNotesAsync(id, updatedNotes, cancellationToken);

        if (!updated)
        {
            return ErrorResponse($"GUID record not found: {id}");
        }

        var existing = await repository.GetGuidByIdAsync(id, cancellationToken);

        return SuccessResponse(
            data: new
            {
                id,
                guid = existing?.GuidValue,
                notes = updatedNotes
            },
            meta: new
            {
                operation = "update_guid_notes"
            });
    }

    [McpServerTool]
    [Description("Deletes a saved GUID by matching GUID value and/or notes prefix.")]
    public async Task<object> DeleteGuid(
        [Description("GUID value prefix to match.")] string? value = null,
        [Description("Notes prefix to match.")] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        var hasValue = !string.IsNullOrWhiteSpace(value);
        var hasNotes = !string.IsNullOrWhiteSpace(notes);

        if (!hasValue && !hasNotes)
        {
            return ErrorResponse("At least one of value or notes must be provided.");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IGuidRepository>();

        var matches = await repository.GetGuidByValueAndNotes(
            hasValue ? value : null,
            hasNotes ? notes : null,
            cancellationToken,
            maxResults: 4);

        if (matches.Count == 0)
        {
            return ErrorResponse("No GUIDs found matching the specified criteria.");
        }

        if (matches.Count > 1)
        {
            return ErrorResponse(
                "Multiple GUIDs found; provide a more specific value.",
                data: new
                {
                    sample = matches.Take(3).Select(x => new
                    {
                        id = x.Id,
                        guid = x.GuidValue,
                        notes = x.Notes
                    })
                },
                meta: new
                {
                    operation = "delete_guid",
                    returnedMatchCount = matches.Count,
                    hasMoreMatches = matches.Count > 3
                });
        }

        var target = matches[0];
        var deleted = await repository.DeleteGuidAsync(target.Id, cancellationToken);

        if (!deleted)
        {
            return ErrorResponse("Delete operation failed.");
        }

        return SuccessResponse(
            data: new
            {
                id = target.Id,
                guid = target.GuidValue,
                notes = target.Notes
            },
            meta: new
            {
                operation = "delete_guid"
            });
    }
}
