using System.Globalization;
using System.ComponentModel;

namespace DevCrew.Desktop.Services;

public sealed record SupportedLanguage(string CultureName, string DisplayName);

public interface ILocalizationService : INotifyPropertyChanged
{
    event EventHandler? LanguageChanged;

    IReadOnlyList<SupportedLanguage> SupportedLanguages { get; }

    CultureInfo CurrentCulture { get; }

    string this[string key] { get; }

    bool SetLanguage(string cultureName);

    string GetString(string key);

    string GetString(string key, params object[] args);

    string GetStringOrFallback(string? key, string fallback, params object[] args);
}
