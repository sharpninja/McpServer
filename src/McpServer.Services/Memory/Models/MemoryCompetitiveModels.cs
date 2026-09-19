namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-010 / TR-MCP-MEMORY-MODEL-002: Documented validation limits and defaults
/// for multi-layer memory fields. S1 Green must enforce these at the CQRS remember path.
/// </summary>
public static class MemoryLimits
{
    /// <summary>Maximum title length accepted by remember/update.</summary>
    public const int MaxTitleLength = 256;

    /// <summary>Maximum single tag length.</summary>
    public const int MaxTagLength = 64;

    /// <summary>Maximum content length.</summary>
    public const int MaxContentLength = 32768;

    /// <summary>Documented default confidence when the caller omits the field.</summary>
    public const double DefaultConfidence = 1.0;

    /// <summary>Allowed Type tokens for FR-MCP-MEMORY-010-02.</summary>
    public static readonly IReadOnlySet<string> AllowedTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "fact", "decision", "preference", "procedure", "entity", "other",
    };
}

/// <summary>
/// FR-MCP-MEMORY-010: Request body for <c>memory_remember</c> (additive CQRS verb).
/// Compat <see cref="MemoryAddRequest"/> remains for legacy add.
/// </summary>
public sealed record MemoryRememberRequest
{
    /// <summary>Optional explicit MEMORY-* id.</summary>
    public string? Id { get; init; }

    /// <summary>Optional title.</summary>
    public string? Title { get; init; }

    /// <summary>Optional summary.</summary>
    public string? Summary { get; init; }

    /// <summary>Required multi-layer content.</summary>
    public string? Content { get; init; }

    /// <summary>Type token: fact, decision, preference, procedure, entity, other.</summary>
    public string? Type { get; init; }

    /// <summary>Optional tags. Null is treated as empty.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Optional confidence in [0,1].</summary>
    public double? Confidence { get; init; }

    /// <summary>Optional provenance kind.</summary>
    public string? SourceKind { get; init; }

    /// <summary>Optional provenance reference.</summary>
    public string? SourceRef { get; init; }

    /// <summary>Scope. Omitted defaults to Workspace.</summary>
    public MemoryScope? Scope { get; init; }

    /// <summary>Legacy category still accepted.</summary>
    public string? Category { get; init; }

    /// <summary>Create attribution.</summary>
    public string? CreatedBy { get; init; }

    /// <summary>Update attribution.</summary>
    public string? UpdatedBy { get; init; }
}

/// <summary>
/// FR-MCP-MEMORY-010 / TEST-MCP-MEMORY-016: HTTP-shaped remember outcome used by S1 acceptance tests.
/// </summary>
public sealed record MemoryRememberResult(
    int StatusCode,
    string? MemoryId = null,
    MemoryItem? Memory = null,
    MemoryMutationFailureKind FailureKind = MemoryMutationFailureKind.None,
    string? Error = null);

/// <summary>
/// FR-MCP-MEMORY-014: One version snapshot for a memory.
/// </summary>
public sealed record MemoryVersionSnapshot
{
    /// <summary>Contiguous positive version number.</summary>
    public required int VersionNumber { get; init; }

    /// <summary>Title at this snapshot.</summary>
    public string? Title { get; init; }

    /// <summary>Content at this snapshot.</summary>
    public required string Content { get; init; }

    /// <summary>When the snapshot was written.</summary>
    public DateTimeOffset CreatedAtUtc { get; init; }
}

/// <summary>
/// FR-MCP-MEMORY-014: Result of listing versions.
/// </summary>
public sealed record MemoryVersionListResult(
    int StatusCode,
    IReadOnlyList<MemoryVersionSnapshot>? Items = null,
    string? Error = null);

/// <summary>
/// FR-MCP-MEMORY-014: Result of reverting a memory to a prior snapshot.
/// </summary>
public sealed record MemoryRevertResult(
    int StatusCode,
    MemoryItem? Memory = null,
    MemoryMutationFailureKind FailureKind = MemoryMutationFailureKind.None,
    string? Error = null);

/// <summary>
/// TR-MCP-MEMORY-MODEL-002: Explicit memory edge record (From, To, EdgeType).
/// </summary>
public sealed record MemoryEdgeRecord
{
    /// <summary>Source memory id.</summary>
    public required string FromMemoryId { get; init; }

    /// <summary>Target memory id.</summary>
    public required string ToMemoryId { get; init; }

    /// <summary>Edge type token.</summary>
    public required string EdgeType { get; init; }

    /// <summary>Optional weight.</summary>
    public double Weight { get; init; }
}

/// <summary>
/// FR-MCP-MEMORY-011 / TR-MCP-MEMORY-SEARCH-002: Documented recall bounds for S2.
/// </summary>
public static class MemorySearchLimits
{
    /// <summary>AC-FR-MCP-MEMORY-011-16: Default topN when the caller omits the field.</summary>
    public const int DefaultTopN = 10;

