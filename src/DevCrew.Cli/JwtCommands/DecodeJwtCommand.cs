using System.ComponentModel;
using DevCrew.Cli.Results;
using DevCrew.Core.Application.Services;
using Spectre.Console;
using Spectre.Console.Cli;

namespace DevCrew.Cli.JwtCommands;

internal class JwtDecodeCommandSettings : CommandSettings
{
    [CommandOption("-d|--decode <TOKEN>")]
    [Description("JWT token to decode.")]
    public required string Token { get; init; }

    [CommandOption("-s|--secret <SECRET>")]
    [Description("Secret or public key used to validate the token signature.")]
    public string? Secret { get; init; }
}

internal sealed class DecodeJwtCommand(IAnsiConsole console, IJwtService jwtService) : AsyncCommand<JwtDecodeCommandSettings>
{
    private readonly IAnsiConsole _console = console;
    private readonly IJwtService _jwtService = jwtService;

    protected override Task<int> ExecuteAsync(CommandContext context, JwtDecodeCommandSettings settings, CancellationToken cancellationToken)
    {
        var result = _jwtService.DecodeToken(settings.Token);

        if (!result.IsValid)
        {
            _console.MarkupLine($"[red]Error:[/] {Markup.Escape(result.ErrorMessage ?? "Unable to decode JWT token.")}");
            return Task.FromResult(Result.Error);
        }

        _console.MarkupLine("[green]JWT Decoded Successfully[/]");

        if (!string.IsNullOrWhiteSpace(result.Algorithm))
        {
            _console.MarkupLine($"[green]Algorithm:[/] {Markup.Escape(result.Algorithm)}");
        }

        if (!string.IsNullOrWhiteSpace(result.Issuer))
        {
            _console.MarkupLine($"[green]Issuer:[/] {Markup.Escape(result.Issuer)}");
        }

        if (!string.IsNullOrWhiteSpace(result.Audience))
        {
            _console.MarkupLine($"[green]Audience:[/] {Markup.Escape(result.Audience)}");
        }

        if (!string.IsNullOrWhiteSpace(result.Subject))
        {
            _console.MarkupLine($"[green]Subject:[/] {Markup.Escape(result.Subject)}");
        }

        if (result.IssuedAt is not null)
        {
            _console.MarkupLine($"[green]Issued At:[/] {result.IssuedAt:O}");
        }

        if (result.NotBefore is not null)
        {
            _console.MarkupLine($"[green]Not Before:[/] {result.NotBefore:O}");
        }

        if (result.ExpiresAt is not null)
        {
            _console.MarkupLine($"[green]Expires At:[/] {result.ExpiresAt:O}");
        }

        _console.MarkupLine("[green]Header:[/]");
        _console.MarkupLine(Markup.Escape(result.Header ?? string.Empty));

        _console.MarkupLine("[green]Payload:[/]");
        _console.MarkupLine(Markup.Escape(result.Payload ?? string.Empty));

        if (!string.IsNullOrWhiteSpace(settings.Secret))
        {
            var isSignatureValid = _jwtService.ValidateTokenSignature(settings.Token, settings.Secret);
            _console.MarkupLine($"[green]Signature Validation:[/] {(isSignatureValid ? "[green]Valid[/]" : "[red]Invalid[/]")}");
        }

        return Task.FromResult(Result.Success);
    }
}