namespace DevCrew.Core.Models.Results;

/// <summary>
/// Result of JSON formatting operation
/// </summary>
public record JsonFormatterResult
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// The formatted JSON output
    /// </summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Language-independent error key for localization.
    /// </summary>
    public string? ErrorKey { get; init; }

    /// <summary>
    /// Optional format arguments for localized error templates.
    /// </summary>
    public object[]? ErrorArgs { get; init; }
}
