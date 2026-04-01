namespace DevCrew.Core.Domain.Results;

public sealed record RegexMatchResult
{
    public bool IsValid { get; init; }

    public string Pattern { get; init; } = string.Empty;

    public bool IgnoreCase { get; init; }

    public bool Multiline { get; init; }

    public int InputLength { get; init; }

    public string? ErrorMessage { get; init; }

    public string? ErrorKey { get; init; }

    public object[]? ErrorArgs { get; init; }

    public IReadOnlyList<RegexMatchItem> Matches { get; init; } = [];

    public int MatchCount => Matches.Count;
}