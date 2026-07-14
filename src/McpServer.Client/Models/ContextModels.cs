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

/// <summary>Request for website URL ingestion.</summary>
public sealed class WebsiteIngestRequest
{
    /// <summary>URL submitted for website ingestion.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Whether the ingestion run should include linked subpages.</summary>
    [JsonPropertyName("includeSubpages")]
    public bool IncludeSubpages { get; set; }

    /// <summary>Maximum number of pages to ingest.</summary>
    [JsonPropertyName("maxPages")]
    public int MaxPages { get; set; } = 20;

    /// <summary>Maximum crawl depth for linked pages.</summary>
    [JsonPropertyName("maxDepth")]
    public int MaxDepth { get; set; } = 1;

    /// <summary>Maximum bytes to read from each page.</summary>
    [JsonPropertyName("maxBytesPerPage")]
    public int MaxBytesPerPage { get; set; } = 262_144;

    /// <summary>Whether existing website ingestion cache entries should be refreshed.</summary>
    [JsonPropertyName("forceRefresh")]
    public bool ForceRefresh { get; set; }

    /// <summary>Whether GraphRAG indexing should run after ingestion.</summary>
    [JsonPropertyName("triggerGraphRagIndex")]
    public bool TriggerGraphRagIndex { get; set; }
}

/// <summary>Per-URL website ingestion outcome.</summary>
public sealed class WebsiteIngestUrlResult
{
    /// <summary>URL processed during website ingestion.</summary>
    [JsonPropertyName("url")]
    public string Url { get; set; } = string.Empty;

    /// <summary>Status value reported for the URL.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Source key assigned to the ingested content.</summary>
    [JsonPropertyName("sourceKey")]
    public string? SourceKey { get; set; }

    /// <summary>Optional status or diagnostic message.</summary>
    [JsonPropertyName("message")]
    public string? Message { get; set; }

    /// <summary>Number of chunks written for the URL.</summary>
    [JsonPropertyName("chunksWritten")]
    public int ChunksWritten { get; set; }
}

/// <summary>Website ingestion response payload.</summary>
public sealed class WebsiteIngestResult
{
    /// <summary>Identifier for the ingestion run.</summary>
    [JsonPropertyName("runId")]
    public string RunId { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the run started.</summary>
    [JsonPropertyName("startedAtUtc")]
    public string? StartedAtUtc { get; set; }

    /// <summary>UTC timestamp when the run completed.</summary>
    [JsonPropertyName("completedAtUtc")]
    public string? CompletedAtUtc { get; set; }

    /// <summary>Status value reported for the operation.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Number of documents ingested.</summary>
    [JsonPropertyName("documentsIngested")]
    public int DocumentsIngested { get; set; }

    /// <summary>Number of chunks written during ingestion.</summary>
    [JsonPropertyName("chunksWritten")]
    public int ChunksWritten { get; set; }

    /// <summary>Per-URL ingestion results.</summary>
    [JsonPropertyName("urlResults")]
    public IReadOnlyList<WebsiteIngestUrlResult> UrlResults { get; set; } = [];

    /// <summary>Whether GraphRAG indexing completed for the run.</summary>
    [JsonPropertyName("graphRagIndexed")]
    public bool GraphRagIndexed { get; set; }

    /// <summary>Optional GraphRAG indexing error message.</summary>
    [JsonPropertyName("graphRagIndexError")]
    public string? GraphRagIndexError { get; set; }
}

/// <summary>Request for GraphRAG query.</summary>
public sealed class GraphRagQueryRequest
{
    /// <summary>GraphRAG query text.</summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>GraphRAG query mode.</summary>
    [JsonPropertyName("mode")]
    public string? Mode { get; set; }

    /// <summary>Maximum number of chunks to include.</summary>
    [JsonPropertyName("maxChunks")]
    public int? MaxChunks { get; set; }

    /// <summary>Whether context chunks should be included in the response.</summary>
    [JsonPropertyName("includeContextChunks")]
    public bool IncludeContextChunks { get; set; } = true;

