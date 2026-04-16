namespace DevCrew.Core.Domain.Results;

public sealed record RegexMatchItem
{
    public int Index { get; init; }

    public int Length { get; init; }

    public string Value { get; init; } = string.Empty;

    public IReadOnlyList<RegexCaptureItem> Captures { get; init; } = [];
}
