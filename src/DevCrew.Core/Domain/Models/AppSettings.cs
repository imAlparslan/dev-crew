using System.ComponentModel.DataAnnotations;

namespace DevCrew.Core.Domain.Models;

/// <summary>
/// Application-wide persisted settings.
/// Stored as a singleton row in the database.
/// </summary>
public class AppSettings
{
    public const int SingletonId = 1;
    public const string DefaultLanguageCultureName = "en-US";
    public const string DefaultFontSizePreference = "Medium";
    public const string DefaultUiFontFamily = "Inter";
    public const string DefaultHeadingFontFamily = "Inter";
    public const string DefaultButtonFontFamily = "Inter";
    public const string DefaultContentFontFamily = "Consolas";

    [Key]
    public int Id { get; set; } = SingletonId;

    [Required]
    public string LanguageCultureName { get; set; } = DefaultLanguageCultureName;

    [Required]
    public string FontSizePreference { get; set; } = DefaultFontSizePreference;

    [Required]
    public string UiFontFamily { get; set; } = DefaultUiFontFamily;

    [Required]
    public string HeadingFontFamily { get; set; } = DefaultHeadingFontFamily;

    [Required]
    public string ButtonFontFamily { get; set; } = DefaultButtonFontFamily;

    [Required]
    public string ContentFontFamily { get; set; } = DefaultContentFontFamily;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
