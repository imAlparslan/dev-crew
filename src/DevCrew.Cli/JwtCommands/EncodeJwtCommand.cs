using System.ComponentModel;
using System.Security.Cryptography;
using System.Text.Json;
using DevCrew.Cli.Results;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Domain.Models;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevCrew.Cli.JwtCommands;

internal sealed class JwtEncodeCommandSettings : CommandSettings
{
    [CommandOption("-t|--template <NAME>")]
    [Description("Use a saved JWT template by name.")]
    public string? Template { get; init; }

    [CommandOption("-a|--algorithm <ALGORITHM>")]
    [Description("JWT algorithm: HS256, HS384, HS512, RS256, RS384, RS512.")]
    public string? Algorithm { get; init; }

    [CommandOption("-s|--secret <SECRET>")]
    [Description("Secret key for HMAC or private key for RSA.")]
    public string? Secret { get; init; }

    [CommandOption("-p|--public-key <PUBLIC_KEY>")]
    [Description("Public key for RSA algorithms.")]
    public string? PublicKey { get; init; }

    [CommandOption("--issuer <ISSUER>")]
    [Description("Issuer (iss) claim.")]
    public string? Issuer { get; init; }

    [CommandOption("--audience <AUDIENCE>")]
    [Description("Audience (aud) claim.")]
    public string? Audience { get; init; }

    [CommandOption("--subject <SUBJECT>")]
    [Description("Subject (sub) claim.")]
    public string? Subject { get; init; }

    [CommandOption("--expiration <MINUTES>")]
    [Description("Expiration in minutes (default: 60).")]
    public int? ExpirationMinutes { get; init; }

    [CommandOption("-c|--claim <CLAIM>")]
    [Description("Custom claim in key=value, key:value, or key-value format. Repeat for multiple claims.")]
    public string[] Claims { get; init; } = [];

    [CommandOption("--save <NAME>")]
    [Description("Save effective encode options as a reusable template.")]
    public string? Save { get; init; }

    public override ValidationResult Validate()
    {
        if (ExpirationMinutes is <= 0)
        {
            return ValidationResult.Error("--expiration must be greater than 0.");
        }

        return ValidationResult.Success();
    }
}

