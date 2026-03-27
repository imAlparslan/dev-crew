using System.ComponentModel.DataAnnotations;

namespace DevCrew.Core.Models;

/// <summary>
/// Application-wide persisted settings.
/// Stored as a singleton row in the database.
/// </summary>
public class AppSettings
{
    public const int SingletonId = 1;
    public const string DefaultLanguageCultureName = "en-US";

    [Key]
    public int Id { get; set; } = SingletonId;

    [Required]
    [MaxLength(10)]
    public string LanguageCultureName { get; set; } = DefaultLanguageCultureName;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
