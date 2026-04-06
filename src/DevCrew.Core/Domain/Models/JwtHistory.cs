using System.ComponentModel.DataAnnotations;

namespace DevCrew.Core.Domain.Models;

public class JwtHistory
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Token { get; set; } = string.Empty;

    public DateTime DecodedAt { get; set; }

    public string? Notes { get; set; }

    public string? Header { get; set; }
    public string? Payload { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public string? Issuer { get; set; }
    public string? Audience { get; set; }
}
