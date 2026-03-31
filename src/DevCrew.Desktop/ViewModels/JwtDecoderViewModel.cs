using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Presentation.ViewModels;
using DevCrew.Desktop.Services;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the JWT Decoder view
/// </summary>
public partial class JwtDecoderViewModel : BaseViewModel
{
    private readonly IJwtService _jwtService;
    private readonly IClipboardService _clipboardService;
    private readonly ILocalizationService _localizationService;

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
    private string publicKey = string.Empty;

    [ObservableProperty]
    private string? algorithm;

    [ObservableProperty]
    private bool? isSignatureValid;

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

    [ObservableProperty]
    private bool isSecretVisible;

    public bool HasToken => !string.IsNullOrWhiteSpace(RawToken);
    public bool CanDecode => HasToken;
    public bool CanValidateSignature => HasToken && ((!string.IsNullOrWhiteSpace(Secret)) || (!string.IsNullOrWhiteSpace(PublicKey)));
    public bool IsRsaAlgorithm => Algorithm?.StartsWith("RS") == true;
    public bool IsTokenExpired => ExpiresAt.HasValue && ExpiresAt.Value < DateTime.UtcNow;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtDecoderViewModel"/> class.
    /// </summary>
    public JwtDecoderViewModel(
        IErrorHandler errorHandler,
        IJwtService jwtService, 
        IClipboardService clipboardService,
        ILocalizationService localizationService)
        : base(errorHandler)
    {
        _jwtService = jwtService;
        _clipboardService = clipboardService;
        _localizationService = localizationService;
    }

    [RelayCommand(CanExecute = nameof(CanDecode))]
    private void DecodeToken()
    {
        ErrorMessage = null;
        IsSignatureValid = null;

        var result = _jwtService.DecodeToken(RawToken);

        if (result.IsValid)
        {
            DecodedHeader = result.Header;
            DecodedPayload = result.Payload;
            Algorithm = result.Algorithm;
            ExpiresAt = result.ExpiresAt;
            IssuedAt = result.IssuedAt;
            Issuer = result.Issuer;
            Audience = result.Audience;
            Subject = result.Subject;
            ErrorMessage = null;
            OnPropertyChanged(nameof(IsRsaAlgorithm));
        }
        else
        {
            ErrorMessage = _localizationService.GetStringOrFallback(
                result.ErrorKey,
                result.ErrorMessage ?? _localizationService.GetString("common.error_unknown"),
                result.ErrorArgs ?? []);
            ClearDecodedData();
        }
    }

    [RelayCommand(CanExecute = nameof(CanValidateSignature))]
    private void ValidateSignature()
    {
        var key = IsRsaAlgorithm ? PublicKey : Secret;
        IsSignatureValid = _jwtService.ValidateTokenSignature(RawToken, key);
    }

    [RelayCommand]
    private async Task CopyHeader()
    {
        if (!string.IsNullOrWhiteSpace(DecodedHeader))
        {
            IsHeaderCopied = await _clipboardService.TrySetTextAsync(DecodedHeader);
            if (IsHeaderCopied)
            {
                await ResetCopyIndicatorAsync(() => IsHeaderCopied = false);
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
                await ResetCopyIndicatorAsync(() => IsPayloadCopied = false);
            }
        }
    }

    [RelayCommand]
    private void Clear()
    {
        RawToken = string.Empty;
        Secret = string.Empty;
        PublicKey = string.Empty;
        ClearDecodedData();
        ErrorMessage = null;
        IsSignatureValid = null;
    }

    private void ClearDecodedData()
    {
        DecodedHeader = null;
        DecodedPayload = null;
        Algorithm = null;
        ExpiresAt = null;
        IssuedAt = null;
        Issuer = null;
        Audience = null;
        Subject = null;
        OnPropertyChanged(nameof(IsRsaAlgorithm));
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
        if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(DecodedHeader) && !IsRsaAlgorithm)
        {
            ValidateSignature();
        }
        ValidateSignatureCommand.NotifyCanExecuteChanged();
    }

    partial void OnPublicKeyChanged(string value)
    {
        // Auto-validate when public key is entered for RSA algorithms
        if (!string.IsNullOrWhiteSpace(value) && !string.IsNullOrWhiteSpace(DecodedHeader) && IsRsaAlgorithm)
        {
            ValidateSignature();
        }
        ValidateSignatureCommand.NotifyCanExecuteChanged();
    }

    /// <summary>
    /// Helper method to reset copy indicator after a delay.
    /// </summary>
    /// <param name="resetAction">Action to execute for resetting the indicator</param>
    private async Task ResetCopyIndicatorAsync(Action resetAction)
    {
        await Task.Delay(2000);
        resetAction();
    }

    [RelayCommand]
    private void ToggleSecretVisibility()
    {
        IsSecretVisible = !IsSecretVisible;
    }
}