    /// <summary>AC-FR-MCP-MEMORY-011-18: Configured maximum topN. Above this is clamped or 400.</summary>
    public const int MaxTopN = 50;

    /// <summary>AC-FR-MCP-MEMORY-011-21: Maximum query length accepted by memory_recall.</summary>
    public const int MaxQueryLength = 2048;

    /// <summary>AC-FR-MCP-MEMORY-011-13: Documented default minScore when omitted (include all scored hits).</summary>
    public const double DefaultMinScore = 0;
}

/// <summary>
/// FR-MCP-MEMORY-011: Request body for <c>memory_recall</c> hybrid search.
/// </summary>
public sealed record MemoryRecallRequest
{
    /// <summary>Recall query text. Empty, whitespace, or null must return 400.</summary>
    public string? Query { get; init; }

    /// <summary>Minimum hit score in [0,1].</summary>
    public double? MinScore { get; init; }

    /// <summary>Maximum number of ranked hits to return.</summary>
    public int? TopN { get; init; }

    /// <summary>Optional tag filter. Combined with other filters using AND.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Optional type filter.</summary>
    public string? Type { get; init; }

    /// <summary>Optional scope filter (Global vs Workspace visibility).</summary>
    public MemoryScope? Scope { get; init; }

    /// <summary>Optional BM25 fusion weight override for fixture ordering tests.</summary>
    public double? FusionBm25Weight { get; init; }

    /// <summary>Optional vector fusion weight override for fixture ordering tests.</summary>
    public double? FusionVectorWeight { get; init; }

    /// <summary>Optional rerank flag. Default production config must keep rerank off.</summary>
    public bool? RerankEnabled { get; init; }
}

/// <summary>
/// FR-MCP-MEMORY-011: One ranked recall hit. Score is required for hybrid recall Green.
/// </summary>
public sealed record MemoryRecallHit
{
    /// <summary>MEMORY-* id of the hit.</summary>
    public required string Id { get; init; }

    /// <summary>Hybrid fusion score in [0,1].</summary>
    public double? Score { get; init; }

    /// <summary>Title when persisted.</summary>
    public string? Title { get; init; }

    /// <summary>Content used to open the memory without a second round-trip.</summary>
    public string? Content { get; init; }

    /// <summary>Legacy text field when Content is unset.</summary>
    public string? Text { get; init; }

    /// <summary>Memory type token.</summary>
    public string? Type { get; init; }

    /// <summary>Tags persisted with the memory.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>Visibility scope of the hit.</summary>
    public MemoryScope? Scope { get; init; }

    /// <summary>How the hit was retrieved (bm25, vector, or hybrid).</summary>
    public string? MatchKind { get; init; }

    /// <summary>Workspace id used for ANN scoping. Must match the caller for workspace hits.</summary>
    public string? AnnWorkspaceId { get; init; }
}

/// <summary>
/// FR-MCP-MEMORY-011 / TEST-MCP-MEMORY-011: HTTP-shaped recall outcome used by S2 acceptance tests.
/// </summary>
public sealed record MemoryRecallResult(
    int StatusCode,
    IReadOnlyList<MemoryRecallHit>? Items = null,
    MemoryMutationFailureKind FailureKind = MemoryMutationFailureKind.None,
    string? Error = null,
    bool? RerankApplied = null,
    string? RankingMode = null);

/// <summary>
/// TR-MCP-MEMORY-SEARCH-002: Outcome of indexing or reconciling a memory embedding.
/// </summary>
public sealed record MemoryIndexResult(
    int StatusCode,
    string? MemoryId = null,
    string? EmbeddingStatus = null,
    string? FailureReason = null,
    int ReadyCount = 0,
    int FailedCount = 0,
    string? Error = null);

/// <summary>
/// FR-MCP-MEMORY-012: Documented explore/Hebbian bounds.
/// Hebbian remains off by default (<c>Mcp:Memory:Hebbian:Enabled=false</c>).
/// Depth above max returns 400. Duplicate explicit edges return 409 to match the
/// unique (From, To, EdgeType) index.
/// </summary>
public static class MemoryExploreLimits
{
    /// <summary>AC-FR-MCP-MEMORY-012-08: Default explore depth when omitted (direct neighbors only).</summary>
    public const int DefaultDepth = 1;

    /// <summary>AC-FR-MCP-MEMORY-012-10: Configured maximum explore depth. Above this returns 400.</summary>
    public const int MaxDepth = 5;

    /// <summary>AC-FR-MCP-MEMORY-012-19: Default neighbor cap when omitted.</summary>
    public const int DefaultMaxNeighbors = 20;

    /// <summary>AC-FR-MCP-MEMORY-012-19: Configured maximum neighbor cap.</summary>
    public const int MaxNeighbors = 50;

    /// <summary>AC-FR-MCP-MEMORY-012-02: Hebbian is off by default (Mcp:Memory:Hebbian:Enabled=false).</summary>
    public const bool DefaultHebbianEnabled = false;

