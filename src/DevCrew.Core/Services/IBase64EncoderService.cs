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
}

/// <summary>
/// Service for Base64 encoding operations.
/// </summary>
public interface IBase64EncoderService
{
    /// <summary>
    /// Encodes a byte array to Base64 string.
    /// </summary>
    /// <param name="input">Binary input data.</param>
    /// <returns>Encoding result with output or error.</returns>
    Base64EncodeResult Encode(byte[] input);
}