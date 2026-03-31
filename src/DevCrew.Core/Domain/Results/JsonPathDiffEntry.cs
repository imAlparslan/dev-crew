namespace DevCrew.Core.Domain.Results;

using DevCrew.Core.Shared.Enums;

public record JsonPathDiffEntry
{
    public string Path { get; init; } = string.Empty;

    public JsonDiffKind Kind { get; init; }

    public string? LeftValue { get; init; }

    public string? RightValue { get; init; }
}
