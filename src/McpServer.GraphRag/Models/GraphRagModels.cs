namespace McpServer.Support.Mcp.Models;

#pragma warning disable CS1591

/// <summary>Query request for GraphRAG retrieval.</summary>
public sealed class GraphRagQueryRequest
{
    /// <summary>TR-MCP-GRAPHRAG-GLOBAL-001: Storage scope for the query target.</summary>
    public GraphRagStorageScope Scope { get; set; } = GraphRagStorageScope.Workspace;

    public string? Query { get; set; }
    public string? Mode { get; set; }
    public int? MaxChunks { get; set; }
    public bool IncludeContextChunks { get; set; } = true;
    public int? MaxEntities { get; set; }
    public int? MaxRelationships { get; set; }
    public int? CommunityDepth { get; set; }
    public int? ResponseTokenBudget { get; set; }
}

/// <summary>Index request for GraphRAG.</summary>
public sealed class GraphRagIndexRequest
{
    /// <summary>TR-MCP-GRAPHRAG-GLOBAL-001: Storage scope for the index target.</summary>
    public GraphRagStorageScope Scope { get; set; } = GraphRagStorageScope.Workspace;

    public bool Force { get; set; }
}

/// <summary>Status payload for GraphRAG readiness and workspace state.</summary>
public sealed class GraphRagStatusResponse
{
    public bool Enabled { get; set; }

    /// <summary>TR-MCP-GRAPHRAG-GLOBAL-001: Storage scope for this status payload.</summary>
    public GraphRagStorageScope Scope { get; set; } = GraphRagStorageScope.Workspace;

    public string WorkspacePath { get; set; } = string.Empty;
    public string GraphRoot { get; set; } = string.Empty;
    public string State { get; set; } = "disabled";
    public bool IsInitialized { get; set; }
    public bool IsIndexed { get; set; }
    public DateTimeOffset? LastIndexedAtUtc { get; set; }
    public DateTimeOffset? LastSuccessAtUtc { get; set; }
    public DateTimeOffset? LastFailureAtUtc { get; set; }
    public string? ActiveJobId { get; set; }
    public string? FailureCode { get; set; }
    public string? LastError { get; set; }
    public string ArtifactVersion { get; set; } = "v1";
    public long? LastIndexDurationMs { get; set; }
    public int? LastIndexedDocumentCount { get; set; }
    public string Backend { get; set; } = "internal-fallback";
    public string IndexCorpus { get; set; } = "graphrag-input";
    public string QueryCorpus { get; set; } = "context-search";
    public string InputPath { get; set; } = string.Empty;
    public int InputDocumentCount { get; set; }
    public string? VisibilityNote { get; set; }
}

/// <summary>Citation payload from GraphRAG query responses.</summary>
public sealed class GraphRagCitation
{
    public string SourceKey { get; set; } = string.Empty;
    public string? ChunkId { get; set; }
    public string? Snippet { get; set; }
}

/// <summary>Query response for GraphRAG operations.</summary>
public sealed class GraphRagQueryResponse
{
    /// <summary>TR-MCP-GRAPHRAG-GLOBAL-001: Storage scope used for the query.</summary>
    public GraphRagStorageScope Scope { get; set; } = GraphRagStorageScope.Workspace;

    public string Query { get; set; } = string.Empty;
    public string Mode { get; set; } = "local";
    public string Answer { get; set; } = string.Empty;
    public IReadOnlyList<GraphRagCitation> Citations { get; set; } = [];
    public IReadOnlyList<ContextChunk> Chunks { get; set; } = [];
    public IReadOnlyList<string> SourceKeys { get; set; } = [];
    public IReadOnlyList<string> Entities { get; set; } = [];
    public IReadOnlyList<string> Relationships { get; set; } = [];
    public IReadOnlyList<string> Communities { get; set; } = [];
    public bool FallbackUsed { get; set; }
    public string? FallbackReason { get; set; }
    public string? FailureCode { get; set; }
    public string Backend { get; set; } = "internal-fallback";
    public string QueryCorpus { get; set; } = "context-search";
    public string? VisibilityNote { get; set; }
}

// ────────────────────────────────────────────────
//  Ad-Hoc Management DTOs (FR-MCP-078/079/080)
// ────────────────────────────────────────────────

/// <summary>FR-MCP-078, TR-GRAPHRAG-ADHOC-001: Request to ingest raw text into the GraphRAG corpus.</summary>
public sealed class GraphRagIngestTextRequest
{
    /// <summary>The text content to ingest.</summary>
    public required string Content { get; set; }

    /// <summary>Optional title for the ingested document.</summary>
    public string? Title { get; set; }

    /// <summary>Source type classification (defaults to "adhoc-text").</summary>
    public string? SourceType { get; set; }

    /// <summary>Source key / path for the document (defaults to Title or generated document ID).</summary>
    public string? SourceKey { get; set; }

    /// <summary>When <see langword="true"/>, triggers a full reindex after ingestion.</summary>
    public bool TriggerReindex { get; set; }
}

/// <summary>FR-MCP-078, TR-GRAPHRAG-ADHOC-001: Response after ingesting raw text.</summary>
public sealed class GraphRagIngestTextResponse
{
    /// <summary>Generated document identifier.</summary>
    public required string DocumentId { get; set; }

    /// <summary>Number of chunks the text was split into.</summary>
    public int ChunkCount { get; set; }

    /// <summary>Total estimated token count across all chunks.</summary>
    public int TokenCount { get; set; }

    /// <summary>Resolved source type used for the document.</summary>
    public required string SourceType { get; set; }

    /// <summary>Resolved source key used for the document.</summary>
    public required string SourceKey { get; set; }

