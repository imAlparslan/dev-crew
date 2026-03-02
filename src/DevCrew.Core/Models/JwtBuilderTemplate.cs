namespace DevCrew.Core.Models;

/// <summary>
/// Represents a saved JWT Builder configuration template
/// </summary>
public class JwtBuilderTemplate
{
    /// <summary>
    /// Gets or sets the template ID
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the template name
    /// </summary>
    public required string TemplateName { get; set; }

    /// <summary>
    /// Gets or sets the JWT signing algorithm (e.g., HS256, RS256)
    /// </summary>
    public required string Algorithm { get; set; }

    /// <summary>
    /// Gets or sets the secret key (for HMAC) or private key (for RSA)
    /// </summary>
    public required string Secret { get; set; }

    /// <summary>
    /// Gets or sets the public key (for RSA algorithms only)
    /// </summary>
    public string? PublicKey { get; set; }

    /// <summary>
    /// Gets or sets the token issuer (iss claim)
    /// </summary>
    public string? Issuer { get; set; }

    /// <summary>
    /// Gets or sets the token audience (aud claim)
    /// </summary>
    public string? Audience { get; set; }

    /// <summary>
    /// Gets or sets the token subject (sub claim)
    /// </summary>
    public string? Subject { get; set; }

    /// <summary>
    /// Gets or sets the expiration duration in minutes
    /// </summary>
    public int ExpirationMinutes { get; set; }

    /// <summary>
    /// Gets or sets whether to include expiration claim in token
    /// </summary>
    public bool IncludeExpiration { get; set; }

    /// <summary>
    /// Gets or sets the custom claims as JSON string
    /// </summary>
    public string? CustomClaimsJson { get; set; }

    /// <summary>
    /// Gets or sets additional notes or description
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the template was created
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the timestamp when the template was last used
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
}
