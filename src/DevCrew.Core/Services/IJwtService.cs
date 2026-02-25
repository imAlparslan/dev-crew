using DevCrew.Core.Models;

namespace DevCrew.Core.Services;

public interface IJwtService
{
    /// <summary>
    /// Decodes a JWT token without validation
    /// </summary>
    JwtDecodeResult DecodeToken(string token);

    /// <summary>
    /// Validates a JWT token signature with the provided secret
    /// </summary>
    bool ValidateTokenSignature(string token, string secret);

    /// <summary>
    /// Builds a JWT token with the specified claims and options
    /// </summary>
    /// <param name="claims">Dictionary of claims to include in the token</param>
    /// <param name="secret">Secret key for HMAC algorithms or private key for RSA algorithms</param>
    /// <param name="algorithm">Algorithm to use (HS256, HS384, HS512, RS256, RS384, RS512)</param>
    /// <param name="expiresAt">Token expiration time (optional)</param>
    /// <param name="issuer">Token issuer (optional)</param>
    /// <param name="audience">Token audience (optional)</param>
    /// <param name="subject">Token subject (optional)</param>
    /// <returns>Generated JWT token or error message</returns>
    (bool Success, string? Token, string? ErrorMessage) BuildToken(
        Dictionary<string, object> claims,
        string secret,
        string algorithm = "HS256",
        DateTime? expiresAt = null,
        string? issuer = null,
        string? audience = null,
        string? subject = null);

    /// <summary>
    /// Gets the default secret key for the specified algorithm
    /// </summary>
    /// <param name="algorithm">Algorithm name (HS256, HS384, HS512, RS256, RS384, RS512)</param>
    /// <returns>Default secret key for the algorithm</returns>
    string GetDefaultSecretKey(string algorithm);
}