    /// <summary>Whether a reindex was triggered after ingestion.</summary>
    public bool ReindexTriggered { get; set; }
}

/// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Request to create or update a graph entity.</summary>
public sealed class GraphEntityRequest
{
    /// <summary>Display name of the entity.</summary>
    public required string Name { get; set; }

    /// <summary>Entity classification (e.g. "person", "organization", "concept").</summary>
    public required string EntityType { get; set; }

    /// <summary>Optional free-text description of the entity.</summary>
    public string? Description { get; set; }

    /// <summary>Optional JSON metadata blob.</summary>
    public string? Metadata { get; set; }
}

/// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Response representing a single graph entity.</summary>
public sealed class GraphEntityResponse
{
    /// <summary>Unique entity identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Display name of the entity.</summary>
    public required string Name { get; set; }

    /// <summary>Entity classification type.</summary>
    public required string EntityType { get; set; }

    /// <summary>Free-text description.</summary>
    public string? Description { get; set; }

    /// <summary>JSON metadata blob.</summary>
    public string? Metadata { get; set; }

    /// <summary>UTC timestamp when the entity was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the entity was last modified.</summary>
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Paginated list of graph entities.</summary>
public sealed class GraphEntityListResponse
{
    /// <summary>The entities in this page.</summary>
    public required IReadOnlyList<GraphEntityResponse> Entities { get; set; }

    /// <summary>Total number of entities matching the query.</summary>
    public int TotalCount { get; set; }
}

/// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Request to create or update a graph relationship.</summary>
public sealed class GraphRelationshipRequest
{
    /// <summary>Source entity identifier.</summary>
    public required string SourceEntityId { get; set; }

    /// <summary>Target entity identifier.</summary>
    public required string TargetEntityId { get; set; }

    /// <summary>Relationship classification type.</summary>
    public required string RelationshipType { get; set; }

    /// <summary>Optional description of the relationship.</summary>
    public string? Description { get; set; }

    /// <summary>Numeric weight/strength (default 1.0).</summary>
    public double Weight { get; set; } = 1.0;

    /// <summary>Optional JSON metadata blob.</summary>
    public string? Metadata { get; set; }
}

/// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Response representing a single graph relationship.</summary>
public sealed class GraphRelationshipResponse
{
    /// <summary>Unique relationship identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Source entity identifier.</summary>
    public required string SourceEntityId { get; set; }

    /// <summary>Target entity identifier.</summary>
    public required string TargetEntityId { get; set; }

    /// <summary>Relationship classification type.</summary>
    public required string RelationshipType { get; set; }

    /// <summary>Free-text description.</summary>
    public string? Description { get; set; }

    /// <summary>Numeric weight/strength.</summary>
    public double Weight { get; set; }

    /// <summary>JSON metadata blob.</summary>
    public string? Metadata { get; set; }

    /// <summary>UTC timestamp when the relationship was created.</summary>
    public DateTime CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the relationship was last modified.</summary>
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Paginated list of graph relationships.</summary>
public sealed class GraphRelationshipListResponse
{
    /// <summary>The relationships in this page.</summary>
    public required IReadOnlyList<GraphRelationshipResponse> Relationships { get; set; }

    /// <summary>Total number of relationships matching the query.</summary>
    public int TotalCount { get; set; }
}

/// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Summary of a document stored in the GraphRAG corpus.</summary>
public sealed class GraphRagDocumentSummary
{
    /// <summary>Unique document identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Source type classification.</summary>
    public required string SourceType { get; set; }

    /// <summary>Source key / path.</summary>
    public required string SourceKey { get; set; }

    /// <summary>UTC timestamp when the document was ingested.</summary>
    public DateTime IngestedAt { get; set; }

    /// <summary>SHA-256 content hash.</summary>
    public required string ContentHash { get; set; }

    /// <summary>Number of chunks the document was split into.</summary>
    public int ChunkCount { get; set; }

    /// <summary>Total estimated token count across all chunks.</summary>
    public int TotalTokens { get; set; }
}

/// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Paginated list of GraphRAG documents.</summary>
public sealed class GraphRagDocumentListResponse
{
    /// <summary>The documents in this page.</summary>
    public required IReadOnlyList<GraphRagDocumentSummary> Documents { get; set; }

    /// <summary>Total number of documents matching the query.</summary>
    public int TotalCount { get; set; }
}

/// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Response after deleting a document.</summary>
public sealed class GraphRagDocumentDeleteResponse
{
    /// <summary>Deleted document identifier.</summary>
    public required string DocumentId { get; set; }

    /// <summary>Number of chunks removed.</summary>
    public int ChunksRemoved { get; set; }

    /// <summary>Whether the deletion was successful.</summary>
    public bool Success { get; set; }
}

/// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Response containing a document's chunks.</summary>
public sealed class GraphRagDocumentChunksResponse
{
    /// <summary>Parent document identifier.</summary>
    public required string DocumentId { get; set; }

    /// <summary>Ordered list of chunk items.</summary>
    public required IReadOnlyList<GraphRagDocumentChunkItem> Chunks { get; set; }

    /// <summary>Total number of chunks in the document.</summary>
    public int TotalChunks { get; set; }
}

/// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: A single chunk within a document.</summary>
public sealed class GraphRagDocumentChunkItem
{
    /// <summary>Unique chunk identifier.</summary>
    public required string Id { get; set; }

    /// <summary>Chunk text content.</summary>
    public required string Content { get; set; }

    /// <summary>Estimated token count.</summary>
    public int TokenCount { get; set; }

    /// <summary>Zero-based index within the parent document.</summary>
    public int ChunkIndex { get; set; }
}

#pragma warning restore CS1591
