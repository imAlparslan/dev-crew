namespace DevCrew.Core.Data;

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
}
