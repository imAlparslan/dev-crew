using System.ComponentModel;
using System.Text.Json;
using DevCrew.Cli.Results;
using DevCrew.Core.Domain.Models;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevCrew.Cli.JwtCommands;

internal sealed class JwtListTemplatesCommandSettings : CommandSettings
{
    [CommandOption("-n|--name <TEXT>")]
    [Description("Filter templates by name.")]
    public string? Name { get; init; }
}

internal sealed class JwtListTemplatesCommand(IAnsiConsole console, IJwtBuilderTemplateRepository templateRepository)
    : AsyncCommand<JwtListTemplatesCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IJwtBuilderTemplateRepository _templateRepository = templateRepository;

    protected override async Task<int> ExecuteAsync(CommandContext context, JwtListTemplatesCommandSettings settings, CancellationToken cancellationToken)
    {
        var templates = await _templateRepository.GetAllAsync(cancellationToken);

        if (!string.IsNullOrWhiteSpace(settings.Name))
        {
            templates = templates
                .Where(template => template.TemplateName.Contains(settings.Name, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        if (templates.Count == 0)
        {
            _console.MarkupLine("[yellow]No JWT templates found.[/]");
            return Result.Success;
        }

        var table = new Table();
        table.AddColumn("Id");
        table.AddColumn("Name");
        table.AddColumn("Algorithm");
        table.AddColumn("Expiration");
        table.AddColumn("Claims");
        table.AddColumn("Last Used");

        foreach (var template in templates)
        {
            table.AddRow(
                template.Id.ToString(),
                Markup.Escape(template.TemplateName),
                Markup.Escape(template.Algorithm),
                Markup.Escape(template.IncludeExpiration ? template.ExpirationMinutes.ToString() : "disabled"),
                Markup.Escape(JwtTemplateCommandFormatting.CountClaims(template.CustomClaimsJson).ToString()),
                Markup.Escape(template.LastUsedAt?.ToString("O") ?? "-")
            );
        }

        _console.Write(table);
        return Result.Success;
    }
}

internal sealed class JwtDeleteTemplateCommandSettings : CommandSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Template name to delete.")]
    public required string Name { get; init; }
}

internal sealed class JwtDeleteTemplateCommand(IAnsiConsole console, IJwtBuilderTemplateRepository templateRepository)
    : AsyncCommand<JwtDeleteTemplateCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IJwtBuilderTemplateRepository _templateRepository = templateRepository;

    protected override async Task<int> ExecuteAsync(CommandContext context, JwtDeleteTemplateCommandSettings settings, CancellationToken cancellationToken)
    {
        var template = await ResolveTemplateByNameAsync(settings.Name, cancellationToken);
        if (template is null)
        {
            _console.MarkupLine($"[red]Error:[/] Template not found: {Markup.Escape(settings.Name)}");
            return Result.Error;
        }

        var deleted = await _templateRepository.DeleteAsync(template.Id, cancellationToken);
        if (!deleted)
        {
            _console.MarkupLine($"[red]Error:[/] Unable to delete template: {Markup.Escape(settings.Name)}");
            return Result.Error;
        }

        _console.MarkupLine($"[green]Deleted template:[/] {Markup.Escape(template.TemplateName)}");
        return Result.Success;
    }

    private async Task<JwtBuilderTemplate?> ResolveTemplateByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        var templates = await _templateRepository.GetAllAsync(cancellationToken);
        return templates.FirstOrDefault(template => string.Equals(template.TemplateName, normalizedName, StringComparison.OrdinalIgnoreCase));
    }
}

internal sealed class JwtUpdateTemplateCommandSettings : CommandSettings
{
    [CommandArgument(0, "<NAME>")]
    [Description("Template name to update.")]
    public required string Name { get; init; }

    [CommandOption("--template-name <NAME>")]
    [Description("Rename the template.")]
    public string? TemplateName { get; init; }

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
    [Description("Expiration in minutes.")]
    public int? ExpirationMinutes { get; init; }

    [CommandOption("-c|--claim <CLAIM>")]
    [Description("Replace claims with the provided set. Repeat for multiple claims.")]
    public string[] Claims { get; init; } = [];

    [CommandOption("--clear-claims")]
    [Description("Remove all custom claims from the template.")]
    public bool ClearClaims { get; init; }

    public override ValidationResult Validate()
    {
        if (ExpirationMinutes is <= 0)
        {
            return ValidationResult.Error("--expiration must be greater than 0.");
        }

        if (ClearClaims && Claims.Length > 0)
        {
            return ValidationResult.Error("Use only one claims mode: --claim or --clear-claims.");
        }

        if (string.IsNullOrWhiteSpace(TemplateName)
            && string.IsNullOrWhiteSpace(Algorithm)
            && string.IsNullOrWhiteSpace(Secret)
            && string.IsNullOrWhiteSpace(PublicKey)
            && string.IsNullOrWhiteSpace(Issuer)
            && string.IsNullOrWhiteSpace(Audience)
            && string.IsNullOrWhiteSpace(Subject)
            && !ExpirationMinutes.HasValue
            && Claims.Length == 0
            && !ClearClaims)
        {
            return ValidationResult.Error("Provide at least one change to apply.");
        }

        return ValidationResult.Success();
    }
}

