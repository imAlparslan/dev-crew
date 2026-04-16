namespace DevCrew.Core.Infrastructure.Persistence.Repositories;

/// <summary>
/// Request payload for persisting a decoded JWT history entry.
/// </summary>
/// <param name="Token">The JWT token string</param>
/// <param name="Header">Decoded header JSON</param>
/// <param name="Payload">Decoded payload JSON</param>
/// <param name="ExpiresAt">Token expiration time</param>
/// <param name="Issuer">Token issuer</param>
/// <param name="Audience">Token audience</param>
/// <param name="Notes">Optional notes</param>
public sealed record SaveJwtRequest(
    string Token,
    string? Header = null,
    string? Payload = null,
    DateTime? ExpiresAt = null,
    string? Issuer = null,
    string? Audience = null,
    string? Notes = null);
