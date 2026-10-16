using System.Text.Json.Nodes;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>
/// TR-MCP-QBRAIN-005: Chooses the appsettings object that stores workspace projection data.
/// Canonical files keep a single QBrainAi section. Legacy files that only have Mcp are updated in place.
/// </summary>
internal static class ProductSettingsDocument
{
    /// <summary>Returns the QBrainAi map when present, otherwise the Mcp map, otherwise a new QBrainAi map.</summary>
    public static IDictionary<object, object> SelectYamlSection(IDictionary<string, object> document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (TryGetYamlSection(document, "QBrainAi", out var canonical))
            return canonical;
        if (TryGetYamlSection(document, "Mcp", out var legacy))
            return legacy;

        var created = new Dictionary<object, object>();
        document["QBrainAi"] = created;
        return created;
    }

    /// <summary>Returns the QBrainAi map when present, otherwise the Mcp map, otherwise a new QBrainAi map.</summary>
    public static IDictionary<object, object> SelectYamlSection(IDictionary<object, object> document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (TryGetObjectSection(document, "QBrainAi", out var canonical))
            return canonical;
        if (TryGetObjectSection(document, "Mcp", out var legacy))
            return legacy;

        var created = new Dictionary<object, object>();
        document["QBrainAi"] = created;
        return created;
    }

    /// <summary>Returns the QBrainAi object when present, otherwise the Mcp object, otherwise a new QBrainAi object.</summary>
    public static JsonObject SelectJsonSection(JsonObject document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (Find(document, "QBrainAi") is JsonObject canonical)
            return canonical;
        if (Find(document, "Mcp") is JsonObject legacy)
            return legacy;

        var created = new JsonObject();
        document["QBrainAi"] = created;
        return created;
    }

    private static bool TryGetYamlSection(IDictionary<string, object> document, string name, out IDictionary<object, object> section)
    {
        foreach (var pair in document)
        {
            if (!string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                continue;
            if (pair.Value is IDictionary<object, object> dictionary)
            {
                section = dictionary;
                return true;
            }
        }

        section = new Dictionary<object, object>();
        return false;
    }

    private static bool TryGetObjectSection(IDictionary<object, object> document, string name, out IDictionary<object, object> section)
    {
        foreach (var pair in document)
        {
            if (pair.Key is not string key || !string.Equals(key, name, StringComparison.OrdinalIgnoreCase))
                continue;
            if (pair.Value is IDictionary<object, object> dictionary)
            {
                section = dictionary;
                return true;
            }
        }

        section = new Dictionary<object, object>();
        return false;
    }

    private static JsonNode? Find(JsonObject document, string name)
    {
        foreach (var pair in document)
        {
            if (string.Equals(pair.Key, name, StringComparison.OrdinalIgnoreCase))
                return pair.Value;
        }

        return null;
    }
}