    /// <summary>Maximum number of entities to include.</summary>
    [JsonPropertyName("maxEntities")]
    public int? MaxEntities { get; set; }

    /// <summary>Maximum number of relationships to include.</summary>
    [JsonPropertyName("maxRelationships")]
    public int? MaxRelationships { get; set; }

    /// <summary>Maximum community traversal depth.</summary>
    [JsonPropertyName("communityDepth")]
    public int? CommunityDepth { get; set; }

    /// <summary>Token budget for the generated response.</summary>
    [JsonPropertyName("responseTokenBudget")]
    public int? ResponseTokenBudget { get; set; }
}

/// <summary>Request for GraphRAG index operation.</summary>
public sealed class GraphRagIndexRequest
{
    /// <summary>Whether the index operation should force a rebuild.</summary>
    [JsonPropertyName("force")]
    public bool Force { get; set; }
}

/// <summary>GraphRAG status response.</summary>
public sealed class GraphRagStatusResult
{
    /// <summary>Whether GraphRAG is enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Workspace path associated with the GraphRAG status.</summary>
    [JsonPropertyName("workspacePath")]
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Resolved GraphRAG storage root.</summary>
    [JsonPropertyName("graphRoot")]
    public string GraphRoot { get; set; } = string.Empty;

    /// <summary>GraphRAG readiness state.</summary>
    [JsonPropertyName("state")]
    public string State { get; set; } = string.Empty;

    /// <summary>Whether GraphRAG storage has been initialized.</summary>
    [JsonPropertyName("isInitialized")]
    public bool IsInitialized { get; set; }

    /// <summary>Whether the GraphRAG corpus is indexed.</summary>
    [JsonPropertyName("isIndexed")]
    public bool IsIndexed { get; set; }

    /// <summary>UTC timestamp for the most recent index run.</summary>
    [JsonPropertyName("lastIndexedAtUtc")]
    public string? LastIndexedAtUtc { get; set; }

    /// <summary>UTC timestamp for the most recent successful operation.</summary>
    [JsonPropertyName("lastSuccessAtUtc")]
    public string? LastSuccessAtUtc { get; set; }

    /// <summary>UTC timestamp for the most recent failed operation.</summary>
    [JsonPropertyName("lastFailureAtUtc")]
    public string? LastFailureAtUtc { get; set; }

    /// <summary>Identifier of the active GraphRAG job, when present.</summary>
    [JsonPropertyName("activeJobId")]
    public string? ActiveJobId { get; set; }

    /// <summary>Optional failure code for the operation.</summary>
    [JsonPropertyName("failureCode")]
    public string? FailureCode { get; set; }

    /// <summary>Last recorded GraphRAG error message.</summary>
    [JsonPropertyName("lastError")]
    public string? LastError { get; set; }

    /// <summary>GraphRAG artifact version label.</summary>
    [JsonPropertyName("artifactVersion")]
    public string ArtifactVersion { get; set; } = string.Empty;

    /// <summary>Duration of the most recent index run in milliseconds.</summary>
    [JsonPropertyName("lastIndexDurationMs")]
    public long? LastIndexDurationMs { get; set; }

    /// <summary>Number of documents indexed in the most recent run.</summary>
    [JsonPropertyName("lastIndexedDocumentCount")]
    public int? LastIndexedDocumentCount { get; set; }

    /// <summary>GraphRAG backend that produced the status.</summary>
    [JsonPropertyName("backend")]
    public string Backend { get; set; } = string.Empty;

    /// <summary>Corpus name used for indexing.</summary>
    [JsonPropertyName("indexCorpus")]
    public string IndexCorpus { get; set; } = string.Empty;

    /// <summary>Corpus name used for querying.</summary>
    [JsonPropertyName("queryCorpus")]
    public string QueryCorpus { get; set; } = string.Empty;

    /// <summary>Resolved input path for the GraphRAG corpus.</summary>
    [JsonPropertyName("inputPath")]
    public string InputPath { get; set; } = string.Empty;

