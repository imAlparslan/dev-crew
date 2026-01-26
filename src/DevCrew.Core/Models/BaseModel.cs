namespace DevCrew.Core.Models;

/// <summary>
/// Temel model sınıfı
/// </summary>
public abstract class BaseModel
{
    /// <summary>
    /// Benzersiz tanımlayıcı
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Oluşturma tarihi
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Son güncelleme tarihi
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
