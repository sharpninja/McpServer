using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>Request for context search.</summary>
public sealed class ContextSearchRequest
{
    /// <summary>Search query text.</summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>Filter by source type.</summary>
    [JsonPropertyName("sourceType")]
    public string? SourceType { get; set; }

    /// <summary>Maximum results to return (default 20).</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 20;
}

/// <summary>Result of a context search.</summary>
public sealed class ContextSearchResult
{
    /// <summary>The original query.</summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>Matching chunks.</summary>
    [JsonPropertyName("chunks")]
    public IReadOnlyList<ContextChunkResult> Chunks { get; set; } = [];

    /// <summary>Source keys of matching documents.</summary>
    [JsonPropertyName("sourceKeys")]
    public IReadOnlyList<string> SourceKeys { get; set; } = [];
}

/// <summary>A chunk returned from context search.</summary>
public sealed class ContextChunkResult
{
    /// <summary>Chunk identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Parent document identifier.</summary>
    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>Chunk text content.</summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>Token count of this chunk.</summary>
    [JsonPropertyName("tokenCount")]
    public int TokenCount { get; set; }

    /// <summary>Chunk index within the document.</summary>
    [JsonPropertyName("chunkIndex")]
    public int ChunkIndex { get; set; }

    /// <summary>Relevance score.</summary>
    [JsonPropertyName("score")]
    public double Score { get; set; }
}

/// <summary>Request for a context pack.</summary>
public sealed class ContextPackRequest
{
    /// <summary>Deterministic query identifier for reproducibility.</summary>
    [JsonPropertyName("queryId")]
    public string? QueryId { get; set; }

    /// <summary>Search query text.</summary>
    [JsonPropertyName("query")]
    public string? Query { get; set; }

    /// <summary>Maximum chunks to include (default 20).</summary>
    [JsonPropertyName("limit")]
    public int Limit { get; set; } = 20;
}

/// <summary>A deterministic context pack.</summary>
public sealed class ContextPack
{
    /// <summary>Deterministic query identifier.</summary>
    [JsonPropertyName("queryId")]
    public string QueryId { get; set; } = string.Empty;

    /// <summary>Ordered chunks.</summary>
    [JsonPropertyName("chunks")]
    public IReadOnlyList<ContextChunkResult> Chunks { get; set; } = [];

    /// <summary>Source keys referenced.</summary>
    [JsonPropertyName("sourceKeys")]
    public IReadOnlyList<string> SourceKeys { get; set; } = [];
}

/// <summary>An indexed document source.</summary>
public sealed class ContextSource
{
    /// <summary>Source key identifier.</summary>
    [JsonPropertyName("sourceKey")]
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>Source type (e.g. repo, session-log).</summary>
    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = string.Empty;

    /// <summary>When the source was last ingested.</summary>
    [JsonPropertyName("ingestedAt")]
    public string? IngestedAt { get; set; }
}
