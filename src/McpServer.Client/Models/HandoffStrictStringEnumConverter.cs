using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>TR-HANDOFF-CONTRACT-001: String-only enum converter. Integer tokens and undefined names fail.</summary>
public sealed class HandoffStrictStringEnumConverter<TEnum> : JsonConverter<TEnum>
    where TEnum : struct, Enum
{
    /// <inheritdoc />
    public override TEnum Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.Number)
            throw new JsonException($"Integer values are not allowed for {typeof(TEnum).Name}.");

        if (reader.TokenType != JsonTokenType.String)
            throw new JsonException($"{typeof(TEnum).Name} must be a string.");

        var text = reader.GetString();
        if (!Enum.TryParse<TEnum>(text, ignoreCase: true, out var value) || !Enum.IsDefined(value))
            throw new JsonException($"'{text}' is not a defined {typeof(TEnum).Name} value.");

        return value;
    }

    /// <inheritdoc />
    public override void Write(Utf8JsonWriter writer, TEnum value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteStringValue(value.ToString());
    }
}
