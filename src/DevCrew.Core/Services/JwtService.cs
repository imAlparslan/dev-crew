using System.IdentityModel.Tokens.Jwt;
using System.Text;
using System.Text.Json;
using DevCrew.Core.Models;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;

namespace DevCrew.Core.Services;

public class JwtService : IJwtService
{
    private readonly JwtSecurityTokenHandler _tokenHandler;
    private readonly ILogger<JwtService>? _logger;

    public JwtService(ILogger<JwtService>? logger = null)
    {
        _tokenHandler = new JwtSecurityTokenHandler();
        _logger = logger;
    }

    public JwtDecodeResult DecodeToken(string token)
    {
        _logger?.LogDebug("JWT token decode işlemi başlatıldı");
        var result = new JwtDecodeResult();

        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                result.ErrorMessage = "Token cannot be empty";
                _logger?.LogWarning("Token boş veya null");
                return result;
            }

            // Remove "Bearer " prefix if present
            token = token.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();

            if (!_tokenHandler.CanReadToken(token))
            {
                result.ErrorMessage = "Invalid JWT token format";
                _logger?.LogWarning("Geçersiz JWT token formatı");
                return result;
            }

            var jwtToken = _tokenHandler.ReadJwtToken(token);
            _logger?.LogDebug("JWT token başarıyla okundu. Issuer: {Issuer}, Algorithm: {Algorithm}", 
                jwtToken.Issuer ?? "N/A", 
                jwtToken.SignatureAlgorithm ?? "N/A");

            // Format header as JSON
            var headerJson = JsonSerializer.Serialize(jwtToken.Header, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            result.Header = headerJson;

            // Format payload as JSON - handle duplicate claims
            var payloadDict = new Dictionary<string, object>();
            var claimGroups = jwtToken.Claims.GroupBy(c => c.Type);
            
            foreach (var group in claimGroups)
            {
                var claims = group.ToList();
                if (claims.Count == 1)
                {
                    payloadDict[group.Key] = claims[0].Value;
                }
                else
                {
                    // Multiple claims with same key - store as array
                    payloadDict[group.Key] = claims.Select(c => c.Value).ToArray();
                }
            }
            
            var payloadJson = JsonSerializer.Serialize(payloadDict, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            result.Payload = payloadJson;

            // Extract common claims - check both properties and claims
            // exp (expiration time)
            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "exp");
            if (expClaim != null && long.TryParse(expClaim.Value, out long expTimestamp))
            {
                result.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expTimestamp).UtcDateTime;
            }
            else if (jwtToken.ValidTo != DateTime.MinValue)
            {
                result.ExpiresAt = jwtToken.ValidTo;
            }

            // iat (issued at)
            var iatClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "iat");
            if (iatClaim != null && long.TryParse(iatClaim.Value, out long iatTimestamp))
            {
                result.IssuedAt = DateTimeOffset.FromUnixTimeSeconds(iatTimestamp).UtcDateTime;
            }
            else if (jwtToken.ValidFrom != DateTime.MinValue)
            {
                result.IssuedAt = jwtToken.ValidFrom;
            }

            // nbf (not before)
            var nbfClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "nbf");
            if (nbfClaim != null && long.TryParse(nbfClaim.Value, out long nbfTimestamp))
            {
                result.NotBefore = DateTimeOffset.FromUnixTimeSeconds(nbfTimestamp).UtcDateTime;
            }
            else if (jwtToken.ValidFrom != DateTime.MinValue)
            {
                result.NotBefore = jwtToken.ValidFrom;
            }
            result.Issuer = jwtToken.Issuer;
            result.Audience = jwtToken.Audiences.FirstOrDefault();
            result.Subject = jwtToken.Subject;

            result.IsValid = true;
            _logger?.LogDebug("JWT token başarıyla decode edildi");
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Error decoding token: {ex.Message}";
            result.IsValid = false;
            _logger?.LogError(ex, "JWT token decode edilirken hata oluştu");
        }

        return result;
    }

    public bool ValidateTokenSignature(string token, string secret)
    {
        _logger?.LogDebug("JWT token signature validasyonu başlatıldı");
        try
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(secret))
            {
                _logger?.LogWarning("Token veya secret boş");
                return false;
            }

            // Remove "Bearer " prefix if present
            token = token.Replace("Bearer ", "", StringComparison.OrdinalIgnoreCase).Trim();

            if (!_tokenHandler.CanReadToken(token))
            {
                _logger?.LogWarning("Token okunamıyor");
                return false;
            }

            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false, // Don't validate expiration for signature check
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret))
            };

            _tokenHandler.ValidateToken(token, validationParameters, out _);
            _logger?.LogDebug("JWT signature başarıyla doğrulandı");
            return true;
        }
        catch (SecurityTokenSignatureKeyNotFoundException ex)
        {
            _logger?.LogWarning(ex, "Signature key bulunamadı");
            return false;
        }
        catch (SecurityTokenInvalidSignatureException ex)
        {
            _logger?.LogWarning(ex, "Geçersiz signature");
            return false;
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Token validasyonunda beklenmeyen hata");
            return false;
        }
    }
}
