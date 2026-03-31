using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using DevCrew.Core.Application.Services;

namespace DevCrew.Desktop.Services;

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