    /// <summary>Number of input documents discovered.</summary>
    [JsonPropertyName("inputDocumentCount")]
    public int InputDocumentCount { get; set; }

    /// <summary>Optional note describing result visibility.</summary>
    [JsonPropertyName("visibilityNote")]
    public string? VisibilityNote { get; set; }
}

/// <summary>GraphRAG citation entry.</summary>
public sealed class GraphRagCitation
{
    /// <summary>Source key referenced by the citation.</summary>
    [JsonPropertyName("sourceKey")]
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>Optional chunk identifier.</summary>
    [JsonPropertyName("chunkId")]
    public string? ChunkId { get; set; }

    /// <summary>Optional citation snippet.</summary>
    [JsonPropertyName("snippet")]
    public string? Snippet { get; set; }
}

/// <summary>GraphRAG query response.</summary>
public sealed class GraphRagQueryResult
{
    /// <summary>GraphRAG query text.</summary>
    [JsonPropertyName("query")]
    public string Query { get; set; } = string.Empty;

    /// <summary>GraphRAG query mode.</summary>
    [JsonPropertyName("mode")]
    public string Mode { get; set; } = string.Empty;

    /// <summary>Answer text returned by GraphRAG.</summary>
    [JsonPropertyName("answer")]
    public string Answer { get; set; } = string.Empty;

    /// <summary>Citations returned with the GraphRAG answer.</summary>
    [JsonPropertyName("citations")]
    public IReadOnlyList<GraphRagCitation> Citations { get; set; } = [];

    /// <summary>Context chunks returned with the GraphRAG answer.</summary>
    [JsonPropertyName("chunks")]
    public IReadOnlyList<ContextChunkResult> Chunks { get; set; } = [];

    /// <summary>Source keys referenced by the result.</summary>
    [JsonPropertyName("sourceKeys")]
    public IReadOnlyList<string> SourceKeys { get; set; } = [];

    /// <summary>Entities returned with the GraphRAG answer.</summary>
    [JsonPropertyName("entities")]
    public IReadOnlyList<string> Entities { get; set; } = [];

    /// <summary>Relationships returned with the GraphRAG answer.</summary>
    [JsonPropertyName("relationships")]
    public IReadOnlyList<string> Relationships { get; set; } = [];

    /// <summary>Communities returned with the GraphRAG answer.</summary>
    [JsonPropertyName("communities")]
    public IReadOnlyList<string> Communities { get; set; } = [];

    /// <summary>Whether a fallback response path was used.</summary>
    [JsonPropertyName("fallbackUsed")]
    public bool FallbackUsed { get; set; }

    /// <summary>Reason the fallback response path was used.</summary>
    [JsonPropertyName("fallbackReason")]
    public string? FallbackReason { get; set; }

    /// <summary>Optional failure code for the operation.</summary>
    [JsonPropertyName("failureCode")]
    public string? FailureCode { get; set; }

    /// <summary>GraphRAG backend that produced the result.</summary>
    [JsonPropertyName("backend")]
    public string Backend { get; set; } = string.Empty;

    /// <summary>Corpus name used for querying.</summary>
    [JsonPropertyName("queryCorpus")]
    public string QueryCorpus { get; set; } = string.Empty;

    /// <summary>Optional note describing result visibility.</summary>
    [JsonPropertyName("visibilityNote")]
    public string? VisibilityNote { get; set; }
}

// ────────────────────────────────────────────────
//  Ad-Hoc Management Client DTOs (FR-MCP-078/079/080)
// ────────────────────────────────────────────────

/// <summary>FR-MCP-078, TR-GRAPHRAG-ADHOC-001: Request to ingest raw text into the GraphRAG corpus.</summary>
public sealed class GraphRagIngestTextRequest
{
    /// <summary>The text content to ingest.</summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>Optional title for the ingested document.</summary>
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    /// <summary>Source type classification (defaults to "adhoc-text").</summary>
    [JsonPropertyName("sourceType")]
    public string? SourceType { get; set; }

