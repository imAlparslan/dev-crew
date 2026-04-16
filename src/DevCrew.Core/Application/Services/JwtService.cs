using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using DevCrew.Core.Domain.Models;
using DevCrew.Core.Shared.Constants;
using Microsoft.IdentityModel.Tokens;

namespace DevCrew.Core.Application.Services;

public class JwtService : IJwtService
{
    private readonly JwtSecurityTokenHandler _tokenHandler;

    /// <summary>
    /// Default secret keys for development/testing ONLY.
    /// CRITICAL: These are example keys for demonstration purposes.
    /// In production, generate unique keys and store them securely.
    /// NEVER use these keys in production environments.
    /// </summary>
    private const string AlgorithmHs256 = "HS256";

    private static readonly Dictionary<string, string> DefaultSecretKeys = new()
    {
        { AlgorithmHs256, "a-string-secret-at-least-256-bits-long" },
        { "HS384", "a-valid-string-secret-that-is-at-least-384-bits-long" },
        { "HS512", "a-valid-string-secret-that-is-at-least-512-bits-long-which-is-very-long" }
    };

    public JwtService()
    {
        _tokenHandler = new JwtSecurityTokenHandler();
    }
    public JwtDecodeResult DecodeToken(string? token)
    {
        var result = new JwtDecodeResult();

        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                result.ErrorMessage = "Token cannot be empty";
                result.ErrorKey = ErrorKeys.Jwt.TokenEmpty;
                return result;
            }

            token = RemoveBearerPrefix(token);

            if (!_tokenHandler.CanReadToken(token))
            {
                result.ErrorMessage = "Invalid JWT token format";
                result.ErrorKey = ErrorKeys.Jwt.TokenFormatInvalid;
                return result;
            }

            var jwtToken = _tokenHandler.ReadJwtToken(token);

            // Extract algorithm from header
            result.Algorithm = jwtToken.Header.Alg;

            // Format header as JSON
            var headerJson = JsonNodeSerializer.Serialize(
                jwtToken.Header.ToDictionary(item => item.Key, item => item.Value),
                writeIndented: true);
            result.Header = headerJson;

            // Format payload as JSON - handle duplicate claims
            var payloadDict = new Dictionary<string, object?>();
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

