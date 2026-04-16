using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Domain.Models;
using DevCrew.Core.Infrastructure.Persistence.Repositories;
using DevCrew.Desktop.Services;

namespace DevCrew.Desktop.ViewModels;

/// <summary>
/// ViewModel for the JWT Builder view
/// </summary>
public partial class JwtBuilderViewModel : BaseViewModel
{
    private readonly IJwtService _jwtService;
    private readonly IClipboardService _clipboardService;
    private readonly IJwtBuilderTemplateRepository _templateRepository;
    private readonly ILocalizationService _localizationService;

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
    private string issuer = string.Empty;

    [ObservableProperty]
    private string audience = string.Empty;

    [ObservableProperty]
    private string subject = string.Empty;

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

    [ObservableProperty]
    private string templateName = string.Empty;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(CanUpdateTemplate))]
    private int? currentTemplateId;

    [ObservableProperty]
    private JwtBuilderTemplateItemViewModel? selectedTemplate;

    public ObservableCollection<CustomClaimItem> CustomClaims { get; } = new();

    public ObservableCollection<JwtBuilderTemplateItemViewModel> SavedTemplates { get; } = new();

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
    public bool CanUpdateTemplate => CurrentTemplateId.HasValue;
    public bool CanSaveTemplate => !string.IsNullOrWhiteSpace(TemplateName);
    public bool CanLoadTemplate => SelectedTemplate != null;

    /// <summary>
    /// Initializes a new instance of the <see cref="JwtBuilderViewModel"/> class.
    /// </summary>
    public JwtBuilderViewModel(
        IErrorHandler errorHandler,
        IJwtService jwtService,
        IClipboardService clipboardService,
        IJwtBuilderTemplateRepository templateRepository,
        ILocalizationService localizationService)
        : base(errorHandler)
    {
        _jwtService = jwtService;
        _clipboardService = clipboardService;
        _templateRepository = templateRepository;
        _localizationService = localizationService;

        // Load templates when the view model is created
        _ = LoadTemplatesAsync();
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
            foreach (var customClaim in CustomClaims.Where(c => !string.IsNullOrWhiteSpace(c.Key)))
            {
                if (!claimsDict.TryGetValue(customClaim.Key, out var claimList))
                {
                    claimList = new List<string>();
                    claimsDict[customClaim.Key] = claimList;
                }
                claimList.Add(customClaim.Value ?? string.Empty);
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
                ErrorMessage = _localizationService.GetStringOrFallback(
                    result.ErrorKey,
                    result.ErrorMessage ?? _localizationService.GetString("common.error_unknown"),
                    result.ErrorArgs ?? []);
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

    #region Template Management

    /// <summary>
    /// Loads all saved templates from the database
    /// </summary>
    private async Task LoadTemplatesAsync()
    {
        try
        {
            var templates = await _templateRepository.GetAllAsync();
            SavedTemplates.Clear();

            foreach (var template in templates)
            {
                SavedTemplates.Add(new JwtBuilderTemplateItemViewModel(
                    template.Id,
                    template.TemplateName,
                    template.Algorithm,
                    template.CreatedAt,
                    template.LastUsedAt));
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, "Load JWT Builder templates");
        }
    }

    /// <summary>
    /// Loads a template and populates the form
    /// </summary>
    [RelayCommand]
    private async Task LoadTemplate()
    {
        if (SelectedTemplate == null)
            return;

        try
        {
            var template = await _templateRepository.GetByIdAsync(SelectedTemplate.Id);
            if (template == null)
            {
                ErrorMessage = _localizationService.GetString("jwtbuilder.template_not_found");
                return;
            }

            // Populate form with template data
            Algorithm = template.Algorithm;
            Secret = template.Secret;
            PublicKey = template.PublicKey ?? string.Empty;
            Issuer = template.Issuer ?? string.Empty;
            Audience = template.Audience ?? string.Empty;
            Subject = template.Subject ?? string.Empty;
            ExpirationMinutes = template.ExpirationMinutes;
            IncludeExpiration = template.IncludeExpiration;
            TemplateName = template.TemplateName;

            // Load custom claims from JSON
            CustomClaims.Clear();
            if (!string.IsNullOrWhiteSpace(template.CustomClaimsJson))
            {
                var claims = DeserializeCustomClaims(template.CustomClaimsJson);
                foreach (var claim in claims)
                {
                    CustomClaims.Add(claim);
                }
            }

            // Set current template ID for update functionality
            CurrentTemplateId = template.Id;

            // Update last used timestamp
            await _templateRepository.UpdateLastUsedAsync(template.Id);

            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, "Load JWT Builder template");
            ErrorMessage = $"Template yükleme hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Saves current configuration as a new template
    /// </summary>
    [RelayCommand]
    private async Task SaveAsNewTemplate()
    {
        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            ErrorMessage = _localizationService.GetString("jwtbuilder.template_name_required");
            return;
        }

        try
        {
            // Check if template name already exists
            var exists = await _templateRepository.TemplateNameExistsAsync(TemplateName);
            if (exists)
            {
                ErrorMessage = _localizationService.GetString("jwtbuilder.template_name_exists");
                return;
            }

            var template = new JwtBuilderTemplate
            {
                TemplateName = TemplateName,
                Algorithm = Algorithm,
                Secret = Secret,
                PublicKey = string.IsNullOrWhiteSpace(PublicKey) ? null : PublicKey,
                Issuer = string.IsNullOrWhiteSpace(Issuer) ? null : Issuer,
                Audience = string.IsNullOrWhiteSpace(Audience) ? null : Audience,
                Subject = string.IsNullOrWhiteSpace(Subject) ? null : Subject,
                ExpirationMinutes = ExpirationMinutes,
                IncludeExpiration = IncludeExpiration,
                CustomClaimsJson = SerializeCustomClaims(),
                Notes = null
            };

            var saved = await _templateRepository.SaveAsync(template);

            // Add to list
            SavedTemplates.Add(new JwtBuilderTemplateItemViewModel(
                saved.Id,
                saved.TemplateName,
                saved.Algorithm,
                saved.CreatedAt,
                saved.LastUsedAt));

            // Set as current template
            CurrentTemplateId = saved.Id;

            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, "Save JWT Builder template");
            ErrorMessage = $"Template kaydetme hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Updates the currently loaded template with current configuration
    /// </summary>
    [RelayCommand]
    private async Task UpdateCurrentTemplate()
    {
        if (!CurrentTemplateId.HasValue)
        {
            ErrorMessage = _localizationService.GetString("jwtbuilder.template_none_to_update");
            return;
        }

        if (string.IsNullOrWhiteSpace(TemplateName))
        {
            ErrorMessage = _localizationService.GetString("jwtbuilder.template_name_required");
            return;
        }

        try
        {
            // Check if template name already exists (excluding current template)
            var exists = await _templateRepository.TemplateNameExistsAsync(TemplateName, CurrentTemplateId.Value);
            if (exists)
            {
                ErrorMessage = _localizationService.GetString("jwtbuilder.template_name_exists_other");
                return;
            }

            var template = new JwtBuilderTemplate
            {
                Id = CurrentTemplateId.Value,
                TemplateName = TemplateName,
                Algorithm = Algorithm,
                Secret = Secret,
                PublicKey = string.IsNullOrWhiteSpace(PublicKey) ? null : PublicKey,
                Issuer = string.IsNullOrWhiteSpace(Issuer) ? null : Issuer,
                Audience = string.IsNullOrWhiteSpace(Audience) ? null : Audience,
                Subject = string.IsNullOrWhiteSpace(Subject) ? null : Subject,
                ExpirationMinutes = ExpirationMinutes,
                IncludeExpiration = IncludeExpiration,
                CustomClaimsJson = SerializeCustomClaims(),
                Notes = null
            };

            var success = await _templateRepository.UpdateAsync(template);
            if (success)
            {
                // Update in list
                var existing = SavedTemplates.FirstOrDefault(t => t.Id == CurrentTemplateId.Value);
                if (existing != null)
                {
                    existing.TemplateName = TemplateName;
                    existing.Algorithm = Algorithm;
                }

                ErrorMessage = null;
            }
            else
            {
                ErrorMessage = _localizationService.GetString("jwtbuilder.template_update_failed");
            }
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, "Update JWT Builder template");
            ErrorMessage = $"Template güncelleme hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Deletes a template from the database
    /// </summary>
    [RelayCommand]
    private async Task DeleteTemplate()
    {
        if (SelectedTemplate == null)
            return;

        try
        {
            await _templateRepository.DeleteAsync(SelectedTemplate.Id);

            // Remove from list
            SavedTemplates.Remove(SelectedTemplate);

            // Clear current template ID if it was the deleted one
            if (CurrentTemplateId == SelectedTemplate.Id)
            {
                CurrentTemplateId = null;
            }

            SelectedTemplate = null;
            ErrorMessage = null;
        }
        catch (Exception ex)
        {
            ErrorHandler.LogException(ex, "Delete JWT Builder template");
            ErrorMessage = $"Template silme hatası: {ex.Message}";
        }
    }

    /// <summary>
    /// Serializes custom claims to JSON string
    /// </summary>
    private string? SerializeCustomClaims()
    {
        if (CustomClaims.Count == 0)
            return null;

        try
        {
            var claimsList = CustomClaims
                .Select(c => new { Key = c.Key, Value = c.Value })
                .ToList();

            return JsonSerializer.Serialize(claimsList);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Deserializes custom claims from JSON string
    /// </summary>
    private static List<CustomClaimItem> DeserializeCustomClaims(string json)
    {
        var result = new List<CustomClaimItem>();

        try
        {
            var claimsList = JsonSerializer.Deserialize<List<Dictionary<string, string>>>(json);
            if (claimsList != null)
            {
                foreach (var claim in claimsList)
                {
                    if (claim.TryGetValue("Key", out var key) && claim.TryGetValue("Value", out var value))
                    {
                        result.Add(new CustomClaimItem { Key = key, Value = value });
                    }
                }
            }
        }
        catch
        {
            // Return empty list on error
        }

        return result;
    }

    #endregion
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
