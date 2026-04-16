namespace DevCrew.Core.Application.Services;

using DevCrew.Core.Domain.Results;

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
    Base64DecodeResult Decode(string? input);
}
