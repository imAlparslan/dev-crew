using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Domain.Models;
using Shouldly;
using Xunit;

namespace DevCrew.Core.Tests.Services;

public sealed class JwtServiceTests
{
    private readonly JwtService _service = new();

    #region DecodeToken Tests

    [Fact]
    public void DecodeToken_ExtractAllClaims_WhenTokenIsValid()
    {
        // Arrange
        var claims = new Dictionary<string, object> { { "sub", "user123" }, { "email", "user@example.com" } };
        var secret = _service.GetDefaultSecretKey("HS256");
        var (success, token, _, _, _) = _service.BuildToken(claims, secret, "HS256", DateTime.UtcNow.AddHours(1));
        success.ShouldBeTrue();

        // Act
        var result = _service.DecodeToken(token);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Payload.ShouldContain("user123");
        result.Payload.ShouldContain("user@example.com");
        result.Subject.ShouldBe("user123");
    }

    [Fact]
    public void DecodeToken_IdentifyExpiredToken_WhenTokenExpired()
    {
        // Arrange - Create token that will expire soon (1 second)
        var claims = new Dictionary<string, object> { { "sub", "user123" } };
        var secret = _service.GetDefaultSecretKey("HS256");
        var expiresAt = DateTime.UtcNow.AddSeconds(1);
        var (success, token, _, _, _) = _service.BuildToken(claims, secret, "HS256", expiresAt);
        success.ShouldBeTrue();

        // Wait for token to expire
        System.Threading.Thread.Sleep(1100);

        // Act
        var result = _service.DecodeToken(token);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.ExpiresAt.ShouldNotBeNull();
        result.ExpiresAt.Value.ShouldBeLessThan(DateTime.UtcNow);
    }

    [Fact]
    public void DecodeToken_RetainAlgorithmInfo_WhenTokenUsesHS256()
    {
        // Arrange
        var claims = new Dictionary<string, object> { { "data", "test" } };
        var secret = _service.GetDefaultSecretKey("HS256");
        var (success, token, _, _, _) = _service.BuildToken(claims, secret, "HS256", DateTime.UtcNow.AddHours(1));
        success.ShouldBeTrue();

        // Act
        var result = _service.DecodeToken(token);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Algorithm.ShouldBe("HS256");
    }

    [Fact]
    public void DecodeToken_RetainAlgorithmInfo_WhenTokenUsesHS512()
    {
        // Arrange
        var claims = new Dictionary<string, object> { { "data", "test" } };
        var secret = _service.GetDefaultSecretKey("HS512");
        var (success, token, _, _, _) = _service.BuildToken(claims, secret, "HS512", DateTime.UtcNow.AddHours(1));
        success.ShouldBeTrue();

        // Act
        var result = _service.DecodeToken(token);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Algorithm.ShouldBe("HS512");
    }