    /// <summary>Source key / path for the document.</summary>
    [JsonPropertyName("sourceKey")]
    public string? SourceKey { get; set; }

    /// <summary>When true, triggers a full reindex after ingestion.</summary>
    [JsonPropertyName("triggerReindex")]
    public bool TriggerReindex { get; set; }
}

/// <summary>FR-MCP-078, TR-GRAPHRAG-ADHOC-001: Response after ingesting raw text.</summary>
public sealed class GraphRagIngestTextResult
{
    /// <summary>Generated document identifier.</summary>
    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>Number of chunks the text was split into.</summary>
    [JsonPropertyName("chunkCount")]
    public int ChunkCount { get; set; }

    /// <summary>Total estimated token count across all chunks.</summary>
    [JsonPropertyName("tokenCount")]
    public int TokenCount { get; set; }

    /// <summary>Resolved source type used for the document.</summary>
    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = string.Empty;

    /// <summary>Resolved source key used for the document.</summary>
    [JsonPropertyName("sourceKey")]
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>Whether a reindex was triggered after ingestion.</summary>
    [JsonPropertyName("reindexTriggered")]
    public bool ReindexTriggered { get; set; }
}

/// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Summary of a document stored in the GraphRAG corpus.</summary>
public sealed class GraphRagDocumentSummary
{
    /// <summary>Unique document identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Source type classification.</summary>
    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = string.Empty;

    /// <summary>Source key / path.</summary>
    [JsonPropertyName("sourceKey")]
    public string SourceKey { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the document was ingested.</summary>
    [JsonPropertyName("ingestedAt")]
    public string? IngestedAt { get; set; }

    /// <summary>SHA-256 content hash.</summary>
    [JsonPropertyName("contentHash")]
    public string ContentHash { get; set; } = string.Empty;

    /// <summary>Number of chunks the document was split into.</summary>
    [JsonPropertyName("chunkCount")]
    public int ChunkCount { get; set; }

    /// <summary>Total estimated token count across all chunks.</summary>
    [JsonPropertyName("totalTokens")]
    public int TotalTokens { get; set; }
}

/// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Paginated list of GraphRAG documents.</summary>
public sealed class GraphRagDocumentListResult
{
    /// <summary>The documents in this page.</summary>
    [JsonPropertyName("documents")]
    public IReadOnlyList<GraphRagDocumentSummary> Documents { get; set; } = [];

    /// <summary>Total number of documents matching the query.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Response containing a document's chunks.</summary>
public sealed class GraphRagDocumentChunksResult
{
    /// <summary>Parent document identifier.</summary>
    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>Ordered list of chunk items.</summary>
    [JsonPropertyName("chunks")]
    public IReadOnlyList<GraphRagDocumentChunkItem> Chunks { get; set; } = [];

    /// <summary>Total number of chunks in the document.</summary>
    [JsonPropertyName("totalChunks")]
    public int TotalChunks { get; set; }
}

/// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: A single chunk within a document.</summary>
public sealed class GraphRagDocumentChunkItem
{
    /// <summary>Unique chunk identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Chunk text content.</summary>
    [JsonPropertyName("content")]
    public string Content { get; set; } = string.Empty;

    /// <summary>Estimated token count.</summary>
    [JsonPropertyName("tokenCount")]
    public int TokenCount { get; set; }

    /// <summary>Zero-based index within the parent document.</summary>
    [JsonPropertyName("chunkIndex")]
    public int ChunkIndex { get; set; }
}

/// <summary>FR-MCP-080, TR-GRAPHRAG-ADHOC-003: Response after deleting a document.</summary>
public sealed class GraphRagDocumentDeleteResult
{
    /// <summary>Deleted document identifier.</summary>
    [JsonPropertyName("documentId")]
    public string DocumentId { get; set; } = string.Empty;

    /// <summary>Number of chunks removed.</summary>
    [JsonPropertyName("chunksRemoved")]
    public int ChunksRemoved { get; set; }

