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

/// <summary>Request for website URL ingestion.</summary>
public sealed class WebsiteIngestRequest
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("includeSubpages")]
    public bool IncludeSubpages { get; set; }

    [JsonPropertyName("maxPages")]
    public int MaxPages { get; set; } = 20;

    [JsonPropertyName("maxDepth")]
    public int MaxDepth { get; set; } = 1;

    [JsonPropertyName("maxBytesPerPage")]
    public int MaxBytesPerPage { get; set; } = 262_144;

    [JsonPropertyName("forceRefresh")]
    public bool ForceRefresh { get; set; }

    [JsonPropertyName("triggerGraphRagIndex")]
    public bool TriggerGraphRagIndex { get; set; }
}

/// <summary>Per-URL website ingestion outcome.</summary>
public sealed class WebsiteIngestUrlResult
{
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("sourceKey")]
    public string? SourceKey { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }

    [JsonPropertyName("chunksWritten")]
    public int ChunksWritten { get; set; }
}

/// <summary>Website ingestion response payload.</summary>
public sealed class WebsiteIngestResult
{
    [JsonPropertyName("runId")]
    public string RunId { get; set; } = string.Empty;

    [JsonPropertyName("startedAtUtc")]
    public string? StartedAtUtc { get; set; }

    [JsonPropertyName("completedAtUtc")]
    public string? CompletedAtUtc { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("documentsIngested")]
    public int DocumentsIngested { get; set; }

    [JsonPropertyName("chunksWritten")]
    public int ChunksWritten { get; set; }

    [JsonPropertyName("urlResults")]
    public IReadOnlyList<WebsiteIngestUrlResult> UrlResults { get; set; } = [];

    [JsonPropertyName("graphRagIndexed")]
    public bool GraphRagIndexed { get; set; }

    [JsonPropertyName("graphRagIndexError")]
    public string? GraphRagIndexError { get; set; }
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

    [JsonPropertyName("maxEntities")]
    public int? MaxEntities { get; set; }

    [JsonPropertyName("maxRelationships")]
    public int? MaxRelationships { get; set; }

    [JsonPropertyName("communityDepth")]
    public int? CommunityDepth { get; set; }

    [JsonPropertyName("responseTokenBudget")]
    public int? ResponseTokenBudget { get; set; }
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

    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    [JsonPropertyName("isInitialized")]
    public bool IsInitialized { get; set; }

    [JsonPropertyName("isIndexed")]
    public bool IsIndexed { get; set; }

    [JsonPropertyName("lastIndexedAtUtc")]
    public string? LastIndexedAtUtc { get; set; }

    [JsonPropertyName("lastSuccessAtUtc")]
    public string? LastSuccessAtUtc { get; set; }

    [JsonPropertyName("lastFailureAtUtc")]
    public string? LastFailureAtUtc { get; set; }

    [JsonPropertyName("activeJobId")]
    public string? ActiveJobId { get; set; }

    [JsonPropertyName("failureCode")]
    public string? FailureCode { get; set; }

    [JsonPropertyName("lastError")]
    public string? LastError { get; set; }

    [JsonPropertyName("artifactVersion")]
    public string ArtifactVersion { get; set; } = string.Empty;

    [JsonPropertyName("lastIndexDurationMs")]
    public long? LastIndexDurationMs { get; set; }

    [JsonPropertyName("lastIndexedDocumentCount")]
    public int? LastIndexedDocumentCount { get; set; }

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

    [JsonPropertyName("entities")]
    public IReadOnlyList<string> Entities { get; set; } = [];

    [JsonPropertyName("relationships")]
    public IReadOnlyList<string> Relationships { get; set; } = [];

    [JsonPropertyName("communities")]
    public IReadOnlyList<string> Communities { get; set; } = [];

    [JsonPropertyName("fallbackUsed")]
    public bool FallbackUsed { get; set; }

    [JsonPropertyName("fallbackReason")]
    public string? FallbackReason { get; set; }

    [JsonPropertyName("failureCode")]
    public string? FailureCode { get; set; }

    [JsonPropertyName("backend")]
    public string Backend { get; set; } = string.Empty;
}

#pragma warning restore CS1591
