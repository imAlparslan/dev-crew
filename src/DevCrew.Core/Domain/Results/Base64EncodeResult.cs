namespace DevCrew.Core.Domain.Results;

/// <summary>
/// Result of Base64 encoding operation.
/// </summary>
public record Base64EncodeResult
{
    /// <summary>
    /// Indicates whether operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Encoded Base64 content.
    /// </summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Error message when encoding fails.
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
