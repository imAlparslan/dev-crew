using System.Collections.ObjectModel;
using System.Security.Cryptography;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Services;
using DevCrew.Core.ViewModels;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the JWT Builder view
/// </summary>
public partial class JwtBuilderViewModel : BaseViewModel
{
    private readonly IJwtService _jwtService;
    private readonly IClipboardService _clipboardService;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanBuildToken), nameof(IsRsaAlgorithm))]
    private string algorithm = "HS256";

    partial void OnAlgorithmChanged(string value)
    {
        // Update CanBuildToken when algorithm changes
        OnPropertyChanged(nameof(CanBuildToken));
        OnPropertyChanged(nameof(IsRsaAlgorithm));
        
        // Clear public key when switching to non-RSA algorithm
        if (!value.StartsWith("RS"))
        {
            PublicKey = string.Empty;
        }
    }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(BuildTokenCommand))]
    private string secret = string.Empty;

    [ObservableProperty]
    private string issuer = "DevCrew JWT Builder";

    [ObservableProperty]
    private string audience = "www.example.com";

    [ObservableProperty]
    private string subject = "example@example.com";

    [ObservableProperty]
    private int expirationMinutes = 60;

    [ObservableProperty]
    private bool includeExpiration = true;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(HasGeneratedToken))]
    private string? generatedToken;

    [ObservableProperty]
    private bool isTokenCopied;

    [ObservableProperty]
    private bool isSecretVisible;

    [ObservableProperty]
    private string publicKey = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddCustomClaimCommand))]
    private string customClaimKey = string.Empty;

    [ObservableProperty]
    private string customClaimValue = string.Empty;

    public ObservableCollection<CustomClaimItem> CustomClaims { get; } = new();

    public List<string> AvailableAlgorithms { get; } = new()
    {
        "HS256",
        "HS384",
        "HS512",
        "RS256",
        "RS384",
        "RS512"
    };

    public bool CanBuildToken => !string.IsNullOrWhiteSpace(Secret);
    public bool CanAddCustomClaim => !string.IsNullOrWhiteSpace(CustomClaimKey);
    public bool HasGeneratedToken => !string.IsNullOrWhiteSpace(GeneratedToken);
    public bool IsRsaAlgorithm => Algorithm.StartsWith("RS");

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtBuilderViewModel"/> class.
    /// </summary>
    public JwtBuilderViewModel(
        IErrorHandler errorHandler,
        IJwtService jwtService, 
        IClipboardService clipboardService)
        : base(errorHandler)
    {
        _jwtService = jwtService;
        _clipboardService = clipboardService;
    }

    [RelayCommand(CanExecute = nameof(CanBuildToken))]
    private void BuildToken()
    {
        ErrorMessage = null;
        GeneratedToken = null;
        IsTokenCopied = false;

        try
        {
            // Build claims dictionary - create array if the same key has multiple values
            var claimsDict = new Dictionary<string, List<string>>();

            // Group claims by key
            foreach (var customClaim in CustomClaims)
            {
                if (!string.IsNullOrWhiteSpace(customClaim.Key))
                {
                    if (!claimsDict.ContainsKey(customClaim.Key))
                    {
                        claimsDict[customClaim.Key] = new List<string>();
                    }
                    claimsDict[customClaim.Key].Add(customClaim.Value ?? string.Empty);
                }
            }

            // Convert to final claims dictionary
            var claims = new Dictionary<string, object>();
            foreach (var kvp in claimsDict)
            {
                if (kvp.Value.Count == 1)
                {
                    claims[kvp.Key] = kvp.Value[0];
                }
                else if (kvp.Value.Count > 1)
                {
                    claims[kvp.Key] = kvp.Value.ToArray();
                }
            }

            // Calculate expiration time
            DateTime? expiresAt = null;
            if (IncludeExpiration)
            {
                expiresAt = DateTime.UtcNow.AddMinutes(ExpirationMinutes);
            }

            // Build token
            var result = _jwtService.BuildToken(
                claims: claims,
                secret: Secret,
                algorithm: Algorithm,
                expiresAt: expiresAt,
                issuer: string.IsNullOrWhiteSpace(Issuer) ? null : Issuer,
                audience: string.IsNullOrWhiteSpace(Audience) ? null : Audience,
                subject: string.IsNullOrWhiteSpace(Subject) ? null : Subject
            );

            if (result.Success)
            {
                GeneratedToken = result.Token;
                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = result.ErrorMessage;
                GeneratedToken = null;
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, "Build JWT token");
            ErrorMessage = $"Error building token: {ex.Message}";
            GeneratedToken = null;
        }
    }

    [RelayCommand(CanExecute = nameof(CanAddCustomClaim))]
    private void AddCustomClaim()
    {
        if (string.IsNullOrWhiteSpace(CustomClaimKey))
            return;

        // Multiple values can be added with the same key
        CustomClaims.Add(new CustomClaimItem
        {
            Key = CustomClaimKey,
            Value = CustomClaimValue
        });

        // Clear input fields
        CustomClaimKey = string.Empty;
        CustomClaimValue = string.Empty;
    }

    [RelayCommand]
    private void RemoveCustomClaim(CustomClaimItem claim)
    {
        CustomClaims.Remove(claim);
    }

    [RelayCommand]
    private async Task CopyToken()
    {
        if (string.IsNullOrWhiteSpace(GeneratedToken))
            return;

        await _clipboardService.TrySetTextAsync(GeneratedToken);
        IsTokenCopied = true;

        // Reset the copied indicator after 2 seconds
        await Task.Delay(2000);
        IsTokenCopied = false;
    }

    [RelayCommand]
    private void Clear()
    {
        Secret = string.Empty;
        PublicKey = string.Empty;
        Issuer = string.Empty;
        Audience = string.Empty;
        Subject = string.Empty;
        ExpirationMinutes = 60;
        IncludeExpiration = true;
        Algorithm = "HS256";
        CustomClaims.Clear();
        CustomClaimKey = string.Empty;
        CustomClaimValue = string.Empty;
        GeneratedToken = null;
        ErrorMessage = null;
        IsTokenCopied = false;
        IsSecretVisible = false;
    }

    [RelayCommand]
    private void ToggleSecretVisibility()
    {
        IsSecretVisible = !IsSecretVisible;
    }

    [RelayCommand]
    private void GenerateSecretKey()
    {
        if (Algorithm.StartsWith("RS"))
        {
            // Auto-generate private key and public key for RSA algorithms
            using var rsa = RSA.Create(2048);
            Secret = rsa.ExportRSAPrivateKeyPem();
            PublicKey = rsa.ExportSubjectPublicKeyInfoPem();
        }
        else
        {
            // Use default secret key for HMAC algorithms
            Secret = _jwtService.GetDefaultSecretKey(Algorithm);
            PublicKey = string.Empty;
        }
    }

    [RelayCommand]
    private async Task CopySecret()
    {
        if (!string.IsNullOrWhiteSpace(Secret))
        {
            await _clipboardService.TrySetTextAsync(Secret);
        }
    }

    [RelayCommand]
    private async Task CopyPublicKey()
    {
        if (!string.IsNullOrWhiteSpace(PublicKey))
        {
            await _clipboardService.TrySetTextAsync(PublicKey);
        }
    }
}

/// <summary>
/// Represents a custom claim item
/// </summary>
public partial class CustomClaimItem : ObservableObject
{
    [ObservableProperty]
    private string key = string.Empty;

    [ObservableProperty]
    private string value = string.Empty;
}
