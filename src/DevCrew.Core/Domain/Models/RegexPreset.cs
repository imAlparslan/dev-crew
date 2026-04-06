namespace DevCrew.Core.Domain.Models;

/// <summary>
/// Represents a saved regex configuration that can be reused later.
/// </summary>
public class RegexPreset
{
    /// <summary>
    /// Gets or sets the preset ID.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the preset display name.
    /// </summary>
    public required string Name { get; set; }

    /// <summary>
    /// Gets or sets the regex pattern.
    /// </summary>
    public required string Pattern { get; set; }

    /// <summary>
    /// Gets or sets whether the regex ignores case.
    /// </summary>
    public bool IgnoreCase { get; set; }

    /// <summary>
    /// Gets or sets whether the regex uses multiline mode.
    /// </summary>
    public bool Multiline { get; set; }

    /// <summary>
    /// Gets or sets the creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the last time the preset was applied.
    /// </summary>
    public DateTime? LastUsedAt { get; set; }
}