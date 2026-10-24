using System.ComponentModel;
using System.Text;
using DevCrew.Core.Application.Services;
using ModelContextProtocol.Server;

namespace DevCrew.Mcp.Tools;

internal sealed class Base64Tools(IBase64EncoderService encoder)
{
    private readonly IBase64EncoderService _encoder = encoder;

    [McpServerTool]
    [Description("Encodes text content as UTF-8 Base64. Does not access local files or the clipboard.")]
    public object EncodeBase64(
        [Description("Text to encode as UTF-8 bytes.")] string input)
    {
        if (input is null)
        {
            return McpToolResponse.Failure("Input text is required.", "base64_encode");
        }

        var result = _encoder.Encode(Encoding.UTF8.GetBytes(input));
        return result.IsSuccess
            ? McpToolResponse.Success(new { output = result.Output }, "base64_encode")
            : McpToolResponse.Failure(result.ErrorMessage ?? "Unable to encode Base64.", "base64_encode");
    }

    [McpServerTool]
    [Description("Decodes Base64 content and returns the decoded bytes as Base64 plus UTF-8 text when valid.")]
    public object DecodeBase64(
        [Description("Base64 input to decode.")] string input)
    {
        if (input is null)
        {
            return McpToolResponse.Failure("Base64 input is required.", "base64_decode");
        }

        var result = _encoder.Decode(input);
        if (!result.IsSuccess || result.Output is null)
        {
            return McpToolResponse.Failure(result.ErrorMessage ?? "Unable to decode Base64.", "base64_decode");
        }

        string? text = null;
        try
        {
            text = new UTF8Encoding(false, true).GetString(result.Output);
        }
        catch (DecoderFallbackException)
        {
            // Binary output remains available as Base64 when it is not valid UTF-8.
        }

        return McpToolResponse.Success(
            new
            {
                outputBase64 = Convert.ToBase64String(result.Output),
                outputText = text,
                isUtf8 = text is not null
            },
            "base64_decode");
    }
}
