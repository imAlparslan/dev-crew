namespace DevCrew.Core.Services;

/// <summary>
/// Result of JSON formatting operation
/// </summary>
public record JsonFormatterResult
{
    /// <summary>
    /// Indicates whether the operation was successful
    /// </summary>
    public bool IsValid { get; init; }

    /// <summary>
    /// The formatted JSON output
    /// </summary>
    public string Output { get; init; } = string.Empty;

    /// <summary>
    /// Error message if validation failed
    /// </summary>
    public string? ErrorMessage { get; init; }

    /// <summary>
    /// Language-independent error key for localization.
    /// </summary>
    public string? ErrorKey { get; init; }

    /// <summary>
    /// Optional format arguments for localized error templates.
    /// </summary>
    public object[]? ErrorArgs { get; init; }
}

/// <summary>
/// Service for JSON validation and formatting operations
/// </summary>
public interface IJsonFormatterService
{
    /// <summary>
    /// Validates if the input is valid JSON
    /// </summary>
    /// <param name="input">JSON string to validate</param>
    /// <returns>Result containing validation status and error message if any</returns>
    JsonFormatterResult Validate(string input);

    /// <summary>
    /// Formats JSON with indentation (prettify)
    /// </summary>
    /// <param name="input">JSON string to format</param>
    /// <param name="sortKeys">If true, sorts object keys alphabetically</param>
    /// <returns>Result containing formatted JSON or error message</returns>
    JsonFormatterResult Prettify(string input, bool sortKeys = false);

    /// <summary>
    /// Minifies JSON by removing whitespace
    /// </summary>
    /// <param name="input">JSON string to minify</param>
    /// <param name="sortKeys">If true, sorts object keys alphabetically</param>
    /// <returns>Result containing minified JSON or error message</returns>
    JsonFormatterResult Minify(string input, bool sortKeys = false);
}
