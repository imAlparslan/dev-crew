using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

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

/// <summary>
/// Avalonia-based clipboard service implementation.
/// </summary>
public sealed class ClipboardService : IClipboardService
{
    /// <inheritdoc/>
    public async Task<bool> TrySetTextAsync(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return false;
        }

        var desktop = Application.Current?.ApplicationLifetime as IClassicDesktopStyleApplicationLifetime;
        var clipboard = desktop?.MainWindow?.Clipboard;

        if (clipboard == null)
        {
            return false;
        }

        await clipboard.SetTextAsync(text);
        return true;
    }
}