internal sealed class JwtUpdateTemplateCommand(IAnsiConsole console, IJwtBuilderTemplateRepository templateRepository)
    : AsyncCommand<JwtUpdateTemplateCommandSettings>
{
    private static readonly string[] SupportedAlgorithms = ["HS256", "HS384", "HS512", "RS256", "RS384", "RS512"];

    private readonly IAnsiConsole _console = console;
    private readonly IJwtBuilderTemplateRepository _templateRepository = templateRepository;

    protected override async Task<int> ExecuteAsync(CommandContext context, JwtUpdateTemplateCommandSettings settings, CancellationToken cancellationToken)
    {
        var template = await ResolveTemplateByNameAsync(settings.Name, cancellationToken);
        if (template is null)
        {
            _console.MarkupLine($"[red]Error:[/] Template not found: {Markup.Escape(settings.Name)}");
            return Result.Error;
        }

        var updatedName = string.IsNullOrWhiteSpace(settings.TemplateName)
            ? template.TemplateName
            : settings.TemplateName.Trim();

        if (!string.Equals(updatedName, template.TemplateName, StringComparison.Ordinal)
            && await _templateRepository.TemplateNameExistsAsync(updatedName, template.Id, cancellationToken))
        {
            _console.MarkupLine($"[red]Error:[/] Template name already exists: {Markup.Escape(updatedName)}");
            return Result.Error;
        }

        var updatedAlgorithm = string.IsNullOrWhiteSpace(settings.Algorithm)
            ? template.Algorithm
            : settings.Algorithm.Trim().ToUpperInvariant();

        if (!SupportedAlgorithms.Contains(updatedAlgorithm))
        {
            _console.MarkupLine($"[red]Error:[/] Unsupported algorithm: {Markup.Escape(updatedAlgorithm)}");
            return Result.Error;
        }

        string? updatedClaims;
        if (settings.ClearClaims)
        {
            updatedClaims = null;
        }
        else if (settings.Claims.Length > 0)
        {
            var parsedClaims = ParseClaims(settings.Claims);
            if (!parsedClaims.Success)
            {
                _console.MarkupLine($"[red]Error:[/] {Markup.Escape(parsedClaims.ErrorMessage ?? "Invalid claims input.")}");
                return Result.Error;
            }

            updatedClaims = SerializeClaims(parsedClaims.Claims);
        }
        else
        {
            updatedClaims = template.CustomClaimsJson;
        }

        var updatedTemplate = new JwtBuilderTemplate
        {
            Id = template.Id,
            TemplateName = updatedName,
            Algorithm = updatedAlgorithm,
            Secret = string.IsNullOrWhiteSpace(settings.Secret) ? template.Secret : settings.Secret,
            PublicKey = updatedAlgorithm.StartsWith("RS", StringComparison.OrdinalIgnoreCase)
                ? (string.IsNullOrWhiteSpace(settings.PublicKey) ? template.PublicKey : settings.PublicKey)
                : null,
            Issuer = string.IsNullOrWhiteSpace(settings.Issuer) ? template.Issuer : settings.Issuer,
            Audience = string.IsNullOrWhiteSpace(settings.Audience) ? template.Audience : settings.Audience,
            Subject = string.IsNullOrWhiteSpace(settings.Subject) ? template.Subject : settings.Subject,
            ExpirationMinutes = settings.ExpirationMinutes ?? template.ExpirationMinutes,
            IncludeExpiration = template.IncludeExpiration,
            CustomClaimsJson = updatedClaims,
            Notes = template.Notes,
            CreatedAt = template.CreatedAt,
            LastUsedAt = template.LastUsedAt
        };

        var updated = await _templateRepository.UpdateAsync(updatedTemplate, cancellationToken);
        if (!updated)
        {
            _console.MarkupLine($"[red]Error:[/] Unable to update template: {Markup.Escape(settings.Name)}");
            return Result.Error;
        }

        _console.MarkupLine($"[green]Updated template:[/] {Markup.Escape(updatedTemplate.TemplateName)}");
        _console.MarkupLine($"[green]Algorithm:[/] {Markup.Escape(updatedTemplate.Algorithm)}");
        _console.MarkupLine($"[green]Expiration:[/] {updatedTemplate.ExpirationMinutes}");
        _console.MarkupLine($"[green]Claims:[/] {JwtTemplateCommandFormatting.CountClaims(updatedTemplate.CustomClaimsJson)}");

        return Result.Success;
    }

    private async Task<JwtBuilderTemplate?> ResolveTemplateByNameAsync(string name, CancellationToken cancellationToken)
    {
        var normalizedName = name.Trim();
        var templates = await _templateRepository.GetAllAsync(cancellationToken);
        return templates.FirstOrDefault(template => string.Equals(template.TemplateName, normalizedName, StringComparison.OrdinalIgnoreCase));
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

    private static string? SerializeClaims(List<KeyValuePair<string, string>> claims)
    {
        if (claims.Count == 0)
        {
            return null;
        }

        var serializable = claims
            .Select(claim => new SerializableClaim { Key = claim.Key, Value = claim.Value })
            .ToList();

        return JsonSerializer.Serialize(serializable);
    }

    private sealed class SerializableClaim
    {
        public required string Key { get; set; }
        public required string Value { get; set; }
    }
}

internal static class JwtTemplateCommandFormatting
{
    public static int CountClaims(string? customClaimsJson)
    {
        if (string.IsNullOrWhiteSpace(customClaimsJson))
        {
            return 0;
        }

        try
        {
            using var document = JsonDocument.Parse(customClaimsJson);
            var root = document.RootElement;

            return root.ValueKind switch
            {
                JsonValueKind.Array => root.GetArrayLength(),
                JsonValueKind.Object => root.EnumerateObject().Count(),
                _ => 0
            };
        }
        catch
        {
            return 0;
        }
    }
}