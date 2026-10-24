using System.ComponentModel;
using System.Text.Json;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Domain.Models;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.DependencyInjection;
using ModelContextProtocol.Server;

namespace DevCrew.Mcp.Tools;

internal sealed class JwtTools(
    IServiceScopeFactory scopeFactory,
    IJwtService jwtService)
{
    private const int MaxListCount = 100;
    private static readonly string[] SupportedAlgorithms = ["HS256", "HS384", "HS512", "RS256", "RS384", "RS512"];

    private readonly IServiceScopeFactory _scopeFactory = scopeFactory;
    private readonly IJwtService _jwtService = jwtService;

    [McpServerTool]
    [Description("Decodes a JWT and optionally validates its signature using a secret or public key.")]
    public object DecodeJwt(
        [Description("JWT token to decode.")] string token,
        [Description("Optional HMAC secret or RSA public key used to validate the signature.")] string? secret = null)
    {
        var result = _jwtService.DecodeToken(token);
        if (!result.IsValid)
        {
            return McpToolResponse.Failure(result.ErrorMessage ?? "Unable to decode JWT.", "jwt_decode",
                new { result.ErrorKey, result.ErrorArgs });
        }

        return McpToolResponse.Success(new
        {
            result.Header,
            result.Payload,
            result.Algorithm,
            result.ExpiresAt,
            result.IssuedAt,
            result.NotBefore,
            result.Issuer,
            result.Audience,
            result.Subject,
            signatureValid = string.IsNullOrWhiteSpace(secret)
                ? (bool?)null
                : _jwtService.ValidateTokenSignature(token, secret)
        }, "jwt_decode");
    }

    [McpServerTool]
    [Description("Builds and signs a JWT, optionally using or saving a stored template.")]
    public async Task<object> BuildJwt(
        [Description("Optional saved template ID. Explicit parameters override template values.")] int? templateId = null,
        [Description("Signing algorithm: HS256, HS384, HS512, RS256, RS384, or RS512.")] string? algorithm = null,
        [Description("HMAC secret or RSA private key. Omit to use the selected template or service default.")] string? secret = null,
        [Description("Optional RSA public key to store when saving a template.")] string? publicKey = null,
        [Description("Issuer claim.")] string? issuer = null,
        [Description("Audience claim.")] string? audience = null,
        [Description("Subject claim.")] string? subject = null,
        [Description("Token lifetime in minutes.")] int? expirationMinutes = null,
        [Description("Whether to include an expiration claim.")] bool? includeExpiration = null,
        [Description("Custom claims as a JSON object. Values are converted to claim strings.")] string? claimsJson = null,
        [Description("Optional name to save the effective settings as a template.")] string? saveAs = null,
        CancellationToken cancellationToken = default)
    {
        if (saveAs is not null && string.IsNullOrWhiteSpace(saveAs))
        {
            return McpToolResponse.Failure("Template name cannot be empty.", "jwt_build");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJwtBuilderTemplateRepository>();

        JwtBuilderTemplate? template = null;
        if (templateId.HasValue)
        {
            template = await repository.GetByIdAsync(templateId.Value, cancellationToken);
            if (template is null)
            {
                return McpToolResponse.Failure($"JWT template not found: {templateId.Value}", "jwt_build");
            }
        }

        var effectiveAlgorithm = NormalizeAlgorithm(algorithm ?? template?.Algorithm ?? "HS256");
        if (!SupportedAlgorithms.Contains(effectiveAlgorithm, StringComparer.Ordinal))
        {
            return McpToolResponse.Failure(
                $"Unsupported algorithm: {effectiveAlgorithm}. Supported algorithms: {string.Join(", ", SupportedAlgorithms)}",
                "jwt_build");
        }

        var effectiveSecret = string.IsNullOrWhiteSpace(secret)
            ? template?.Secret ?? _jwtService.GetDefaultSecretKey(effectiveAlgorithm)
            : secret;
        var effectiveIssuer = issuer ?? template?.Issuer;
        var effectiveAudience = audience ?? template?.Audience;
        var effectiveSubject = subject ?? template?.Subject;
        var shouldIncludeExpiration = includeExpiration ?? template?.IncludeExpiration ?? true;
        var effectiveExpiration = expirationMinutes ?? template?.ExpirationMinutes ?? 60;
        if (shouldIncludeExpiration && effectiveExpiration <= 0)
        {
            return McpToolResponse.Failure("Expiration minutes must be greater than 0.", "jwt_build");
        }

        var effectiveClaimsJson = claimsJson ?? template?.CustomClaimsJson;
        if (!TryParseClaims(effectiveClaimsJson, out var claims, out var claimsError))
        {
            return McpToolResponse.Failure(claimsError!, "jwt_build");
        }

        var normalizedSaveName = saveAs?.Trim();
        if (!string.IsNullOrWhiteSpace(normalizedSaveName)
            && await repository.TemplateNameExistsAsync(normalizedSaveName, cancellationToken: cancellationToken))
        {
            return McpToolResponse.Failure($"JWT template name already exists: {normalizedSaveName}", "jwt_build");
        }

        var expiresAt = shouldIncludeExpiration ? DateTime.UtcNow.AddMinutes(effectiveExpiration) : (DateTime?)null;
        var buildResult = _jwtService.BuildToken(
            claims,
            effectiveSecret,
            effectiveAlgorithm,
            expiresAt,
            effectiveIssuer,
            effectiveAudience,
            effectiveSubject);

        if (!buildResult.Success || string.IsNullOrWhiteSpace(buildResult.Token))
        {
            return McpToolResponse.Failure(buildResult.ErrorMessage ?? "Unable to build JWT.",
                "jwt_build", new { buildResult.ErrorKey, buildResult.ErrorArgs });
        }

        if (template is not null)
        {
            await repository.UpdateLastUsedAsync(template.Id, cancellationToken);
        }

        int? savedTemplateId = null;
        if (!string.IsNullOrWhiteSpace(normalizedSaveName))
        {
            var saved = await repository.SaveAsync(new JwtBuilderTemplate
            {
                TemplateName = normalizedSaveName,
                Algorithm = effectiveAlgorithm,
                Secret = effectiveSecret,
                PublicKey = publicKey ?? template?.PublicKey,
                Issuer = effectiveIssuer,
                Audience = effectiveAudience,
                Subject = effectiveSubject,
                ExpirationMinutes = effectiveExpiration,
                IncludeExpiration = shouldIncludeExpiration,
                CustomClaimsJson = effectiveClaimsJson,
                Notes = template?.Notes
            }, cancellationToken);
            savedTemplateId = saved.Id;
        }

        return McpToolResponse.Success(new
        {
            token = buildResult.Token,
            algorithm = effectiveAlgorithm,
            expiresAt,
            savedTemplateId
        }, "jwt_build");
    }

    [McpServerTool]
    [Description("Lists saved JWT templates without returning secret or private-key values.")]
    public async Task<object> ListJwtTemplates(
        [Description("Maximum number of records to return (maximum 100).")] int count = 50,
        [Description("Optional case-insensitive template name filter.")] string? name = null,
        CancellationToken cancellationToken = default)
    {
        if (count <= 0)
        {
            return McpToolResponse.Failure("Count must be greater than 0.", "jwt_list_templates");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJwtBuilderTemplateRepository>();
        var templates = await repository.GetAllAsync(cancellationToken);
        var filtered = string.IsNullOrWhiteSpace(name)
            ? templates
            : templates.Where(item => item.TemplateName.Contains(name, StringComparison.OrdinalIgnoreCase)).ToList();
        var boundedCount = Math.Min(count, MaxListCount);
        var items = filtered.Take(boundedCount).Select(ToTemplateSummary).ToList();

        return McpToolResponse.Success(
            new { count = items.Count, items },
            "jwt_list_templates",
            new { requestedCount = count, wasCapped = count > MaxListCount });
    }

    [McpServerTool]
    [Description("Reads a saved JWT template's configuration without exposing secret or private-key values.")]
    public async Task<object> GetJwtTemplate(
        [Description("Saved template ID.")] int id,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJwtBuilderTemplateRepository>();
        var template = await repository.GetByIdAsync(id, cancellationToken);
        return template is null
            ? McpToolResponse.Failure($"JWT template not found: {id}", "jwt_get_template")
            : McpToolResponse.Success(ToTemplateSummary(template), "jwt_get_template");
    }

    [McpServerTool]
    [Description("Creates a saved JWT template. Secret and private-key fields are never returned by MCP tools.")]
    public async Task<object> CreateJwtTemplate(
        [Description("Unique display name.")] string templateName,
        [Description("Signing algorithm: HS256, HS384, HS512, RS256, RS384, or RS512.")] string algorithm,
        [Description("HMAC secret or RSA private key.")] string secret,
        [Description("Optional RSA public key.")] string? publicKey = null,
        [Description("Issuer claim.")] string? issuer = null,
        [Description("Audience claim.")] string? audience = null,
        [Description("Subject claim.")] string? subject = null,
        [Description("Token lifetime in minutes.")] int expirationMinutes = 60,
        [Description("Whether tokens created from this template have an expiration.")] bool includeExpiration = true,
        [Description("Custom claims as a JSON object.")] string? claimsJson = null,
        [Description("Optional notes.")] string? notes = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(templateName) || string.IsNullOrWhiteSpace(algorithm))
        {
            return McpToolResponse.Failure("Template name and algorithm are required.", "jwt_create_template");
        }

        var normalizedName = templateName.Trim();
        var normalizedAlgorithm = NormalizeAlgorithm(algorithm);
        if (normalizedName.Length == 0 || string.IsNullOrWhiteSpace(secret))
        {
            return McpToolResponse.Failure("Template name and secret are required.", "jwt_create_template");
        }

        if (!SupportedAlgorithms.Contains(normalizedAlgorithm, StringComparer.Ordinal))
        {
            return McpToolResponse.Failure(
                $"Unsupported algorithm: {normalizedAlgorithm}. Supported algorithms: {string.Join(", ", SupportedAlgorithms)}",
                "jwt_create_template");
        }

        if (includeExpiration && expirationMinutes <= 0)
        {
            return McpToolResponse.Failure("Expiration minutes must be greater than 0.", "jwt_create_template");
        }

        if (!TryNormalizeClaimsJson(claimsJson, out var normalizedClaims, out var claimsError))
        {
            return McpToolResponse.Failure(claimsError!, "jwt_create_template");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJwtBuilderTemplateRepository>();
        if (await repository.TemplateNameExistsAsync(normalizedName, cancellationToken: cancellationToken))
        {
            return McpToolResponse.Failure($"JWT template name already exists: {normalizedName}", "jwt_create_template");
        }

        var saved = await repository.SaveAsync(new JwtBuilderTemplate
        {
            TemplateName = normalizedName,
            Algorithm = normalizedAlgorithm,
            Secret = secret,
            PublicKey = publicKey,
            Issuer = issuer,
            Audience = audience,
            Subject = subject,
            ExpirationMinutes = expirationMinutes,
            IncludeExpiration = includeExpiration,
            CustomClaimsJson = normalizedClaims,
            Notes = notes
        }, cancellationToken);

        return McpToolResponse.Success(ToTemplateSummary(saved), "jwt_create_template");
    }

    [McpServerTool]
    [Description("Updates a saved JWT template. Secret and private-key values are never returned.")]
    public async Task<object> UpdateJwtTemplate(
        [Description("Saved template ID.")] int id,
        [Description("New display name.")] string? templateName = null,
        [Description("New signing algorithm.")] string? algorithm = null,
        [Description("New HMAC secret or RSA private key.")] string? secret = null,
        [Description("New RSA public key.")] string? publicKey = null,
        [Description("Clear the stored public key.")] bool clearPublicKey = false,
        [Description("New issuer claim.")] string? issuer = null,
        [Description("Clear the stored issuer.")] bool clearIssuer = false,
        [Description("New audience claim.")] string? audience = null,
        [Description("Clear the stored audience.")] bool clearAudience = false,
        [Description("New subject claim.")] string? subject = null,
        [Description("Clear the stored subject.")] bool clearSubject = false,
        [Description("New expiration duration.")] int? expirationMinutes = null,
        [Description("New expiration-enabled value.")] bool? includeExpiration = null,
        [Description("Replace custom claims with this JSON object.")] string? claimsJson = null,
        [Description("Clear all custom claims.")] bool clearClaims = false,
        [Description("New notes.")] string? notes = null,
        [Description("Clear stored notes.")] bool clearNotes = false,
        CancellationToken cancellationToken = default)
    {
        if (!clearClaims && claimsJson is not null && string.IsNullOrWhiteSpace(claimsJson))
        {
            return McpToolResponse.Failure("Custom claims must be a JSON object, or use clear_claims=true.",
                "jwt_update_template");
        }

        if (clearPublicKey && !string.IsNullOrWhiteSpace(publicKey)
            || clearIssuer && !string.IsNullOrWhiteSpace(issuer)
            || clearAudience && !string.IsNullOrWhiteSpace(audience)
            || clearSubject && !string.IsNullOrWhiteSpace(subject)
            || clearClaims && !string.IsNullOrWhiteSpace(claimsJson)
            || clearNotes && !string.IsNullOrWhiteSpace(notes))
        {
            return McpToolResponse.Failure("A field cannot be set and cleared in the same request.", "jwt_update_template");
        }

        if (expirationMinutes is <= 0)
        {
            return McpToolResponse.Failure("Expiration minutes must be greater than 0.", "jwt_update_template");
        }

        string? normalizedClaims = null;
        if (!clearClaims && claimsJson is not null
            && !TryNormalizeClaimsJson(claimsJson, out normalizedClaims, out var claimsError))
        {
            return McpToolResponse.Failure(claimsError!, "jwt_update_template");
        }

        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJwtBuilderTemplateRepository>();
        var template = await repository.GetByIdAsync(id, cancellationToken);
        if (template is null)
        {
            return McpToolResponse.Failure($"JWT template not found: {id}", "jwt_update_template");
        }

        var updatedName = string.IsNullOrWhiteSpace(templateName) ? template.TemplateName : templateName.Trim();
        if (await repository.TemplateNameExistsAsync(updatedName, id, cancellationToken))
        {
            return McpToolResponse.Failure($"JWT template name already exists: {updatedName}", "jwt_update_template");
        }

        var updatedAlgorithm = NormalizeAlgorithm(algorithm ?? template.Algorithm);
        if (!SupportedAlgorithms.Contains(updatedAlgorithm, StringComparer.Ordinal))
        {
            return McpToolResponse.Failure(
                $"Unsupported algorithm: {updatedAlgorithm}. Supported algorithms: {string.Join(", ", SupportedAlgorithms)}",
                "jwt_update_template");
        }

        template.TemplateName = updatedName;
        template.Algorithm = updatedAlgorithm;
        if (!string.IsNullOrWhiteSpace(secret))
        {
            template.Secret = secret;
        }
        template.PublicKey = clearPublicKey ? null : publicKey ?? template.PublicKey;
        template.Issuer = clearIssuer ? null : issuer ?? template.Issuer;
        template.Audience = clearAudience ? null : audience ?? template.Audience;
        template.Subject = clearSubject ? null : subject ?? template.Subject;
        template.ExpirationMinutes = expirationMinutes ?? template.ExpirationMinutes;
        template.IncludeExpiration = includeExpiration ?? template.IncludeExpiration;
        template.CustomClaimsJson = clearClaims ? null : claimsJson is null ? template.CustomClaimsJson : normalizedClaims;
        template.Notes = clearNotes ? null : notes ?? template.Notes;

        if (template.IncludeExpiration && template.ExpirationMinutes <= 0)
        {
            return McpToolResponse.Failure("Expiration minutes must be greater than 0 when expiration is enabled.",
                "jwt_update_template");
        }

        var updated = await repository.UpdateAsync(template, cancellationToken);
        return updated
            ? McpToolResponse.Success(ToTemplateSummary(template), "jwt_update_template")
            : McpToolResponse.Failure($"JWT template not found: {id}", "jwt_update_template");
    }

    [McpServerTool]
    [Description("Deletes a saved JWT template by ID.")]
    public async Task<object> DeleteJwtTemplate(
        [Description("Saved template ID.")] int id,
        CancellationToken cancellationToken = default)
    {
        await using var scope = _scopeFactory.CreateAsyncScope();
        var repository = scope.ServiceProvider.GetRequiredService<IJwtBuilderTemplateRepository>();
        var existing = await repository.GetByIdAsync(id, cancellationToken);
        if (existing is null)
        {
            return McpToolResponse.Failure($"JWT template not found: {id}", "jwt_delete_template");
        }

        await repository.DeleteAsync(id, cancellationToken);
        return McpToolResponse.Success(new { id, templateName = existing.TemplateName }, "jwt_delete_template");
    }

    private static object ToTemplateSummary(JwtBuilderTemplate template) => new
    {
        id = template.Id,
        templateName = template.TemplateName,
        algorithm = template.Algorithm,
        issuer = template.Issuer,
        audience = template.Audience,
        subject = template.Subject,
        expirationMinutes = template.ExpirationMinutes,
        includeExpiration = template.IncludeExpiration,
        customClaimCount = CountClaims(template.CustomClaimsJson),
        notes = template.Notes,
        createdAt = template.CreatedAt,
        lastUsedAt = template.LastUsedAt,
        hasSecret = !string.IsNullOrWhiteSpace(template.Secret),
        hasPublicKey = !string.IsNullOrWhiteSpace(template.PublicKey)
    };

    private static int CountClaims(string? claimsJson)
    {
        if (string.IsNullOrWhiteSpace(claimsJson))
        {
            return 0;
        }

        try
        {
            using var document = JsonDocument.Parse(claimsJson);
            return document.RootElement.ValueKind == JsonValueKind.Object
                ? document.RootElement.EnumerateObject().Count()
                : 0;
        }
        catch (JsonException)
        {
            return 0;
        }
    }

    private static string NormalizeAlgorithm(string algorithm) => algorithm.Trim().ToUpperInvariant();

    private static bool TryNormalizeClaimsJson(string? claimsJson, out string? normalizedClaims, out string? error)
    {
        normalizedClaims = null;
        error = null;
        if (string.IsNullOrWhiteSpace(claimsJson))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(claimsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Custom claims must be a JSON object.";
                return false;
            }

            normalizedClaims = document.RootElement.GetRawText();
            return true;
        }
        catch (JsonException ex)
        {
            error = $"Invalid claims JSON: {ex.Message}";
            return false;
        }
    }

    private static bool TryParseClaims(
        string? claimsJson,
        out Dictionary<string, object> claims,
        out string? error)
    {
        claims = new Dictionary<string, object>(StringComparer.Ordinal);
        error = null;
        if (string.IsNullOrWhiteSpace(claimsJson))
        {
            return true;
        }

        try
        {
            using var document = JsonDocument.Parse(claimsJson);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                error = "Custom claims must be a JSON object.";
                return false;
            }

            foreach (var property in document.RootElement.EnumerateObject())
            {
                claims[property.Name] = property.Value.ValueKind switch
                {
                    JsonValueKind.Array => property.Value.EnumerateArray().Select(item => item.ToString()).ToArray(),
                    JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                    _ => property.Value.ToString()
                };
            }

            return true;
        }
        catch (JsonException ex)
        {
            error = $"Invalid claims JSON: {ex.Message}";
            return false;
        }
    }
}
