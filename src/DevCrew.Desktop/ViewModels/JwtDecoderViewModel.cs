using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using System.Text.Json;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the JWT Decoder view
/// </summary>
public partial class JwtDecoderViewModel : ObservableObject
{
    private readonly IJwtService _jwtService;
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasToken), nameof(CanDecode))]
    private string rawToken = string.Empty;

    [ObservableProperty]
    private string? decodedHeader;

    [ObservableProperty]
    private string? decodedPayload;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanValidateSignature))]
    private string secret = string.Empty;

    [ObservableProperty]
    private bool? isSignatureValid;

    [ObservableProperty]
    private string? errorMessage;

    [ObservableProperty]
    private bool isHeaderCopied;

    [ObservableProperty]
    private bool isPayloadCopied;

    [ObservableProperty]
    private DateTime? expiresAt;

    [ObservableProperty]
    private DateTime? issuedAt;

    [ObservableProperty]
    private string? issuer;

    [ObservableProperty]
    private string? audience;

    [ObservableProperty]
    private string? subject;

    public bool HasToken => !string.IsNullOrWhiteSpace(RawToken);
    public bool CanDecode => HasToken;
    public bool CanValidateSignature => HasToken && !string.IsNullOrWhiteSpace(Secret);

    public bool IsTokenExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtDecoderViewModel"/> class.
    /// </summary>
    public JwtDecoderViewModel(IJwtService jwtService, IClipboardService clipboardService)
    {
        _jwtService = jwtService;
        _clipboardService = clipboardService;
    }

    [RelayCommand(CanExecute = nameof(CanDecode))]
    private void DecodeToken()
    {
        try
        {
            ErrorMessage = null;
            IsSignatureValid = null;

            var result = _jwtService.DecodeToken(RawToken);

            if (result.IsValid)
            {
                DecodedHeader = result.Header;
                DecodedPayload = result.Payload;
                ExpiresAt = result.ExpiresAt;
                IssuedAt = result.IssuedAt;
                Issuer = result.Issuer;
                Audience = result.Audience;
                Subject = result.Subject;
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
                ClearDecodedData();
            }
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Error: {ex.Message}";
            ClearDecodedData();
        }
    }

    [RelayCommand(CanExecute = nameof(CanValidateSignature))]
    private void ValidateSignature()
    {
        try
        {
            IsSignatureValid = _jwtService.ValidateTokenSignature(RawToken, Secret);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Validation error: {ex.Message}";
            IsSignatureValid = false;
        }
    }

    [RelayCommand]
    private async Task CopyHeader()
    {
        if (!string.IsNullOrWhiteSpace(DecodedHeader))
        {
            IsHeaderCopied = await _clipboardService.TrySetTextAsync(DecodedHeader);
            if (IsHeaderCopied)
            {
                await Task.Delay(2000);
                IsHeaderCopied = false;
            }
        }
    }

    [RelayCommand]
    private async Task CopyPayload()
    {
        if (!string.IsNullOrWhiteSpace(DecodedPayload))
        {
            IsPayloadCopied = await _clipboardService.TrySetTextAsync(DecodedPayload);
            if (IsPayloadCopied)
            {
                await Task.Delay(2000);
                IsPayloadCopied = false;
            }
        }
    }

    [RelayCommand]
    private void Clear()
    {
        RawToken = string.Empty;
        Secret = string.Empty;
        ClearDecodedData();
        ErrorMessage = null;
        IsSignatureValid = null;
    }

    private void ClearDecodedData()
    {
        DecodedHeader = null;
        DecodedPayload = null;
        ExpiresAt = null;
        IssuedAt = null;
        Issuer = null;
        Audience = null;
        Subject = null;
    }

    partial void OnRawTokenChanged(string value)
    {
        // Auto-decode when token is pasted
        if (!string.IsNullOrWhiteSpace(value) && value.Length > 20)
        {
            DecodeToken();
        }
        DecodeTokenCommand.NotifyCanExecuteChanged();
        ValidateSignatureCommand.NotifyCanExecuteChanged();
    }

    partial void OnSecretChanged(string value)
    {
        // Auto-validate when secret is entered and token is already decoded
        if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(DecodedHeader))
        {
            ValidateSignature();
        }
        ValidateSignatureCommand.NotifyCanExecuteChanged();
    }
}