    [Fact]
    public void DecodeToken_ThrowException_WhenTokenIsInvalidFormat()
    {
        // Arrange
        var invalidToken = "this.is.not.valid";

        // Act
        var result = _service.DecodeToken(invalidToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorKey.ShouldBe("jwt.token_format_invalid");
    }

    [Fact]
    public void DecodeToken_ReturnFalse_WhenTokenIsEmpty()
    {
        // Arrange
        var emptyToken = string.Empty;

        // Act
        var result = _service.DecodeToken(emptyToken);

        // Assert
        result.IsValid.ShouldBeFalse();
        result.ErrorKey.ShouldBe("jwt.token_empty");
    }

    [Fact]
    public void DecodeToken_RemoveBearerPrefix_WhenTokenHasBearer()
    {
        // Arrange
        var claims = new Dictionary<string, object> { { "sub", "user123" } };
        var secret = _service.GetDefaultSecretKey("HS256");
        var (success, token, _, _, _) = _service.BuildToken(claims, secret, "HS256", DateTime.UtcNow.AddHours(1));
        success.ShouldBeTrue();
        var bearerToken = $"Bearer {token}";

        // Act
        var result = _service.DecodeToken(bearerToken);

        // Assert
        result.IsValid.ShouldBeTrue();
        result.Subject.ShouldBe("user123");
    }

    #endregion

    #region BuildToken Tests

    [Fact]
    public void BuildToken_CreateValidJwt_WhenAllClaimsProvided()
    {
        // Arrange
        var claims = new Dictionary<string, object>
        {
            { "sub", "user456" },
            { "email", "user@test.com" },
            { "role", "admin" }
        };
        var secret = _service.GetDefaultSecretKey("HS256");

        // Act
        var (success, token, errorMessage, errorKey, _) = _service.BuildToken(claims, secret, "HS256", DateTime.UtcNow.AddHours(1));

        // Assert
        success.ShouldBeTrue();
        token.ShouldNotBeNullOrWhiteSpace();
        errorMessage.ShouldBeNull();
        errorKey.ShouldBeNull();
        
        // Verify token can be decoded
        var decoded = _service.DecodeToken(token);
        decoded.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void BuildToken_ApplyExpiration_WhenExpirationMinutesSet()
    {
        // Arrange
        var claims = new Dictionary<string, object> { { "sub", "user" } };
        var secret = _service.GetDefaultSecretKey("HS256");
        var expiresAt = DateTime.UtcNow.AddMinutes(30);

        // Act
        var (success, token, _, _, _) = _service.BuildToken(claims, secret, "HS256", expiresAt);

        // Assert
        success.ShouldBeTrue();
        var decoded = _service.DecodeToken(token);
        decoded.IsValid.ShouldBeTrue();
        decoded.ExpiresAt.ShouldNotBeNull();
        // Should expire within 31 minutes
        (decoded.ExpiresAt.Value - DateTime.UtcNow).TotalHours.ShouldBeLessThan(1);
    }

    [Fact]
    public void BuildToken_UseCorrectAlgorithm_WhenHS256Selected()
    {
        // Arrange
        var claims = new Dictionary<string, object> { { "data", "test" } };
        var secret = _service.GetDefaultSecretKey("HS256");

        // Act
        var (success, token, _, _, _) = _service.BuildToken(claims, secret, "HS256");

        // Assert
        success.ShouldBeTrue();
        var decoded = _service.DecodeToken(token);
        decoded.Algorithm.ShouldBe("HS256");
    }

    [Fact]
    public void BuildToken_UseCorrectAlgorithm_WhenHS512Selected()
    {
        // Arrange
        var claims = new Dictionary<string, object> { { "data", "test" } };
        var secret = _service.GetDefaultSecretKey("HS512");

        // Act
        var (success, token, _, _, _) = _service.BuildToken(claims, secret, "HS512");

        // Assert
        success.ShouldBeTrue();
        var decoded = _service.DecodeToken(token);
        decoded.Algorithm.ShouldBe("HS512");
    }

    [Fact]
    public void BuildToken_ReturnFalse_WhenSecretIsEmpty()
    {
        // Arrange
        var claims = new Dictionary<string, object> { { "sub", "user" } };
        var emptySecret = string.Empty;

        // Act
        var (success, token, errorMessage, errorKey, _) = _service.BuildToken(claims, emptySecret);

        // Assert
        success.ShouldBeFalse();
        token.ShouldBeNull();
        errorKey.ShouldBe("jwt.build_secret_required");
    }

    [Fact]
    public void BuildToken_ReturnFalse_WhenUnsupportedAlgorithm()
    {
        // Arrange
        var claims = new Dictionary<string, object> { { "sub", "user" } };
        var secret = _service.GetDefaultSecretKey("HS256");
        var unsupportedAlgorithm = "INVALID";

        // Act
        var (success, token, errorMessage, errorKey, _) = _service.BuildToken(claims, secret, unsupportedAlgorithm);

        // Assert
        success.ShouldBeFalse();
        token.ShouldBeNull();
        errorKey.ShouldBe("jwt.build_unsupported_algorithm");
    }

    [Fact]
    public void BuildToken_IncludeIssuer_WhenIssuerProvided()
    {
        // Arrange
        var claims = new Dictionary<string, object> { { "sub", "user" } };
        var secret = _service.GetDefaultSecretKey("HS256");
        var issuer = "https://example.com";

        // Act
        var (success, token, _, _, _) = _service.BuildToken(claims, secret, "HS256", issuer: issuer);

        // Assert
        success.ShouldBeTrue();
        var decoded = _service.DecodeToken(token);
        decoded.Issuer.ShouldBe(issuer);
    }

    [Fact]
    public void BuildToken_IncludeAudience_WhenAudienceProvided()
    {
        // Arrange
        var claims = new Dictionary<string, object> { { "sub", "user" } };
        var secret = _service.GetDefaultSecretKey("HS256");
        var audience = "api-client";

        // Act
        var (success, token, _, _, _) = _service.BuildToken(claims, secret, "HS256", audience: audience);

        // Assert
        success.ShouldBeTrue();
        var decoded = _service.DecodeToken(token);
        decoded.Audience.ShouldBe(audience);
    }

    #endregion

    #region GetDefaultSecretKey Tests

    [Fact]
    public void GetDefaultSecretKey_ReturnKeyForAlgorithm_WhenHS256Requested()
    {
        // Act
        var key = _service.GetDefaultSecretKey("HS256");

        // Assert
        key.ShouldNotBeNullOrWhiteSpace();
        key.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetDefaultSecretKey_ReturnKeyForAlgorithm_WhenHS512Requested()
    {
        // Act
        var key = _service.GetDefaultSecretKey("HS512");

        // Assert
        key.ShouldNotBeNullOrWhiteSpace();
        key.Length.ShouldBeGreaterThan(0);
    }

    [Fact]
    public void GetDefaultSecretKey_ReturnEmptyString_WhenAlgorithmIsInvalid()
    {
        // Act
        var key = _service.GetDefaultSecretKey("INVALID");

        // Assert
        key.ShouldBe(string.Empty);
    }

    #endregion

    #region Roundtrip Tests

    [Fact]
    public void BuildTokenAndDecodeToken_RoundTrip_WhenUsingValidClaims()
    {
        // Arrange
        var originalClaims = new Dictionary<string, object>
        {
            { "sub", "user999" },
            { "email", "roundtrip@test.com" },
            { "role", "user" }
        };
        var secret = _service.GetDefaultSecretKey("HS256");
        var expiresAt = DateTime.UtcNow.AddHours(2);

        // Act
        var (buildSuccess, token, _, _, _) = _service.BuildToken(originalClaims, secret, "HS256", expiresAt);
        var decodeResult = _service.DecodeToken(token);

        // Assert
        buildSuccess.ShouldBeTrue();
        decodeResult.IsValid.ShouldBeTrue();
        decodeResult.Subject.ShouldBe("user999");
        decodeResult.Payload.ShouldContain("roundtrip@test.com");
    }

    #endregion
}
