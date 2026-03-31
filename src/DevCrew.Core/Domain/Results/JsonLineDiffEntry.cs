namespace DevCrew.Core.Domain.Results;

using DevCrew.Core.Shared.Enums;

public record JsonLineDiffEntry
{
    public JsonDiffKind Kind { get; init; }

    public int? LeftLineNumber { get; init; }

    public int? RightLineNumber { get; init; }

    public string? LeftLine { get; init; }

    public string? RightLine { get; init; }
}
