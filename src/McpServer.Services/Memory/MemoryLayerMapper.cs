using System.Text.Json;
using McpServer.Support.Mcp.Storage.Entities;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-010 / TR-MCP-MEMORY-MODEL-002: Maps multi-layer memory fields and tags JSON.
/// </summary>
public static class MemoryLayerMapper
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    /// <summary>Serializes tags. Null and empty become an empty array. Duplicates are de-duplicated in order.</summary>
    public static string SerializeTags(IReadOnlyList<string>? tags)
    {
        var normalized = NormalizeTags(tags);
        return JsonSerializer.Serialize(normalized, JsonOptions);
    }

    /// <summary>Deserializes tags JSON. Null/invalid payloads become empty.</summary>
    public static IReadOnlyList<string> DeserializeTags(string? tagsJson)
    {
        if (string.IsNullOrWhiteSpace(tagsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<List<string>>(tagsJson, JsonOptions) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    /// <summary>De-duplicates tags while preserving first-seen ordinal values.</summary>
    public static IReadOnlyList<string> NormalizeTags(IReadOnlyList<string>? tags)
    {
        if (tags is null || tags.Count == 0)
            return [];

        var seen = new HashSet<string>(StringComparer.Ordinal);
        var result = new List<string>();
        foreach (var tag in tags)
        {
            if (string.IsNullOrWhiteSpace(tag))
                continue;
            var trimmed = tag.Trim();
            if (seen.Add(trimmed))
                result.Add(trimmed);
        }

        return result;
    }

    /// <summary>Effective content: persisted Content or legacy Text backfill.</summary>
    public static string EffectiveContent(MemoryEntity entity)
        => string.IsNullOrEmpty(entity.Content) ? entity.Text : entity.Content;

    /// <summary>Maps an entity to the public memory item including multi-layer fields.</summary>
    public static MemoryItem ToItem(MemoryEntity entity)
    {
        var content = EffectiveContent(entity);
        return new MemoryItem
        {
            Id = entity.Id,
            Category = entity.Category,
            Scope = string.Equals(entity.Scope, MemoryEntity.GlobalScope, StringComparison.Ordinal)
                ? MemoryScope.Global
                : MemoryScope.Workspace,
            WorkspacePath = entity.WorkspaceId,
            Text = entity.Text,
            Version = entity.Version,
            CreatedAtUtc = entity.CreatedAtUtc,
            UpdatedAtUtc = entity.UpdatedAtUtc,
            UpdatedBy = entity.UpdatedBy,
            Title = entity.Title,
            Summary = entity.Summary,
            Content = content,
            Type = entity.Type,
            Tags = DeserializeTags(entity.Tags),
            Confidence = entity.Confidence,
            SourceKind = entity.SourceKind,
            SourceRef = entity.SourceRef,
            CreatedBy = entity.CreatedBy,
        };
    }
}
