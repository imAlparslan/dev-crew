using System.Text.Json;

namespace DevCrew.Core.Services;

public enum JsonDiffKind
{
    Added,
    Removed,
    Changed,
    Unchanged
}

public record JsonDiffOptions
{
    public bool IgnoreObjectPropertyOrder { get; init; } = true;

    public bool TreatArrayOrderAsSignificant { get; init; } = true;

    public bool IgnoreWhitespaceDifferences { get; init; } = true;

    public bool TreatNullAndEmptyStringAsEqual { get; init; }
}

public record JsonPathDiffEntry
{
    public string Path { get; init; } = string.Empty;

    public JsonDiffKind Kind { get; init; }

    public string? LeftValue { get; init; }

    public string? RightValue { get; init; }
}

public record JsonLineDiffEntry
{
    public JsonDiffKind Kind { get; init; }

    public int? LeftLineNumber { get; init; }

    public int? RightLineNumber { get; init; }

    public string? LeftLine { get; init; }

    public string? RightLine { get; init; }
}

public record JsonDiffSummary
{
    public int AddedCount { get; init; }

    public int RemovedCount { get; init; }

    public int ChangedCount { get; init; }

    public int UnchangedCount { get; init; }

    public int TotalDifferences => AddedCount + RemovedCount + ChangedCount;
}

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

public interface IJsonDiffService
{
    JsonDiffResult Compare(string leftJson, string rightJson, JsonDiffOptions? options = null);
}
