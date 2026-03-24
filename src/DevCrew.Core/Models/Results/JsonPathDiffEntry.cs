namespace DevCrew.Core.Models.Results;

using DevCrew.Core.Enums;

public record JsonPathDiffEntry
{
    public string Path { get; init; } = string.Empty;

    public JsonDiffKind Kind { get; init; }

    public string? LeftValue { get; init; }

    public string? RightValue { get; init; }
}
