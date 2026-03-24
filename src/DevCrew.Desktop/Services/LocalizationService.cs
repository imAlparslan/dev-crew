using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Resources;

namespace DevCrew.Desktop.Services;

public sealed class LocalizationService : ILocalizationService
{
    private const string FallbackCulture = "en-US";
    private static readonly ResourceManager ResourceManager = new("DevCrew.Desktop.Lang.Resources", typeof(LocalizationService).Assembly);

    private static readonly IReadOnlyList<SupportedLanguage> Supported = new ReadOnlyCollection<SupportedLanguage>(
    [
        new("tr-TR", "Türkçe"),
        new("en-US", "English"),
        new("de-DE", "Deutsch"),
        new("fr-FR", "Français")
    ]);

    public event EventHandler? LanguageChanged;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IReadOnlyList<SupportedLanguage> SupportedLanguages => Supported;

    public CultureInfo CurrentCulture { get; private set; }

    public string this[string key] => GetString(key);

    public LocalizationService(string? requestedCulture = null)
    {
        var cultureName = ResolveSupportedCultureName(requestedCulture) ?? FallbackCulture;
        CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
        ApplyCulture(CurrentCulture);
    }

    public static bool IsSupportedCulture(string? cultureName) => ResolveSupportedCultureName(cultureName) is not null;

    public static string ResolveOrFallbackCulture(string? cultureName) => ResolveSupportedCultureName(cultureName) ?? FallbackCulture;

    public bool SetLanguage(string cultureName)
    {
        var resolvedCulture = ResolveSupportedCultureName(cultureName);
        if (resolvedCulture is null)
        {
            return false;
        }

        if (string.Equals(CurrentCulture.Name, resolvedCulture, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        CurrentCulture = CultureInfo.GetCultureInfo(resolvedCulture);
        ApplyCulture(CurrentCulture);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(CurrentCulture)));
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs("Item[]"));
        LanguageChanged?.Invoke(this, EventArgs.Empty);
        return true;
    }

    public string GetString(string key)
    {
        foreach (var cultureName in EnumerateCultureFallback(CurrentCulture.Name))
        {
            var culture = CultureInfo.GetCultureInfo(cultureName);
            var value = ResourceManager.GetString(key, culture);
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }
        }

        return key;
    }

    public string GetString(string key, params object[] args)
    {
        var template = GetString(key);
        if (args.Length == 0)
        {
            return template;
        }

        try
        {
            return string.Format(CurrentCulture, template, args);
        }
        catch (FormatException)
        {
            return template;
        }
    }

    public string GetStringOrFallback(string? key, string fallback, params object[] args)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            return GetString(key, args);
        }

        if (string.IsNullOrWhiteSpace(fallback))
        {
            return GetString("common.error_unknown");
        }

        return fallback;
    }

    private static IEnumerable<string> EnumerateCultureFallback(string cultureName)
    {
        yield return cultureName;

        var separatorIndex = cultureName.IndexOf('-');
        if (separatorIndex > 0)
        {
            yield return cultureName[..separatorIndex];
        }

        if (!string.Equals(cultureName, FallbackCulture, StringComparison.OrdinalIgnoreCase))
        {
            yield return FallbackCulture;
        }
    }

    private static string? ResolveSupportedCultureName(string? cultureName)
    {
        if (string.IsNullOrWhiteSpace(cultureName))
        {
            return null;
        }

        if (Supported.Any(x => string.Equals(x.CultureName, cultureName, StringComparison.OrdinalIgnoreCase)))
        {
            return Supported.First(x => string.Equals(x.CultureName, cultureName, StringComparison.OrdinalIgnoreCase)).CultureName;
        }

        try
        {
            var requested = CultureInfo.GetCultureInfo(cultureName);
            var neutralName = requested.TwoLetterISOLanguageName;

            var match = Supported.FirstOrDefault(x => x.CultureName.StartsWith(neutralName + "-", StringComparison.OrdinalIgnoreCase));
            return match?.CultureName;
        }
        catch (CultureNotFoundException)
        {
            return null;
        }
    }

    private static void ApplyCulture(CultureInfo culture)
    {
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
    }
}

