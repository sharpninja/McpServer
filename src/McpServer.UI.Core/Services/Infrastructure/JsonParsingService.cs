using System;
using System.Text.Json;
using System.Text.Json.Nodes;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Services.Infrastructure;

/// <summary>
/// Host implementation of <see cref="IJsonParsingService"/> backed by <see cref="System.Text.Json"/>.
/// </summary>
public sealed class JsonParsingService : IJsonParsingService
{
    private static readonly JsonSerializerOptions IndentedOptions = new() { WriteIndented = true };

    public JsonTreeResult ParseToTree(string jsonText)
    {
        var node = JsonNode.Parse(jsonText);
        int count = CountNodes(node);
        return new JsonTreeResult(node!, count);
    }

    public string? Validate(string jsonText)
    {
        try
        {
            using var doc = JsonDocument.Parse(jsonText);
            return null;
        }
        catch (JsonException ex)
        {
            return ex.Message;
        }
    }

    public string PrettyPrint(string jsonText)
    {
        var node = JsonNode.Parse(jsonText);
        return node?.ToJsonString(IndentedOptions) ?? jsonText;
    }

    private static int CountNodes(JsonNode? node)
    {
        if (node is null) return 0;

        int count = 1;
        if (node is JsonObject obj)
        {
            foreach (var kvp in obj)
                count += CountNodes(kvp.Value);
        }
        else if (node is JsonArray arr)
        {
            foreach (var item in arr)
                count += CountNodes(item);
        }
        return count;
    }
}

