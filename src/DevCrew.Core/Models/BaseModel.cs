namespace DevCrew.Core.Models;

/// <summary>
/// Temel model sınıfı
/// </summary>
public abstract class BaseModel
{
    /// <summary>
    /// Unique identifier
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Creation date
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Last update date
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