    /// <summary>Whether the deletion was successful.</summary>
    [JsonPropertyName("success")]
    public bool Success { get; set; }
}

/// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Request to create or update a graph entity.</summary>
public sealed class GraphEntityRequest
{
    /// <summary>Display name of the entity.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Entity classification (e.g. "person", "organization", "concept").</summary>
    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Optional free-text description of the entity.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Optional JSON metadata blob.</summary>
    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }
}

/// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Response representing a single graph entity.</summary>
public sealed class GraphEntityResult
{
    /// <summary>Unique entity identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Display name of the entity.</summary>
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    /// <summary>Entity classification type.</summary>
    [JsonPropertyName("entityType")]
    public string EntityType { get; set; } = string.Empty;

    /// <summary>Free-text description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>JSON metadata blob.</summary>
    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }

    /// <summary>UTC timestamp when the entity was created.</summary>
    [JsonPropertyName("createdAtUtc")]
    public string? CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the entity was last modified.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public string? UpdatedAtUtc { get; set; }
}

/// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Paginated list of graph entities.</summary>
public sealed class GraphEntityListResult
{
    /// <summary>The entities in this page.</summary>
    [JsonPropertyName("entities")]
    public IReadOnlyList<GraphEntityResult> Entities { get; set; } = [];

    /// <summary>Total number of entities matching the query.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}

/// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Request to create or update a graph relationship.</summary>
public sealed class GraphRelationshipRequest
{
    /// <summary>Source entity identifier.</summary>
    [JsonPropertyName("sourceEntityId")]
    public string SourceEntityId { get; set; } = string.Empty;

    /// <summary>Target entity identifier.</summary>
    [JsonPropertyName("targetEntityId")]
    public string TargetEntityId { get; set; } = string.Empty;

    /// <summary>Relationship classification type.</summary>
    [JsonPropertyName("relationshipType")]
    public string RelationshipType { get; set; } = string.Empty;

    /// <summary>Optional description of the relationship.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Numeric weight/strength (default 1.0).</summary>
    [JsonPropertyName("weight")]
    public double Weight { get; set; } = 1.0;

    /// <summary>Optional JSON metadata blob.</summary>
    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }
}

/// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Response representing a single graph relationship.</summary>
public sealed class GraphRelationshipResult
{
    /// <summary>Unique relationship identifier.</summary>
    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    /// <summary>Source entity identifier.</summary>
    [JsonPropertyName("sourceEntityId")]
    public string SourceEntityId { get; set; } = string.Empty;

    /// <summary>Target entity identifier.</summary>
    [JsonPropertyName("targetEntityId")]
    public string TargetEntityId { get; set; } = string.Empty;

    /// <summary>Relationship classification type.</summary>
    [JsonPropertyName("relationshipType")]
    public string RelationshipType { get; set; } = string.Empty;

    /// <summary>Free-text description.</summary>
    [JsonPropertyName("description")]
    public string? Description { get; set; }

    /// <summary>Numeric weight/strength.</summary>
    [JsonPropertyName("weight")]
    public double Weight { get; set; }

    /// <summary>JSON metadata blob.</summary>
    [JsonPropertyName("metadata")]
    public string? Metadata { get; set; }

    /// <summary>UTC timestamp when the relationship was created.</summary>
    [JsonPropertyName("createdAtUtc")]
    public string? CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the relationship was last modified.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public string? UpdatedAtUtc { get; set; }
}

/// <summary>FR-MCP-079, TR-GRAPHRAG-ADHOC-002: Paginated list of graph relationships.</summary>
public sealed class GraphRelationshipListResult
{
    /// <summary>The relationships in this page.</summary>
    [JsonPropertyName("relationships")]
    public IReadOnlyList<GraphRelationshipResult> Relationships { get; set; } = [];

    /// <summary>Total number of relationships matching the query.</summary>
    [JsonPropertyName("totalCount")]
    public int TotalCount { get; set; }
}
