using Avalonia;
using Avalonia.Media;
using DevCrew.Core.Models;

namespace DevCrew.Desktop.Services;

public class FontService : IFontService
{
    // Base sizes at Medium (1.0x scale)
    private static readonly Dictionary<string, double> BaseSizes = new()
    {
        ["FontSizeSmall"]   = 11,
        ["FontSizeDefault"] = 12,
        ["FontSizeLabel"]   = 13,
        ["FontSizeMedium"]  = 14,
        ["FontSizeLarge"]   = 16,
        ["FontSizeHeading"] = 20,
        ["FontSizeXLarge"]  = 32,
    };

    private static readonly Dictionary<string, double> Scales = new()
    {
        ["Small"]  = 0.85,
        ["Medium"] = 1.0,
        ["Large"]  = 1.2,
    };

    public string CurrentFontSizePreference { get; private set; } = AppSettings.DefaultFontSizePreference;
    public string CurrentUiFontFamily { get; private set; } = AppSettings.DefaultUiFontFamily;
    public string CurrentContentFontFamily { get; private set; } = AppSettings.DefaultContentFontFamily;

    public IReadOnlyList<string> FontSizeOptions { get; } = ["Small", "Medium", "Large"];

    public IReadOnlyList<FontOption> AvailableUiFonts { get; }

    public IReadOnlyList<FontOption> AvailableContentFonts { get; } =
    [
        new("Consolas",       "Consolas",       "Consolas, Courier New, monospace"),
        new("CourierNew",     "Courier New",    "Courier New, monospace"),
        new("SystemDefault",  "System Default", "monospace"),
    ];

    public FontService()
    {
        var systemFonts = FontManager.Current.SystemFonts
            .Select(f => f.Name)
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .Select(name => new FontOption(name, name, name))
            .ToList();

        var result = new List<FontOption>(systemFonts.Count + 1)
        {
            new("SystemDefault", "System Default", "sans-serif")
        };
        result.AddRange(systemFonts);
        AvailableUiFonts = result.AsReadOnly();
    }

    public void ApplyFontSettings(string fontSizePreference, string uiFontFamily, string contentFontFamily)
    {
        CurrentFontSizePreference = fontSizePreference;
        CurrentUiFontFamily = uiFontFamily;
        CurrentContentFontFamily = contentFontFamily;

        var scale = Scales.TryGetValue(fontSizePreference, out var s) ? s : 1.0;
        var uiFont = ResolveUiFontFamily(uiFontFamily);
        var contentFont = ResolveContentFontFamily(contentFontFamily);

        var resources = Application.Current!.Resources;

        foreach (var kv in BaseSizes)
        {
            resources[kv.Key] = Math.Round(kv.Value * scale);
        }

        resources["FontDefault"] = uiFont;
        resources["FontHeading"] = uiFont;
        resources["FontMonospace"] = contentFont;
    }

    private FontFamily ResolveUiFontFamily(string key)
    {
        foreach (var opt in AvailableUiFonts)
        {
            if (opt.Key == key)
                return new FontFamily(opt.FontFamilyValue);
        }
        // Fall back to treating the key directly as a font family name
        return new FontFamily(key);
    }

    private FontFamily ResolveContentFontFamily(string key)
    {
        foreach (var opt in AvailableContentFonts)
        {
            if (opt.Key == key)
                return new FontFamily(opt.FontFamilyValue);
        }
        return new FontFamily(AvailableContentFonts[0].FontFamilyValue);
    }
}