internal sealed class JwtEncodeCommand(
    IAnsiConsole console,
    IJwtService jwtService,
    IJwtBuilderTemplateRepository templateRepository) : AsyncCommand<JwtEncodeCommandSettings>
{
    private static readonly string[] SupportedAlgorithms = ["HS256", "HS384", "HS512", "RS256", "RS384", "RS512"];

    private readonly IAnsiConsole _console = console;
    private readonly IJwtService _jwtService = jwtService;
    private readonly IJwtBuilderTemplateRepository _templateRepository = templateRepository;

    protected override async Task<int> ExecuteAsync(CommandContext context, JwtEncodeCommandSettings settings, CancellationToken cancellationToken)
    {
        var warnings = new List<string>();
        var parseResult = await BuildEffectiveEncodeOptionsAsync(settings, warnings, cancellationToken);

        if (!parseResult.Success || parseResult.Options is null)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(parseResult.ErrorMessage ?? "Unable to prepare encode options.")}");
            return Result.Error;
        }

        var options = parseResult.Options;
        var claims = BuildClaimsDictionary(options.Claims);
        var expiresAt = DateTime.UtcNow.AddMinutes(options.ExpirationMinutes);

        var buildResult = _jwtService.BuildToken(
            claims: claims,
            secret: options.Secret,
            algorithm: options.Algorithm,
            expiresAt: expiresAt,
            issuer: options.Issuer,
            audience: options.Audience,
            subject: options.Subject);

        if (!buildResult.Success || string.IsNullOrWhiteSpace(buildResult.Token))
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(buildResult.ErrorMessage ?? "Unable to build JWT token.")}");
            return Result.Error;
        }

        if (options.TemplateId.HasValue)
        {
            _ = await _templateRepository.UpdateLastUsedAsync(options.TemplateId.Value, cancellationToken);
        }

        var saveResult = await SaveTemplateIfRequestedAsync(settings.Save, options, cancellationToken);

        _console.MarkupLine("[green]JWT Encoded Successfully[/]");
        _console.MarkupLine($"[green]Algorithm:[/] {Markup.Escape(options.Algorithm)}");
        _console.MarkupLine($"[green]Expires At:[/] {expiresAt:O}");

        if (IsRsaAlgorithm(options.Algorithm) && !string.IsNullOrWhiteSpace(options.PublicKey))
        {
            _console.MarkupLine("[green]Public Key:[/]");
            _console.MarkupLine(Markup.Escape(options.PublicKey));
        }

        if (warnings.Count > 0)
        {
            foreach (var warning in warnings)
            {
                _console.MarkupLine($"[yellow]Warning:[/] {Markup.Escape(warning)}");
            }
        }

        _console.MarkupLine("[green]Token:[/]");
        _console.MarkupLine(Markup.Escape(buildResult.Token));

        if (!saveResult.Success)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(saveResult.ErrorMessage ?? "Token generated but template could not be saved.")}");
            return Result.Error;
        }

        if (!string.IsNullOrWhiteSpace(saveResult.Message))
        {
            _console.MarkupLine($"[green]{Markup.Escape(saveResult.Message)}[/]");
        }

        return Result.Success;
    }

    private async Task<(bool Success, EncodeOptions? Options, string? ErrorMessage)> BuildEffectiveEncodeOptionsAsync(
        JwtEncodeCommandSettings settings,
        List<string> warnings,
        CancellationToken cancellationToken)
    {
        var options = new EncodeOptions();

        if (!string.IsNullOrWhiteSpace(settings.Template))
        {
            var templates = await _templateRepository.GetAllAsync(cancellationToken);
            var matches = templates
                .Where(t => string.Equals(t.TemplateName, settings.Template, StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (matches.Count == 0)
            {
                return (false, null, $"Template not found: {settings.Template}");
            }

            if (matches.Count > 1)
            {
                return (false, null, $"Multiple templates found with the same name: {settings.Template}. Use a unique template name.");
            }

            ApplyTemplate(options, matches[0]);
        }

        if (!string.IsNullOrWhiteSpace(settings.Algorithm))
        {
            options.Algorithm = settings.Algorithm.Trim().ToUpperInvariant();
        }

        if (!string.IsNullOrWhiteSpace(settings.Secret))
        {
            options.Secret = settings.Secret;
        }

        if (!string.IsNullOrWhiteSpace(settings.PublicKey))
        {
            options.PublicKey = settings.PublicKey;
        }

        if (!string.IsNullOrWhiteSpace(settings.Issuer))
        {
            options.Issuer = settings.Issuer;
        }

        if (!string.IsNullOrWhiteSpace(settings.Audience))
        {
            options.Audience = settings.Audience;
        }

        if (!string.IsNullOrWhiteSpace(settings.Subject))
        {
            options.Subject = settings.Subject;
        }

        if (settings.ExpirationMinutes.HasValue)
        {
            options.ExpirationMinutes = settings.ExpirationMinutes.Value;
        }

        if (!SupportedAlgorithms.Contains(options.Algorithm))
        {
            var supported = string.Join(", ", SupportedAlgorithms);
            return (false, null, $"Unsupported algorithm: {options.Algorithm}. Supported: {supported}");
        }

        var cliClaimsParse = ParseClaims(settings.Claims);
        if (!cliClaimsParse.Success)
        {
            return (false, null, cliClaimsParse.ErrorMessage);
        }

        options.Claims.AddRange(cliClaimsParse.Claims);
        ApplyKeyFallbacks(options, warnings);

        if (string.IsNullOrWhiteSpace(options.Secret))
        {
            return (false, null, "Secret key could not be resolved for the selected algorithm.");
        }

        return (true, options, null);
    }

    private static Dictionary<string, object> BuildClaimsDictionary(List<KeyValuePair<string, string>> claims)
    {
        var grouped = claims
            .GroupBy(x => x.Key)
            .ToDictionary(x => x.Key, x => x.Select(y => y.Value).ToList());

        var payload = new Dictionary<string, object>();
        foreach (var claim in grouped)
        {
            payload[claim.Key] = claim.Value.Count == 1 ? claim.Value[0] : claim.Value.ToArray();
        }

        return payload;
    }

    private async Task<(bool Success, string? ErrorMessage, string? Message)> SaveTemplateIfRequestedAsync(
        string? saveName,
        EncodeOptions options,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(saveName))
        {
            return (true, null, null);
        }

        var normalizedName = saveName.Trim();
        var exists = await _templateRepository.TemplateNameExistsAsync(normalizedName, cancellationToken: cancellationToken);
        if (exists)
        {
            return (false, $"Template name already exists: {normalizedName}", null);
        }

        var template = new JwtBuilderTemplate
        {
            TemplateName = normalizedName,
            Algorithm = options.Algorithm,
            Secret = options.Secret,
            PublicKey = string.IsNullOrWhiteSpace(options.PublicKey) ? null : options.PublicKey,
            Issuer = string.IsNullOrWhiteSpace(options.Issuer) ? null : options.Issuer,
            Audience = string.IsNullOrWhiteSpace(options.Audience) ? null : options.Audience,
            Subject = string.IsNullOrWhiteSpace(options.Subject) ? null : options.Subject,
            ExpirationMinutes = options.ExpirationMinutes,
            IncludeExpiration = true,
            CustomClaimsJson = SerializeClaims(options.Claims),
            Notes = null
        };

        _ = await _templateRepository.SaveAsync(template, cancellationToken);
        return (true, null, $"Template saved: {normalizedName}");
    }

    private static void ApplyTemplate(EncodeOptions options, JwtBuilderTemplate template)
    {
        options.TemplateId = template.Id;
        options.Algorithm = template.Algorithm;
        options.Secret = template.Secret;
        options.PublicKey = template.PublicKey;
        options.Issuer = template.Issuer;
        options.Audience = template.Audience;
        options.Subject = template.Subject;

        if (template.IncludeExpiration && template.ExpirationMinutes > 0)
        {
            options.ExpirationMinutes = template.ExpirationMinutes;
        }

        options.Claims.AddRange(DeserializeClaims(template.CustomClaimsJson));
    }

    private static (bool Success, List<KeyValuePair<string, string>> Claims, string? ErrorMessage) ParseClaims(IEnumerable<string> rawClaims)
    {
        var claims = new List<KeyValuePair<string, string>>();

        foreach (var rawClaim in rawClaims)
        {
            if (!TryParseClaim(rawClaim, out var key, out var value))
            {
                return (false, [], $"Invalid claim format: {rawClaim}. Use key=value, key:value, or key-value.");
            }

            claims.Add(new KeyValuePair<string, string>(key, value));
        }

        return (true, claims, null);
    }

    private static bool TryParseClaim(string rawClaim, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        if (string.IsNullOrWhiteSpace(rawClaim))
        {
            return false;
        }

        var trimmed = rawClaim.Trim();
        var separators = new[] { '=', ':', '-' };

        foreach (var separator in separators)
        {
            var index = trimmed.IndexOf(separator);
            if (index <= 0 || index >= trimmed.Length - 1)
            {
                continue;
            }

            key = trimmed[..index].Trim();
            value = trimmed[(index + 1)..].Trim();
            return !string.IsNullOrWhiteSpace(key);
        }

        return false;
    }

    private void ApplyKeyFallbacks(EncodeOptions options, List<string> warnings)
    {
        if (IsRsaAlgorithm(options.Algorithm))
        {
            if (string.IsNullOrWhiteSpace(options.Secret))
            {
                GenerateMockRsaKeys(options);
                warnings.Add("RSA key pair was not fully provided. A mock RSA private/public key pair was generated for this run.");
                return;
            }

            if (string.IsNullOrWhiteSpace(options.PublicKey))
            {
                if (TryDerivePublicKeyFromPrivateKey(options.Secret, out var derivedPublicKey))
                {
                    options.PublicKey = derivedPublicKey;
                    warnings.Add("Public key was not provided. It was derived from the RSA private key.");
                }
                else
                {
                    GenerateMockRsaKeys(options);
                    warnings.Add("Provided RSA key was not usable for deriving a public key. A mock RSA key pair was generated for this run.");
                }
            }

            return;
        }

        options.PublicKey = null;
        if (string.IsNullOrWhiteSpace(options.Secret))
        {
            options.Secret = _jwtService.GetDefaultSecretKey(options.Algorithm);
            warnings.Add("Secret key was not provided. A development mock secret was used for the selected HMAC algorithm.");
        }
    }

    private static bool IsRsaAlgorithm(string algorithm)
    {
        return algorithm.StartsWith("RS", StringComparison.OrdinalIgnoreCase);
    }

    private static void GenerateMockRsaKeys(EncodeOptions options)
    {
        using var rsa = RSA.Create(2048);
        options.Secret = rsa.ExportRSAPrivateKeyPem();
        options.PublicKey = rsa.ExportSubjectPublicKeyInfoPem();
    }

    private static bool TryDerivePublicKeyFromPrivateKey(string privateKey, out string publicKey)
    {
        publicKey = string.Empty;

        try
        {
            using var rsa = RSA.Create();
            if (privateKey.Contains("BEGIN RSA PRIVATE KEY", StringComparison.Ordinal) ||
                privateKey.Contains("BEGIN PRIVATE KEY", StringComparison.Ordinal))
            {
                rsa.ImportFromPem(privateKey);
            }
            else
            {
                rsa.FromXmlString(privateKey);
            }

            publicKey = rsa.ExportSubjectPublicKeyInfoPem();
            return true;
        }
        catch
        {
            return false;
        }
    }

    private static List<KeyValuePair<string, string>> DeserializeClaims(string? customClaimsJson)
    {
        var claims = new List<KeyValuePair<string, string>>();
        if (string.IsNullOrWhiteSpace(customClaimsJson))
        {
            return claims;
        }

        try
        {
            using var document = JsonDocument.Parse(customClaimsJson);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Array)
            {
                foreach (var item in root.EnumerateArray())
                {
                    if (item.ValueKind != JsonValueKind.Object)
                    {
                        continue;
                    }

                    if (!TryReadClaimPair(item, out var key, out var value))
                    {
                        continue;
                    }

                    claims.Add(new KeyValuePair<string, string>(key, value));
                }
            }
            else if (root.ValueKind == JsonValueKind.Object)
            {
                foreach (var property in root.EnumerateObject())
                {
                    var value = property.Value.ValueKind == JsonValueKind.String
                        ? property.Value.GetString() ?? string.Empty
                        : property.Value.ToString();

                    claims.Add(new KeyValuePair<string, string>(property.Name, value));
                }
            }
        }
        catch
        {
            return [];
        }

        return claims;
    }

    private static bool TryReadClaimPair(JsonElement item, out string key, out string value)
    {
        key = string.Empty;
        value = string.Empty;

        var hasKey = item.TryGetProperty("Key", out var keyElement)
            || item.TryGetProperty("key", out keyElement);
        var hasValue = item.TryGetProperty("Value", out var valueElement)
            || item.TryGetProperty("value", out valueElement);

        if (!hasKey || !hasValue)
        {
            return false;
        }

        key = keyElement.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        value = valueElement.ValueKind == JsonValueKind.String
            ? valueElement.GetString() ?? string.Empty
            : valueElement.ToString();

        return true;
    }

    private static string? SerializeClaims(List<KeyValuePair<string, string>> claims)
    {
        if (claims.Count == 0)
        {
            return null;
        }

        var serializable = claims
            .Select(c => new SerializableClaim { Key = c.Key, Value = c.Value })
            .ToList();

        return JsonSerializer.Serialize(serializable);
    }

    private sealed class EncodeOptions
    {
        public int? TemplateId { get; set; }
        public string Algorithm { get; set; } = "HS256";
        public string Secret { get; set; } = string.Empty;
        public string? PublicKey { get; set; }
        public string? Issuer { get; set; }
        public string? Audience { get; set; }
        public string? Subject { get; set; }
        public int ExpirationMinutes { get; set; } = 60;
        public List<KeyValuePair<string, string>> Claims { get; } = [];
    }

    private sealed class SerializableClaim
    {
        public required string Key { get; set; }
        public required string Value { get; set; }
    }
}
