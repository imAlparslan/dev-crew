using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using DevCrew.Core.Models;
using Microsoft.IdentityModel.Tokens;

namespace DevCrew.Core.Services;

public class JwtService : IJwtService
{
    private readonly JwtSecurityTokenHandler _tokenHandler;

    // Default secret keys for each algorithm
    private static readonly Dictionary<string, string> DefaultSecretKeys = new()
    {
        { "HS256", "a-string-secret-at-least-256-bits-long" },
        { "HS384", "a-valid-string-secret-that-is-at-least-384-bits-long" },
        { "HS512", "a-valid-string-secret-that-is-at-least-512-bits-long-which-is-very-long" },
        { "RS256", "-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJTUt9Us8cKjMzEfYyjiWA4R4/M2bS1GB4t7NXp98C3SC6dVMvDuictGeurT8jNbvJZHtCSuYEvuNMoSfm76oqFvAp8Gy0iz5sxjZmSnXyCdPEovGhLa0VzMaQ8s+CLOyS56YyCFGeJZqgtzJ6GR3eqoYSW9b9UMvkBpZODSctWSNGj3P7jRFDO5VoTwCQAWbFnOjDfH5Ulhp2PKSQnSJP3AJLQNFNe7br1XbrhV//eO+t51mIpGSDCUv3E0DDFcWDTH9cXDTTlRZVEiR2BwpZOOkE/Z0/BVnhZYL71oZV34bKfWjQIt6V/isSMahdsAASDqp4ZTGN0iwBfrgIDAQABAoIBABiCKvJun7lyPJ8jMuiKqLBXAObMhd0N8gH0/Ny/OBPGxXiPqRMbyNz/LglLKZz1sLYEW0tVMTp8pP+WylVkP6KBw9N7VOhfXe3C8SplvNjxE6xARQHK2VT1j1J0pVmP0hGnpZT4GV9bEz+0DShEQzKhMqfWKZxXBkJxB8c0i7vJDXW/tA7OBgxXjY7cJFkLZ3hPFXNeFJLRgjOJBSNZExPsPVPLdQmGFpKPQQ7LBz5VX6aQWdJLBQJJN7bFCb5AKLBFBm3GQqGJ7r+B9nRnQuqYJB9YWRgLMPGpPVQKZZQm3k/Sg9jwKB0TRCBCPqS5Ac1AoEFNJqTGMtLR4kJVwAECgYEA3fMj7X1MZqgLhENqY7rCqhA9xQQNiJ7C2MXqVZNYhSkPKt9R8LoX/b7PqEKXYxELsPQELBJRKSf0n4k2GfOdU/wEuEz8LNSdN8v7lPKLRJ0lZVJSE9r7qxD1VKAM8qp1JWhKPTHQqPCAEZ5yLYuFZEj5gTKz9plPgqcqBqCqWAECgYEA2HKGNQMhKMVJgPKPLBb1J0FRBqBWXDO6jTHqFRfGC0LQKqW4qMj9QqPYqQGE7V4Y5qBVK7J9PQMX/nFXqGkCVN8vpJQqz/7J1R7hM9lJ7XqMx0gJP7A3QkP1bJXpF7j3aXQ9BWz0k/lKMqF7N0lV7N4WQJ8XlB/7Z0YqzqT71wIDAQAB\n-----END PRIVATE KEY-----" },
        { "RS384", "-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJTUt9Us8cKjMzEfYyjiWA4R4/M2bS1GB4t7NXp98C3SC6dVMvDuictGeurT8jNbvJZHtCSuYEvuNMoSfm76oqFvAp8Gy0iz5sxjZmSnXyCdPEovGhLa0VzMaQ8s+CLOyS56YyCFGeJZqgtzJ6GR3eqoYSW9b9UMvkBpZODSctWSNGj3P7jRFDO5VoTwCQAWbFnOjDfH5Ulhp2PKSQnSJP3AJLQNFNe7br1XbrhV//eO+t51mIpGSDCUv3E0DDFcWDTH9cXDTTlRZVEiR2BwpZOOkE/Z0/BVnhZYL71oZV34bKfWjQIt6V/isSMahdsAASDqp4ZTGN0iwBfrgIDAQABAoIBABiCKvJun7lyPJ8jMuiKqLBXAObMhd0N8gH0/Ny/OBPGxXiPqRMbyNz/LglLKZz1sLYEW0tVMTp8pP+WylVkP6KBw9N7VOhfXe3C8SplvNjxE6xARQHK2VT1j1J0pVmP0hGnpZT4GV9bEz+0DShEQzKhMqfWKZxXBkJxB8c0i7vJDXW/tA7OBgxXjY7cJFkLZ3hPFXNeFJLRgjOJBSNZExPsPVPLdQmGFpKPQQ7LBz5VX6aQWdJLBQJJN7bFCb5AKLBFBm3GQqGJ7r+B9nRnQuqYJB9YWRgLMPGpPVQKZZQm3k/Sg9jwKB0TRCBCPqS5Ac1AoEFNJqTGMtLR4kJVwAECgYEA3fMj7X1MZqgLhENqY7rCqhA9xQQNiJ7C2MXqVZNYhSkPKt9R8LoX/b7PqEKXYxELsPQELBJRKSf0n4k2GfOdU/wEuEz8LNSdN8v7lPKLRJ0lZVJSE9r7qxD1VKAM8qp1JWhKPTHQqPCAEZ5yLYuFZEj5gTKz9plPgqcqBqCqWAECgYEA2HKGNQMhKMVJgPKPLBb1J0FRBqBWXDO6jTHqFRfGC0LQKqW4qMj9QqPYqQGE7V4Y5qBVK7J9PQMX/nFXqGkCVN8vpJQqz/7J1R7hM9lJ7XqMx0gJP7A3QkP1bJXpF7j3aXQ9BWz0k/lKMqF7N0lV7N4WQJ8XlB/7Z0YqzqT71wIDAQAB\n-----END PRIVATE KEY-----" },
        { "RS512", "-----BEGIN PRIVATE KEY-----\nMIIEvQIBADANBgkqhkiG9w0BAQEFAASCBKcwggSjAgEAAoIBAQC7VJTUt9Us8cKjMzEfYyjiWA4R4/M2bS1GB4t7NXp98C3SC6dVMvDuictGeurT8jNbvJZHtCSuYEvuNMoSfm76oqFvAp8Gy0iz5sxjZmSnXyCdPEovGhLa0VzMaQ8s+CLOyS56YyCFGeJZqgtzJ6GR3eqoYSW9b9UMvkBpZODSctWSNGj3P7jRFDO5VoTwCQAWbFnOjDfH5Ulhp2PKSQnSJP3AJLQNFNe7br1XbrhV//eO+t51mIpGSDCUv3E0DDFcWDTH9cXDTTlRZVEiR2BwpZOOkE/Z0/BVnhZYL71oZV34bKfWjQIt6V/isSMahdsAASDqp4ZTGN0iwBfrgIDAQABAoIBABiCKvJun7lyPJ8jMuiKqLBXAObMhd0N8gH0/Ny/OBPGxXiPqRMbyNz/LglLKZz1sLYEW0tVMTp8pP+WylVkP6KBw9N7VOhfXe3C8SplvNjxE6xARQHK2VT1j1J0pVmP0hGnpZT4GV9bEz+0DShEQzKhMqfWKZxXBkJxB8c0i7vJDXW/tA7OBgxXjY7cJFkLZ3hPFXNeFJLRgjOJBSNZExPsPVPLdQmGFpKPQQ7LBz5VX6aQWdJLBQJJN7bFCb5AKLBFBm3GQqGJ7r+B9nRnQuqYJB9YWRgLMPGpPVQKZZQm3k/Sg9jwKB0TRCBCPqS5Ac1AoEFNJqTGMtLR4kJVwAECgYEA3fMj7X1MZqgLhENqY7rCqhA9xQQNiJ7C2MXqVZNYhSkPKt9R8LoX/b7PqEKXYxELsPQELBJRKSf0n4k2GfOdU/wEuEz8LNSdN8v7lPKLRJ0lZVJSE9r7qxD1VKAM8qp1JWhKPTHQqPCAEZ5yLYuFZEj5gTKz9plPgqcqBqCqWAECgYEA2HKGNQMhKMVJgPKPLBb1J0FRBqBWXDO6jTHqFRfGC0LQKqW4qMj9QqPYqQGE7V4Y5qBVK7J9PQMX/nFXqGkCVN8vpJQqz/7J1R7hM9lJ7XqMx0gJP7A3QkP1bJXpF7j3aXQ9BWz0k/lKMqF7N0lV7N4WQJ8XlB/7Z0YqzqT71wIDAQAB\n-----END PRIVATE KEY-----" }
    };

