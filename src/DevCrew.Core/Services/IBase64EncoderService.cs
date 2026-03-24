namespace DevCrew.Core.Services;

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

/// <summary>
/// Service for Base64 encoding and decoding operations.
/// </summary>
public interface IBase64EncoderService
{
    /// <summary>
    /// Encodes a byte array to Base64 string.
    /// </summary>
    /// <param name="input">Binary input data.</param>
    /// <returns>Encoding result with output or error.</returns>
    Base64EncodeResult Encode(byte[] input);

    /// <summary>
    /// Decodes a Base64 string back to its original binary form.
    /// </summary>
    /// <param name="input">Base64 encoded string.</param>
    /// <returns>Decoding result with binary output or error.</returns>
    Base64DecodeResult Decode(string input);
}