            var payloadJson = JsonNodeSerializer.Serialize(payloadDict, writeIndented: true);
            result.Payload = payloadJson;
            // Extract common claims - check both properties and claims
            // Expiration time
            var expClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.Expiration);
            if (expClaim != null && long.TryParse(expClaim.Value, out long expTimestamp))
            {
                result.ExpiresAt = DateTimeOffset.FromUnixTimeSeconds(expTimestamp).UtcDateTime;
            }
            else if (jwtToken.ValidTo != DateTime.MinValue)
            {
                result.ExpiresAt = jwtToken.ValidTo;
            }

            // Issued at
            var iatClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.IssuedAt);
            if (iatClaim != null && long.TryParse(iatClaim.Value, out long iatTimestamp))
            {
                result.IssuedAt = DateTimeOffset.FromUnixTimeSeconds(iatTimestamp).UtcDateTime;
            }
            else if (jwtToken.ValidFrom != DateTime.MinValue)
            {
                result.IssuedAt = jwtToken.ValidFrom;
            }

            // Not before
            var nbfClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == JwtClaimTypes.NotBefore);
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
        }
        catch (Exception ex)
        {
            result.ErrorMessage = $"Error decoding token: {ex.Message}";
            result.ErrorKey = ErrorKeys.Jwt.DecodeFailed;
            result.ErrorArgs = [ex.Message];
            result.IsValid = false;
        }

        return result;
    }

    public bool ValidateTokenSignature(string token, string secret)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(token) || string.IsNullOrWhiteSpace(secret))
            {
                return false;
            }

            token = RemoveBearerPrefix(token);

            if (!_tokenHandler.CanReadToken(token))
            {
                return false;
            }

            var jwtToken = _tokenHandler.ReadJwtToken(token);
            var algorithm = jwtToken.Header.Alg?.ToUpperInvariant();

            SecurityKey securityKey;

            // Determine key type based on algorithm
            if (algorithm?.StartsWith("RS") == true)
            {
                // RSA algorithms - parse public key
                try
                {
                    var rsa = RSA.Create();

                    // Try to import as PEM format first
                    if (secret.Contains("BEGIN PUBLIC KEY") || secret.Contains("BEGIN RSA PUBLIC KEY"))
                    {
                        rsa.ImportFromPem(secret);
                    }
                    else
                    {
                        // Try to import as XML format
                        rsa.FromXmlString(secret);
                    }

                    securityKey = new RsaSecurityKey(rsa);
                }
                catch
                {
                    return false;
                }
            }
            else
            {
                // HMAC algorithms - use symmetric key
                securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secret));
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuerSigningKey = true,
                ValidateIssuer = false,
                ValidateAudience = false,
                ValidateLifetime = false, // Don't validate expiration for signature check
                IssuerSigningKey = securityKey
            };

            _tokenHandler.ValidateToken(token, validationParameters, out _);
            return true;
        }
        catch (SecurityTokenSignatureKeyNotFoundException)
        {
            return false;
        }
        catch (SecurityTokenInvalidSignatureException)
        {
            return false;
        }
        catch (Exception)
        {
            return false;
        }
    }

    /// <summary>
    /// Removes "Bearer " prefix from JWT token if present.
    /// Safely removes only the prefix at the beginning of the token.
    /// </summary>
    /// <param name="token">The token with possible "Bearer " prefix</param>
    /// <returns>Token without the prefix</returns>
    private static string RemoveBearerPrefix(string token)
    {
        const string bearerPrefix = "Bearer ";

        if (token.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return token[bearerPrefix.Length..].Trim();
        }

        return token.Trim();
    }

    public (bool Success, string? Token, string? ErrorMessage, string? ErrorKey, object[]? ErrorArgs) BuildToken(
        Dictionary<string, object> claims,
        string secret,
        string algorithm = AlgorithmHs256,
        DateTime? expiresAt = null,
        string? issuer = null,
        string? audience = null,
        string? subject = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                return (false, null, "Secret key cannot be empty", ErrorKeys.Jwt.BuildSecretRequired, null);
            }

            // Claims dictionary can be empty - we'll add standard claims below
            if (claims == null)
            {
                claims = new Dictionary<string, object>();
            }

            // Normalize algorithm name
            algorithm = algorithm.ToUpperInvariant();

            // Validate algorithm
            var supportedAlgorithms = new[] { AlgorithmHs256, "HS384", "HS512", "RS256", "RS384", "RS512" };
            if (!supportedAlgorithms.Contains(algorithm))
            {
                var supported = string.Join(", ", supportedAlgorithms);
                return (false, null, $"Unsupported algorithm: {algorithm}. Supported: {supported}", ErrorKeys.Jwt.BuildUnsupportedAlgorithm, [algorithm, supported]);
            }

            // Create security key based on algorithm
            SecurityKey securityKey;
            SigningCredentials signingCredentials;

            if (algorithm.StartsWith("HS"))
            {
                // HMAC algorithms - use symmetric key
                var keyBytes = Encoding.UTF8.GetBytes(secret);
                securityKey = new SymmetricSecurityKey(keyBytes);

                var algorithmName = algorithm switch
                {
                    AlgorithmHs256 => SecurityAlgorithms.HmacSha256,
                    "HS384" => SecurityAlgorithms.HmacSha384,
                    "HS512" => SecurityAlgorithms.HmacSha512,
                    _ => SecurityAlgorithms.HmacSha256
                };

                signingCredentials = new SigningCredentials(securityKey, algorithmName);
            }
            else // RS256, RS384, RS512
            {
                // RSA algorithms - parse private key
                try
                {
                    var rsa = RSA.Create();

                    // Try to import as PEM format first
                    if (secret.Contains("BEGIN RSA PRIVATE KEY") || secret.Contains("BEGIN PRIVATE KEY"))
                    {
                        rsa.ImportFromPem(secret);
                    }
                    else
                    {
                        // Try to import as XML format
                        rsa.FromXmlString(secret);
                    }

                    securityKey = new RsaSecurityKey(rsa);

                    var algorithmName = algorithm switch
                    {
                        "RS256" => SecurityAlgorithms.RsaSha256,
                        "RS384" => SecurityAlgorithms.RsaSha384,
                        "RS512" => SecurityAlgorithms.RsaSha512,
                        _ => SecurityAlgorithms.RsaSha256
                    };

                    signingCredentials = new SigningCredentials(securityKey, algorithmName);
                }
                catch (Exception ex)
                {
                    return (false, null, $"Invalid RSA private key format: {ex.Message}", ErrorKeys.Jwt.BuildInvalidRsaPrivateKey, [ex.Message]);
                }
            }

            // Build claims list
            var claimsList = new List<Claim>();

            // Add custom claims
            foreach (var claim in claims)
            {
                if (claim.Value is string[] arrayValue)
                {
                    // Create a separate Claim for each value if the same key has multiple values
                    foreach (var value in arrayValue)
                    {
                        claimsList.Add(new Claim(claim.Key, value ?? string.Empty));
                    }
                }
                else
                {
                    // Single value
                    var claimValue = claim.Value?.ToString() ?? string.Empty;
                    claimsList.Add(new Claim(claim.Key, claimValue));
                }
            }

            // Add subject if provided
            if (!string.IsNullOrWhiteSpace(subject))
            {
                claimsList.Add(new Claim(JwtRegisteredClaimNames.Sub, subject));
            }

            // Create token descriptor
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claimsList),
                SigningCredentials = signingCredentials
            };

            // Set issuer if provided
            if (!string.IsNullOrWhiteSpace(issuer))
            {
                tokenDescriptor.Issuer = issuer;
            }

            // Set audience if provided
            if (!string.IsNullOrWhiteSpace(audience))
            {
                tokenDescriptor.Audience = audience;
            }

            // Set expiration if provided
            if (expiresAt.HasValue)
            {
                tokenDescriptor.Expires = expiresAt.Value;
            }

            // Set issued at time
            tokenDescriptor.IssuedAt = DateTime.UtcNow;
            tokenDescriptor.NotBefore = DateTime.UtcNow;

            // Create token
            var token = _tokenHandler.CreateToken(tokenDescriptor);
            var tokenString = _tokenHandler.WriteToken(token);

            return (true, tokenString, null, null, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Error building token: {ex.Message}", ErrorKeys.Jwt.BuildFailed, [ex.Message]);
        }
    }

    public string GetDefaultSecretKey(string algorithm)
    {
        if (DefaultSecretKeys.TryGetValue(algorithm, out var secretKey))
        {
            return secretKey;
        }

        return string.Empty;
    }
}
