using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace DevCrew.Desktop.Services;

public interface IClipboardService
{
    Task<bool> TrySetTextAsync(string text);
}

public sealed class ClipboardService : IClipboardService
{
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
