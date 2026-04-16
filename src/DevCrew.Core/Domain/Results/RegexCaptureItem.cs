namespace DevCrew.Core.Domain.Results;

public sealed record RegexCaptureItem
{
    public string Name { get; init; } = string.Empty;

    public string Value { get; init; } = string.Empty;

    public int Index { get; init; }

    public int Length { get; init; }

    public bool IsNamed { get; init; }
}
