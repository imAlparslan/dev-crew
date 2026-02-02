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
}
