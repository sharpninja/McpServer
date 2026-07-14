namespace McpServer.Support.Mcp.Models;

/// <summary>Query request for GraphRAG retrieval.</summary>
public sealed class GraphRagQueryRequest
{
    /// <summary>TR-MCP-GRAPHRAG-GLOBAL-001: Storage scope for the query target.</summary>
    public GraphRagStorageScope Scope { get; set; } = GraphRagStorageScope.Workspace;

    /// <summary>GraphRAG query text.</summary>
    public string? Query { get; set; }

    /// <summary>GraphRAG query mode.</summary>
    public string? Mode { get; set; }

    /// <summary>Maximum number of chunks to include.</summary>
    public int? MaxChunks { get; set; }

    /// <summary>Whether context chunks should be included in the response.</summary>
    public bool IncludeContextChunks { get; set; } = true;

    /// <summary>Maximum number of entities to include.</summary>
    public int? MaxEntities { get; set; }

    /// <summary>Maximum number of relationships to include.</summary>
    public int? MaxRelationships { get; set; }

    /// <summary>Maximum community traversal depth.</summary>
    public int? CommunityDepth { get; set; }

    /// <summary>Token budget for the generated response.</summary>
    public int? ResponseTokenBudget { get; set; }
}

/// <summary>Index request for GraphRAG.</summary>
public sealed class GraphRagIndexRequest
{
    /// <summary>TR-MCP-GRAPHRAG-GLOBAL-001: Storage scope for the index target.</summary>
    public GraphRagStorageScope Scope { get; set; } = GraphRagStorageScope.Workspace;

    /// <summary>Whether the index operation should force a rebuild.</summary>
    public bool Force { get; set; }
}

/// <summary>Status payload for GraphRAG readiness and workspace state.</summary>
public sealed class GraphRagStatusResponse
{
    /// <summary>Whether GraphRAG is enabled.</summary>
    public bool Enabled { get; set; }

    /// <summary>TR-MCP-GRAPHRAG-GLOBAL-001: Storage scope for this status payload.</summary>
    public GraphRagStorageScope Scope { get; set; } = GraphRagStorageScope.Workspace;

    /// <summary>Workspace path associated with the GraphRAG status.</summary>
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Resolved GraphRAG storage root.</summary>
    public string GraphRoot { get; set; } = string.Empty;

    /// <summary>GraphRAG readiness state.</summary>
    public string State { get; set; } = "disabled";

    /// <summary>Whether GraphRAG storage has been initialized.</summary>
    public bool IsInitialized { get; set; }

    /// <summary>Whether the GraphRAG corpus is indexed.</summary>
    public bool IsIndexed { get; set; }

    /// <summary>UTC timestamp for the most recent index run.</summary>
    public DateTimeOffset? LastIndexedAtUtc { get; set; }

    /// <summary>UTC timestamp for the most recent successful operation.</summary>
    public DateTimeOffset? LastSuccessAtUtc { get; set; }

    /// <summary>UTC timestamp for the most recent failed operation.</summary>
    public DateTimeOffset? LastFailureAtUtc { get; set; }

    /// <summary>Identifier of the active GraphRAG job, when present.</summary>
    public string? ActiveJobId { get; set; }

    /// <summary>Optional failure code for the operation.</summary>
    public string? FailureCode { get; set; }

    /// <summary>Last recorded GraphRAG error message.</summary>
    public string? LastError { get; set; }

    /// <summary>GraphRAG artifact version label.</summary>
    public string ArtifactVersion { get; set; } = "v1";

    /// <summary>Duration of the most recent index run in milliseconds.</summary>
    public long? LastIndexDurationMs { get; set; }

    /// <summary>Number of documents indexed in the most recent run.</summary>
    public int? LastIndexedDocumentCount { get; set; }

    /// <summary>GraphRAG backend that produced the status.</summary>
    public string Backend { get; set; } = "internal-fallback";

    /// <summary>Corpus name used for indexing.</summary>
    public string IndexCorpus { get; set; } = "graphrag-input";

    /// <summary>Corpus name used for querying.</summary>
    public string QueryCorpus { get; set; } = "context-search";

    /// <summary>Resolved input path for the GraphRAG corpus.</summary>
    public string InputPath { get; set; } = string.Empty;

    /// <summary>Number of input documents discovered.</summary>
    public int InputDocumentCount { get; set; }

    /// <summary>Optional note describing result visibility.</summary>
    public string? VisibilityNote { get; set; }
}

/// <summary>Citation payload from GraphRAG query responses.</summary>
public sealed class GraphRagCitation
{
    /// <summary>Source key referenced by the citation.</summary>
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>Optional chunk identifier.</summary>
    public string? ChunkId { get; set; }

    /// <summary>Optional citation snippet.</summary>
    public string? Snippet { get; set; }
}

/// <summary>Query response for GraphRAG operations.</summary>
public sealed class GraphRagQueryResponse
{
    /// <summary>TR-MCP-GRAPHRAG-GLOBAL-001: Storage scope used for the query.</summary>
    public GraphRagStorageScope Scope { get; set; } = GraphRagStorageScope.Workspace;

    /// <summary>GraphRAG query text.</summary>
    public string Query { get; set; } = string.Empty;

    /// <summary>GraphRAG query mode.</summary>
    public string Mode { get; set; } = "local";

    /// <summary>Answer text returned by GraphRAG.</summary>
    public string Answer { get; set; } = string.Empty;

    /// <summary>Citations returned with the GraphRAG answer.</summary>
    public IReadOnlyList<GraphRagCitation> Citations { get; set; } = [];

    /// <summary>Context chunks returned with the GraphRAG answer.</summary>
    public IReadOnlyList<ContextChunk> Chunks { get; set; } = [];

    /// <summary>Source keys referenced by the result.</summary>
    public IReadOnlyList<string> SourceKeys { get; set; } = [];

    /// <summary>Entities returned with the GraphRAG answer.</summary>
    public IReadOnlyList<string> Entities { get; set; } = [];

    /// <summary>Relationships returned with the GraphRAG answer.</summary>
    public IReadOnlyList<string> Relationships { get; set; } = [];

    /// <summary>Communities returned with the GraphRAG answer.</summary>
    public IReadOnlyList<string> Communities { get; set; } = [];

    /// <summary>Whether a fallback response path was used.</summary>
    public bool FallbackUsed { get; set; }

    /// <summary>Reason the fallback response path was used.</summary>
    public string? FallbackReason { get; set; }

    /// <summary>Optional failure code for the operation.</summary>
    public string? FailureCode { get; set; }

    /// <summary>GraphRAG backend that produced the result.</summary>
    public string Backend { get; set; } = "internal-fallback";

    /// <summary>Corpus name used for querying.</summary>
    public string QueryCorpus { get; set; } = "context-search";

    /// <summary>Optional note describing result visibility.</summary>
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
