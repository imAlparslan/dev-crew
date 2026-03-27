namespace DevCrew.Desktop.Services;

public interface IFontService
{
    string CurrentFontSizePreference { get; }
    string CurrentUiFontFamily { get; }
    string CurrentContentFontFamily { get; }

    IReadOnlyList<string> FontSizeOptions { get; }
    IReadOnlyList<FontOption> AvailableUiFonts { get; }
    IReadOnlyList<FontOption> AvailableContentFonts { get; }

    void ApplyFontSettings(string fontSizePreference, string uiFontFamily, string contentFontFamily);
}