    public JwtService()
    {
        _tokenHandler = new JwtSecurityTokenHandler();
    }

    public JwtDecodeResult DecodeToken(string token)
    {
        var result = new JwtDecodeResult();

        try
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                result.ErrorMessage = "Token cannot be empty";
                return result;
            }

            token = RemoveBearerPrefix(token);

            if (!_tokenHandler.CanReadToken(token))
            {
                result.ErrorMessage = "Invalid JWT token format";
                return result;
            }

            var jwtToken = _tokenHandler.ReadJwtToken(token);

            // Extract algorithm from header
            result.Algorithm = jwtToken.Header.Alg;

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
    private string RemoveBearerPrefix(string token)
    {
        const string bearerPrefix = "Bearer ";

        if (token.StartsWith(bearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return token[bearerPrefix.Length..].Trim();
        }

        return token.Trim();
    }

    public (bool Success, string? Token, string? ErrorMessage) BuildToken(
        Dictionary<string, object> claims,
        string secret,
        string algorithm = "HS256",
        DateTime? expiresAt = null,
        string? issuer = null,
        string? audience = null,
        string? subject = null)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(secret))
            {
                return (false, null, "Secret key cannot be empty");
            }

            // Claims dictionary can be empty - we'll add standard claims below
            if (claims == null)
            {
                claims = new Dictionary<string, object>();
            }

            // Normalize algorithm name
            algorithm = algorithm.ToUpperInvariant();

            // Validate algorithm
            var supportedAlgorithms = new[] { "HS256", "HS384", "HS512", "RS256", "RS384", "RS512" };
            if (!supportedAlgorithms.Contains(algorithm))
            {
                return (false, null, $"Unsupported algorithm: {algorithm}. Supported: {string.Join(", ", supportedAlgorithms)}");
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
                    "HS256" => SecurityAlgorithms.HmacSha256,
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
                    return (false, null, $"Invalid RSA private key format: {ex.Message}");
                }
            }

            // Build claims list
            var claimsList = new List<Claim>();

            // Add custom claims
            foreach (var claim in claims)
            {
                if (claim.Value is string[] arrayValue)
                {
                    // Aynı key'in birden fazla value'si varsa, her biri için ayrı Claim oluştur
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

            return (true, tokenString, null);
        }
        catch (Exception ex)
        {
            return (false, null, $"Error building token: {ex.Message}");
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
