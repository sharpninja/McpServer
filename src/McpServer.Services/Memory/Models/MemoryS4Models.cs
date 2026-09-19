namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-MEMORY-013 / TR-MCP-MEMORY-JOBS-002: Documented consolidate bounds.
/// Dry-run remains the default (<c>Mcp:Memory:Consolidate:DryRun=true</c>).
/// </summary>
public static class MemoryConsolidateLimits
{
    /// <summary>AC-FR-MCP-MEMORY-013-06 / AC-TR-MCP-MEMORY-JOBS-002-02: dry-run defaults true.</summary>
    public const bool DefaultDryRun = true;

    /// <summary>AC-FR-MCP-MEMORY-013-03: hard-delete is off by default.</summary>
    public const bool DefaultAllowHardDelete = false;

    /// <summary>Default similarity threshold for near-duplicate detection.</summary>
    public const double DefaultSimilarityThreshold = 0.85;

    /// <summary>Minimum accepted similarity threshold (inclusive).</summary>
    public const double MinSimilarityThreshold = 0.0;

    /// <summary>Maximum accepted similarity threshold (inclusive).</summary>
    public const double MaxSimilarityThreshold = 1.0;
}

/// <summary>
/// FR-MCP-MEMORY-013: Request body for consolidate / sleep merge planning.
/// </summary>
public sealed record MemoryConsolidateRequest
{
    /// <summary>When null, uses <see cref="MemoryConsolidateLimits.DefaultDryRun"/> (true).</summary>
    public bool? DryRun { get; init; }

    /// <summary>Similarity threshold in [0,1]. Out of range is 400.</summary>
    public double? SimilarityThreshold { get; init; }

    /// <summary>When true, merged-away rows may be hard-deleted. Default false.</summary>
    public bool? AllowHardDelete { get; init; }

    /// <summary>Optional cancellation token id / client run id for mid-run cancel tests.</summary>
    public string? RunId { get; init; }

    /// <summary>When true, the harness simulates a mid-run cancel.</summary>
    public bool CancelRequested { get; init; }
}

/// <summary>
/// FR-MCP-MEMORY-013: One planned merge cluster.
/// </summary>
public sealed record MemoryConsolidatePlanItem
{
    /// <summary>Candidate MEMORY-* ids in the cluster.</summary>
    public required IReadOnlyList<string> CandidateIds { get; init; }

    /// <summary>Similarity score for the cluster.</summary>
    public double SimilarityScore { get; init; }

    /// <summary>Chosen survivor id when write-mode applied; null for dry-run-only plans.</summary>
    public string? SurvivorId { get; init; }
}

/// <summary>
/// FR-MCP-MEMORY-013 / TEST-MCP-MEMORY-013: HTTP-shaped consolidate outcome.
/// </summary>
public sealed record MemoryConsolidateResult(
    int StatusCode,
    IReadOnlyList<MemoryConsolidatePlanItem>? Plan = null,
    string? SurvivorId = null,
    IReadOnlyList<string>? MergedAwayIds = null,
    string? RunId = null,
    bool? DryRunApplied = null,
    bool? EventEmitted = null,
    MemoryMutationFailureKind FailureKind = MemoryMutationFailureKind.None,
    string? Error = null);

/// <summary>
/// FR-MCP-MEMORY-015: Supported promote source kinds.
/// </summary>
public static class MemoryPromoteSourceKinds
{
    /// <summary>Session log action/dialog source.</summary>
    public const string SessionLog = "sessionlog";

    /// <summary>Context chunk source.</summary>
    public const string Context = "context";
}

/// <summary>
/// FR-MCP-MEMORY-015: Request body for explicit promote into memory.
/// </summary>
public sealed record MemoryPromoteRequest
{
    /// <summary>Source kind token (sessionlog or context).</summary>
    public string? SourceKind { get; init; }

    /// <summary>Opaque source reference (turn/action id or context chunk key).</summary>
    public string? SourceRef { get; init; }

    /// <summary>Optional caller-supplied content override. When null, Content is taken from source raw text.</summary>
    public string? Content { get; init; }

    /// <summary>Optional summary override.</summary>
    public string? Summary { get; init; }
}

/// <summary>
/// FR-MCP-MEMORY-015 / TEST-MCP-MEMORY-015: HTTP-shaped promote outcome.
/// </summary>
public sealed record MemoryPromoteResult(
    int StatusCode,
    MemoryItem? Memory = null,
    string? SourceKind = null,
    string? SourceRef = null,
    MemoryMutationFailureKind FailureKind = MemoryMutationFailureKind.None,
    string? Error = null);

/// <summary>
/// TR-MCP-MEMORY-JOBS-002: Outcome of a scheduled consolidate tick / stats probe.
/// </summary>
public sealed record MemoryConsolidateJobResult(
    int StatusCode,
    string? WorkspaceId = null,
    string? RunId = null,
    bool? DryRunApplied = null,
    DateTimeOffset? LastRunAt = null,
    bool? LockHeld = null,
    bool? Disabled = null,
    MemoryMutationFailureKind FailureKind = MemoryMutationFailureKind.None,
    string? Error = null);
