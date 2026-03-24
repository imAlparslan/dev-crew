namespace DevCrew.Core.Models.Results;

public record JsonDiffResult
{
    public bool IsValid { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ErrorKey { get; init; }

    public object[]? ErrorArgs { get; init; }

    public JsonDiffSummary Summary { get; init; } = new();

    public IReadOnlyList<JsonPathDiffEntry> PathDiffs { get; init; } = [];

    public IReadOnlyList<JsonLineDiffEntry> LineDiffs { get; init; } = [];

    public string LeftDisplayJson { get; init; } = string.Empty;

    public string RightDisplayJson { get; init; } = string.Empty;
}
