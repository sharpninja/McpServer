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
