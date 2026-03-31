namespace DevCrew.Core.Domain.Models;

public class JwtDecodeResult
{
    public bool IsValid { get; set; }
    public string? Header { get; set; }
    public string? Payload { get; set; }
    public string? Algorithm { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorKey { get; set; }
    public object[]? ErrorArgs { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public DateTime? IssuedAt { get; set; }
    public DateTime? NotBefore { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
    public string? Subject { get; set; }
}
