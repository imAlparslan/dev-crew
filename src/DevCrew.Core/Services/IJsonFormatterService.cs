namespace DevCrew.Core.Services;

using DevCrew.Core.Models.Results;

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
