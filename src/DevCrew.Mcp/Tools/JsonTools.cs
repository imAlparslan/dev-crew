using System.ComponentModel;
using DevCrew.Core.Application.Services;
using DevCrew.Core.Domain.Results;
using ModelContextProtocol.Server;

namespace DevCrew.Mcp.Tools;

internal sealed class JsonTools(IJsonFormatterService formatter, IJsonDiffService differ)
{
    private readonly IJsonFormatterService _formatter = formatter;
    private readonly IJsonDiffService _differ = differ;

    [McpServerTool]
    [Description("Validates whether a string contains valid JSON.")]
    public object ValidateJson(
        [Description("JSON text to validate.")] string input)
    {
        var result = _formatter.Validate(input);
        return result.IsValid
            ? McpToolResponse.Success(new { isValid = true }, "json_validate")
            : McpToolResponse.Failure(result.ErrorMessage ?? "Invalid JSON.", "json_validate",
                new { isValid = false, result.ErrorKey, result.ErrorArgs });
    }

    [McpServerTool]
    [Description("Prettifies or minifies JSON and can sort object keys.")]
    public object FormatJson(
        [Description("JSON text to format.")] string input,
        [Description("Output mode: 'prettify' or 'minify'.")] string mode = "prettify",
        [Description("Sort object keys alphabetically.")] bool sortKeys = false)
    {
        var normalizedMode = mode?.Trim().ToLowerInvariant();
        var result = normalizedMode switch
        {
            "prettify" => _formatter.Prettify(input, sortKeys),
            "minify" => _formatter.Minify(input, sortKeys),
            _ => null
        };

        if (result is null)
        {
            return McpToolResponse.Failure("Mode must be 'prettify' or 'minify'.", "json_format");
        }

        return result.IsValid
            ? McpToolResponse.Success(new { output = result.Output, mode = normalizedMode, sortKeys }, "json_format")
            : McpToolResponse.Failure(result.ErrorMessage ?? "Unable to format JSON.", "json_format",
                new { result.ErrorKey, result.ErrorArgs });
    }

    [McpServerTool]
    [Description("Compares two JSON values and returns summary, path-level differences, and display JSON.")]
    public object CompareJson(
        [Description("First JSON document.")] string leftJson,
        [Description("Second JSON document.")] string rightJson,
        [Description("Ignore object property ordering.")] bool ignoreObjectPropertyOrder = true,
        [Description("Treat array ordering as significant.")] bool treatArrayOrderAsSignificant = true,
        [Description("Ignore whitespace-only differences.")] bool ignoreWhitespaceDifferences = true,
        [Description("Treat null and empty string as equal.")] bool treatNullAndEmptyStringAsEqual = false)
    {
        var result = _differ.Compare(leftJson, rightJson, new JsonDiffOptions
        {
            IgnoreObjectPropertyOrder = ignoreObjectPropertyOrder,
            TreatArrayOrderAsSignificant = treatArrayOrderAsSignificant,
            IgnoreWhitespaceDifferences = ignoreWhitespaceDifferences,
            TreatNullAndEmptyStringAsEqual = treatNullAndEmptyStringAsEqual
        });

        return result.IsValid
            ? McpToolResponse.Success(new
            {
                result.Summary,
                result.PathDiffs,
                result.LineDiffs,
                result.LeftDisplayJson,
                result.RightDisplayJson
            }, "json_diff")
            : McpToolResponse.Failure(result.ErrorMessage ?? "Unable to compare JSON.", "json_diff",
                new { result.ErrorKey, result.ErrorArgs });
    }
}
