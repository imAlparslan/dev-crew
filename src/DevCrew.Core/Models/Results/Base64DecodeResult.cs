namespace DevCrew.Core.Models.Results;

/// <summary>
/// Result of Base64 decoding operation.
/// </summary>
public record Base64DecodeResult
{
    /// <summary>
    /// Indicates whether operation completed successfully.
    /// </summary>
    public bool IsSuccess { get; init; }

    /// <summary>
    /// Decoded binary output.
    /// </summary>
    public byte[]? Output { get; init; }

    /// <summary>
    /// Error message when decoding fails.
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
