namespace DevCrew.Core.Infrastructure.Persistence;

/// <summary>
/// Central configuration constants for database entity constraints.
/// Used across models and DbContext to ensure consistency.
/// </summary>
public static class EntityConfiguration
{
    /// <summary>
    /// Maximum length for GUID value field (standard GUID string representation)
    /// </summary>
    public const int GuidValueMaxLength = 36;

    /// <summary>
    /// Maximum length for notes/description fields
    /// </summary>
    public const int NotesMaxLength = 500;

    /// <summary>
    /// Maximum length for JWT token storage
    /// </summary>
    public const int JwtTokenMaxLength = 5000;

    /// <summary>
    /// Maximum length for JWT header/payload JSON
    /// </summary>
    public const int JwtPayloadMaxLength = 2000;

    /// <summary>
    /// Maximum length for generic string fields
    /// </summary>
    public const int StringFieldMaxLength = 255;

    /// <summary>
    /// Maximum length for JWT Builder template names
    /// </summary>
    public const int TemplateNameMaxLength = 100;

    /// <summary>
    /// Maximum length for saved regex preset names
    /// </summary>
    public const int RegexPresetNameMaxLength = 100;

    /// <summary>
    /// Maximum length for saved regex pattern content
    /// </summary>
    public const int RegexPatternMaxLength = 5000;

    /// <summary>
    /// Maximum length for culture name values (for example: en-US)
    /// </summary>
    public const int CultureNameMaxLength = 10;

    /// <summary>
    /// Maximum length for font family key identifiers
    /// </summary>
    public const int FontFamilyKeyMaxLength = 50;
}
