namespace DevCrew.Core.Services;

/// <summary>
/// Standard JWT claim type constants for type-safe access to common JWT claims.
/// </summary>
public static class JwtClaimTypes
{
    /// <summary>
    /// Expiration time claim ("exp")
    /// </summary>
    public const string Expiration = "exp";

    /// <summary>
    /// Issued at claim ("iat")
    /// </summary>
    public const string IssuedAt = "iat";

    /// <summary>
    /// Not before claim ("nbf")
    /// </summary>
    public const string NotBefore = "nbf";

    /// <summary>
    /// Issuer claim ("iss")
    /// </summary>
    public const string Issuer = "iss";

    /// <summary>
    /// Subject claim ("sub")
    /// </summary>
    public const string Subject = "sub";

    /// <summary>
    /// Audience claim ("aud")
    /// </summary>
    public const string Audience = "aud";

    /// <summary>
    /// JWT ID claim ("jti")
    /// </summary>
    public const string JwtId = "jti";
}