    /// <summary>AC-FR-MCP-MEMORY-012-14: Minimum allowed edge weight.</summary>
    public const double MinWeight = 0;

    /// <summary>AC-FR-MCP-MEMORY-012-14: Maximum allowed edge weight.</summary>
    public const double MaxWeight = 1;

    /// <summary>AC-FR-MCP-MEMORY-012-15: Allowed EdgeType tokens.</summary>
    public static readonly IReadOnlySet<string> AllowedEdgeTypes = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "explicit", "related", "derived", "co-retrieved",
    };

    /// <summary>AC-FR-MCP-MEMORY-012-12: Documented duplicate explicit-edge policy is HTTP 409.</summary>
    public const int DuplicateExplicitStatusCode = 409;

    /// <summary>AC-FR-MCP-MEMORY-012-10: Documented depth-above-max policy is HTTP 400.</summary>
    public const int DepthAboveMaxStatusCode = 400;

    /// <summary>AC-FR-MCP-MEMORY-012-11: Documented self-loop create policy is HTTP 400.</summary>
    public const int SelfLoopStatusCode = 400;

    /// <summary>
    /// AC-FR-MCP-MEMORY-012-13: Minimum recall score required to treat a query as a seed.
    /// Weaker incidental token overlap is documented empty, not a neighborhood walk.
    /// </summary>
    public const double QuerySeedMinScore = 0.5;
}

/// <summary>
/// FR-MCP-MEMORY-012: Request body for <c>memory_explore</c> neighborhood walk.
/// Provide a seed id, a query seed, or both. Query-only uses the top recall hit.
/// </summary>
public sealed record MemoryExploreRequest
{
    /// <summary>Optional MEMORY-* seed. Unknown or soft-deleted seeds must return 404.</summary>
    public string? SeedId { get; init; }

    /// <summary>Optional query seed used when SeedId is omitted (top recall hit or documented empty).</summary>
    public string? Query { get; init; }

    /// <summary>Hop depth. Omitted uses <see cref="MemoryExploreLimits.DefaultDepth"/>. Non-positive is 400.</summary>
    public int? Depth { get; init; }

    /// <summary>Neighbor cap. Omitted uses <see cref="MemoryExploreLimits.DefaultMaxNeighbors"/>.</summary>
    public int? MaxNeighbors { get; init; }

    /// <summary>Optional Hebbian override. Null uses the default (off).</summary>
    public bool? HebbianEnabled { get; init; }
}

/// <summary>
/// FR-MCP-MEMORY-012: One explore neighbor. Weight and EdgeType are required for explicit edges.
/// </summary>
public sealed record MemoryExploreNeighbor
{
    /// <summary>MEMORY-* id of the neighbor.</summary>
    public required string Id { get; init; }

    /// <summary>Edge weight in [0,1].</summary>
    public double Weight { get; init; }

    /// <summary>Edge type token (explicit, related, derived, or co-retrieved).</summary>
    public required string EdgeType { get; init; }

    /// <summary>Hop distance from the seed (1 for a direct neighbor).</summary>
    public int Depth { get; init; }
}

/// <summary>
/// FR-MCP-MEMORY-012 / TEST-MCP-MEMORY-012: HTTP-shaped explore outcome used by S3 acceptance tests.
/// </summary>
public sealed record MemoryExploreResult(
    int StatusCode,
    IReadOnlyList<MemoryExploreNeighbor>? Items = null,
    MemoryMutationFailureKind FailureKind = MemoryMutationFailureKind.None,
    string? Error = null,
    string? SeedId = null,
    bool? HebbianApplied = null);

/// <summary>
/// FR-MCP-MEMORY-012: Request body for creating a directed memory edge.
/// </summary>
public sealed record MemoryCreateEdgeRequest
{
    /// <summary>Source MEMORY-* id.</summary>
    public string? FromMemoryId { get; init; }

    /// <summary>Target MEMORY-* id.</summary>
    public string? ToMemoryId { get; init; }

    /// <summary>Edge type token.</summary>
    public string? EdgeType { get; init; }

    /// <summary>Edge weight in [0,1].</summary>
    public double? Weight { get; init; }
}

/// <summary>
/// FR-MCP-MEMORY-012 / TEST-MCP-MEMORY-012: HTTP-shaped create-edge outcome used by S3 acceptance tests.
/// </summary>
public sealed record MemoryCreateEdgeResult(
    int StatusCode,
    MemoryEdgeRecord? Edge = null,
    MemoryMutationFailureKind FailureKind = MemoryMutationFailureKind.None,
    string? Error = null);

/// <summary>
/// FR-MCP-MEMORY-012: Outcome of recording a Hebbian co-retrieval strengthening.
/// Must not change recall ranking.
/// </summary>
public sealed record MemoryHebbianResult(
    int StatusCode,
    IReadOnlyList<MemoryEdgeRecord>? Edges = null,
    MemoryMutationFailureKind FailureKind = MemoryMutationFailureKind.None,
    string? Error = null,
    bool RankingUnchanged = false);
