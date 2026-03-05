using System.Collections.Generic;
using System.Text.Json.Serialization;

#pragma warning disable CS1591

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

/// <summary>Request for GraphRAG query.</summary>
public sealed class GraphRagQueryRequest
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    [JsonPropertyName("maxChunks")]
    public int? MaxChunks { get; set; }

    [JsonPropertyName("includeContextChunks")]
    public bool IncludeContextChunks { get; set; } = true;
}

/// <summary>Request for GraphRAG index operation.</summary>
public sealed class GraphRagIndexRequest
{
    [JsonPropertyName("force")]
    public bool Force { get; set; }
}

/// <summary>GraphRAG status response.</summary>
public sealed class GraphRagStatusResult
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;

    [JsonPropertyName("graphRoot")]
    public string GraphRoot { get; set; } = string.Empty;

    [JsonPropertyName("isInitialized")]
    public bool IsInitialized { get; set; }

    [JsonPropertyName("isIndexed")]
    public bool IsIndexed { get; set; }

    [JsonPropertyName("lastIndexedAtUtc")]
    public string? LastIndexedAtUtc { get; set; }

    [JsonPropertyName("lastError")]
    public string? LastError { get; set; }

    [JsonPropertyName("backend")]
    public string Backend { get; set; } = string.Empty;
}

/// <summary>GraphRAG citation entry.</summary>
public sealed class GraphRagCitation
{
    [JsonPropertyName("sourceKey")]
    public string SourceKey { get; set; } = string.Empty;

    [JsonPropertyName("chunkId")]
    public string? ChunkId { get; set; }

    [JsonPropertyName("snippet")]
    public string? Snippet { get; set; }
}

/// <summary>GraphRAG query response.</summary>
public sealed class GraphRagQueryResult
{
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;

    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    [JsonPropertyName("citations")]
    public IReadOnlyList<GraphRagCitation> Citations { get; set; } = [];

    [JsonPropertyName("chunks")]
    public IReadOnlyList<ContextChunkResult> Chunks { get; set; } = [];

    [JsonPropertyName("sourceKeys")]
    public IReadOnlyList<string> SourceKeys { get; set; } = [];

    [JsonPropertyName("fallbackUsed")]
    public bool FallbackUsed { get; set; }

    [JsonPropertyName("backend")]
    public string Backend { get; set; } = string.Empty;
}

#pragma warning restore CS1591
