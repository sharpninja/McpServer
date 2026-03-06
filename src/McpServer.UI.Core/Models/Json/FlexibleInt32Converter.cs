using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.UI.Core.Models.Json;

/// <summary>
/// Deserializes an int from JSON number (int/long/double), string, or null (→ 0).
/// Use when the source may emit e.g. totalTokens as a double or string.
/// </summary>
public sealed class FlexibleInt32Converter : JsonConverter<int>
{
    public override int Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        switch (reader.TokenType)
        {
            case JsonTokenType.Number:
                if (reader.TryGetInt32(out var i)) return i;
                if (reader.TryGetInt64(out var l)) return (int)l;
                if (reader.TryGetDouble(out var d)) return (int)d;
                break;
            case JsonTokenType.String:
                if (reader.GetString() is { } s && int.TryParse(s, out var parsed))
                    return parsed;
                break;
            case JsonTokenType.Null:
                return 0;
        }
        return 0;
    }

    public override void Write(Utf8JsonWriter writer, int value, JsonSerializerOptions options)
    {
        writer.WriteNumberValue(value);
    }
}

