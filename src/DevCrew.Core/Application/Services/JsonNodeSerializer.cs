using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace DevCrew.Core.Application.Services;

internal static class JsonNodeSerializer
{
    public static string Serialize(object? value, bool writeIndented)
    {
        var node = FromObject(value);
        return node?.ToJsonString(new JsonSerializerOptions { WriteIndented = writeIndented }) ?? "null";
    }

    public static string SerializeElement(JsonElement element, bool writeIndented)
    {
        using var stream = new MemoryStream();
        using (var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = writeIndented }))
        {
            element.WriteTo(writer);
        }

        return Encoding.UTF8.GetString(stream.ToArray());
    }

    public static JsonNode? FromObject(object? value)
    {
        switch (value)
        {
            case null:
                return null;

            case JsonElement element:
                return JsonNode.Parse(element.GetRawText());

            case JsonNode node:
                return node;

            case IDictionary<string, object?> map:
            {
                var obj = new JsonObject();
                foreach (var item in map)
                {
                    obj[item.Key] = FromObject(item.Value);
                }

                return obj;
            }

            case IEnumerable<KeyValuePair<string, object>> map:
            {
                var obj = new JsonObject();
                foreach (var item in map)
                {
                    obj[item.Key] = FromObject(item.Value);
                }

                return obj;
            }

            case IEnumerable<object?> sequence:
            {
                var array = new JsonArray();
                foreach (var item in sequence)
                {
                    array.Add(FromObject(item));
                }

                return array;
            }

            case string stringValue:
                return JsonValue.Create(stringValue);

            case bool boolValue:
                return JsonValue.Create(boolValue);

            case int intValue:
                return JsonValue.Create(intValue);

            case long longValue:
                return JsonValue.Create(longValue);

            case double doubleValue:
                return JsonValue.Create(doubleValue);

            case float floatValue:
                return JsonValue.Create(floatValue);

            case decimal decimalValue:
                return JsonValue.Create(decimalValue);

            default:
                return JsonValue.Create(value.ToString());
        }
    }
}