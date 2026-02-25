namespace DevCrew.Core.Services;

/// <summary>
/// Service for clipboard operations.
/// </summary>
public interface IClipboardService
{
    /// <summary>
    /// Attempts to set text content to the clipboard.
    /// </summary>
    /// <param name="text">Text to copy to clipboard.</param>
    /// <returns>True if successful, false otherwise.</returns>
    Task<bool> TrySetTextAsync(string text);
}
