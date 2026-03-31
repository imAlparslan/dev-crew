using System.Text.Json;
using DevCrew.Core.Domain.Results;
using DevCrew.Core.Shared.Constants;

namespace DevCrew.Core.Application.Services;

/// <summary>
/// Default implementation of JSON formatter service
/// </summary>
public class JsonFormatterService : IJsonFormatterService
{
    /// <summary>
    /// Validates if the input is valid JSON
    /// </summary>
    public JsonFormatterResult Validate(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new JsonFormatterResult
            {
                IsValid = false,
                ErrorMessage = "JSON girişi boş olamaz",
                ErrorKey = ErrorKeys.JsonFormatter.InputRequired
            };
        }

        try
        {
            using var document = JsonDocument.Parse(input);
            return new JsonFormatterResult
            {
                IsValid = true,
                Output = input
            };
        }
        catch (JsonException ex)
        {
            return new JsonFormatterResult
            {
                IsValid = false,
                ErrorMessage = $"Geçersiz JSON: {ex.Message}",
                ErrorKey = ErrorKeys.JsonFormatter.InvalidJson,
                ErrorArgs = [ex.Message]
            };
        }
    }

    /// <summary>
    /// Formats JSON with indentation (prettify)
    /// </summary>
    public JsonFormatterResult Prettify(string input, bool sortKeys = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new JsonFormatterResult
            {
                IsValid = false,
                ErrorMessage = "JSON girişi boş olamaz",
                ErrorKey = ErrorKeys.JsonFormatter.InputRequired
            };
        }

        try
        {
            using var document = JsonDocument.Parse(input);
            var elementToSerialize = sortKeys ? SortJsonElement(document.RootElement) : (object)document.RootElement;
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var formatted = JsonSerializer.Serialize(elementToSerialize, options);
            
            return new JsonFormatterResult
            {
                IsValid = true,
                Output = formatted
            };
        }
        catch (JsonException ex)
        {
            return new JsonFormatterResult
            {
                IsValid = false,
                ErrorMessage = $"Geçersiz JSON: {ex.Message}",
                ErrorKey = ErrorKeys.JsonFormatter.InvalidJson,
                ErrorArgs = [ex.Message]
            };
        }
    }

    /// <summary>
    /// Minifies JSON by removing whitespace
    /// </summary>
    public JsonFormatterResult Minify(string input, bool sortKeys = false)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new JsonFormatterResult
            {
                IsValid = false,
                ErrorMessage = "JSON girişi boş olamaz",
                ErrorKey = ErrorKeys.JsonFormatter.InputRequired
            };
        }

        try
        {
            using var document = JsonDocument.Parse(input);
            var elementToSerialize = sortKeys ? SortJsonElement(document.RootElement) : (object)document.RootElement;
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = false
            };
            var minified = JsonSerializer.Serialize(elementToSerialize, options);
            
            return new JsonFormatterResult
            {
                IsValid = true,
                Output = minified
            };
        }
        catch (JsonException ex)
        {
            return new JsonFormatterResult
            {
                IsValid = false,
                ErrorMessage = $"Geçersiz JSON: {ex.Message}",
                ErrorKey = ErrorKeys.JsonFormatter.InvalidJson,
                ErrorArgs = [ex.Message]
            };
        }
    }

    /// <summary>
    /// Sorts JSON object keys alphabetically
    /// </summary>
    public JsonFormatterResult SortKeys(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return new JsonFormatterResult
            {
                IsValid = false,
                ErrorMessage = "JSON girişi boş olamaz",
                ErrorKey = ErrorKeys.JsonFormatter.InputRequired
            };
        }

        try
        {
            using var document = JsonDocument.Parse(input);
            var sorted = SortJsonElement(document.RootElement);
            
            var options = new JsonSerializerOptions
            {
                WriteIndented = true
            };
            var sortedJson = JsonSerializer.Serialize(sorted, options);
            
            return new JsonFormatterResult
            {
                IsValid = true,
                Output = sortedJson
            };
        }
        catch (JsonException ex)
        {
            return new JsonFormatterResult
            {
                IsValid = false,
                ErrorMessage = $"Geçersiz JSON: {ex.Message}",
                ErrorKey = ErrorKeys.JsonFormatter.InvalidJson,
                ErrorArgs = [ex.Message]
            };
        }
    }

    /// <summary>
    /// Recursively sorts JSON object keys
    /// </summary>
    private object? SortJsonElement(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var sortedDict = new SortedDictionary<string, object?>();
                foreach (var property in element.EnumerateObject())
                {
                    sortedDict[property.Name] = SortJsonElement(property.Value);
                }
                return sortedDict;

            case JsonValueKind.Array:
                var array = new List<object?>();
                foreach (var item in element.EnumerateArray())
                {
                    array.Add(SortJsonElement(item));
                }
                return array;

            case JsonValueKind.String:
                return element.GetString();

            case JsonValueKind.Number:
                if (element.TryGetInt32(out int intValue))
                    return intValue;
                if (element.TryGetInt64(out long longValue))
                    return longValue;
                return element.GetDouble();

            case JsonValueKind.True:
                return true;

            case JsonValueKind.False:
                return false;

            case JsonValueKind.Null:
                return null;

            default:
                return null;
        }
    }
}
