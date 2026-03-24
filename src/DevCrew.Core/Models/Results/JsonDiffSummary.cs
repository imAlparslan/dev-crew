namespace DevCrew.Core.Models.Results;

public record JsonDiffSummary
{
    public int AddedCount { get; init; }

    public int RemovedCount { get; init; }

    public int ChangedCount { get; init; }

    public int UnchangedCount { get; init; }

    public int TotalDifferences => AddedCount + RemovedCount + ChangedCount;
}
