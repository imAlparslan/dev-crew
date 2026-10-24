namespace DevCrew.Mcp.Tools;

internal static class McpToolResponse
{
    public static object Success(object? data, string operation, object? details = null)
        => new
        {
            success = true,
            error = (string?)null,
            data,
            meta = new Dictionary<string, object?>
            {
                ["operation"] = operation,
                ["details"] = details
            }
        };

    public static object Failure(string error, string operation, object? data = null)
        => new
        {
            success = false,
            error,
            data,
            meta = new
            {
                operation
            }
        };
}
