using System.ComponentModel.DataAnnotations;

namespace DevCrew.Core.Domain.Models;

/// <summary>
/// Represents a GUID generation history record
/// </summary>
public class GuidHistory
{
    /// <summary>
    /// Unique identifier for the history record
    /// </summary>
    [Key]
    public int Id { get; set; }

    /// <summary>
    /// The generated GUID value
    /// </summary>
    [Required]
    public string GuidValue { get; set; } = string.Empty;

    /// <summary>
    /// Date and time when the GUID was generated
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Optional notes or description
    /// </summary>
    public string? Notes { get; set; }
}
