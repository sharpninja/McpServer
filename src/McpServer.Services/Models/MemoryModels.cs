using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Services;

/// <summary>Memory visibility scope.</summary>
[JsonConverter(typeof(JsonStringEnumConverter<MemoryScope>))]
public enum MemoryScope
{
    /// <summary>Memory visible to every workspace.</summary>
    Global = 0,

    /// <summary>Memory visible only to the active workspace.</summary>
    Workspace = 1,
}

/// <summary>Request to create a memory.</summary>
public sealed record MemoryAddRequest
{
    /// <summary>Optional explicit memory id. When omitted, the service generates <c>MEMORY-{CATEGORY}-{NNN}</c>.</summary>
    public string? Id { get; init; }

    /// <summary>Category used for filtering and generated id prefixes.</summary>
    public required string Category { get; init; }

    /// <summary>Memory visibility scope. Defaults to Workspace to avoid accidental global writes.</summary>
    public MemoryScope Scope { get; init; } = MemoryScope.Workspace;

    /// <summary>Raw memory text.</summary>
    public required string Text { get; init; }

    /// <summary>Optional actor or subsystem name recorded as the updater.</summary>
    public string? UpdatedBy { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional title for multi-layer remember payloads.</summary>
    public string? Title { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional summary. Omitted on partial update must not clear an existing value.</summary>
    public string? Summary { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Multi-layer content. Legacy <see cref="Text"/> maps into this field.</summary>
    public string? Content { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Memory type token (fact, decision, preference, procedure, entity, other).</summary>
    public string? Type { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional tags. Null is treated as empty.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional confidence in [0,1]. Omitted uses the documented default.</summary>
    public double? Confidence { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional provenance kind.</summary>
    public string? SourceKind { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional provenance reference.</summary>
    public string? SourceRef { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional create attribution.</summary>
    public string? CreatedBy { get; init; }
}

/// <summary>Request to query effective memories visible to the active workspace.</summary>
public sealed record MemoryListRequest
{
    /// <summary>Optional scope filter. Null returns Global plus active Workspace memories.</summary>
    public MemoryScope? Scope { get; init; }

    /// <summary>Optional category filter.</summary>
    public string? Category { get; init; }

    /// <summary>Optional case-sensitive keyword filter over id, category, and text.</summary>
    public string? Keyword { get; init; }
}

/// <summary>Request to update an existing memory.</summary>
public sealed record MemoryUpdateRequest
{
    /// <summary>Optional category replacement.</summary>
    public string? Category { get; init; }

    /// <summary>Optional scope replacement.</summary>
    public MemoryScope? Scope { get; init; }

    /// <summary>Optional raw text replacement.</summary>
    public string? Text { get; init; }

    /// <summary>Optional actor or subsystem name recorded as the updater.</summary>
    public string? UpdatedBy { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional title replacement.</summary>
    public string? Title { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional summary replacement. Null-omit must not clear unless explicit clear is defined.</summary>
    public string? Summary { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional multi-layer content replacement.</summary>
    public string? Content { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional type replacement.</summary>
    public string? Type { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional tags replacement.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional confidence replacement.</summary>
    public double? Confidence { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional provenance kind replacement.</summary>
    public string? SourceKind { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Optional provenance reference replacement.</summary>
    public string? SourceRef { get; init; }
}

/// <summary>Flattened memory row returned by memory APIs.</summary>
public sealed record MemoryItem
{
    /// <summary>Stable globally unique memory id.</summary>
    public required string Id { get; init; }

    /// <summary>Normalized category token.</summary>
    public required string Category { get; init; }

    /// <summary>Memory visibility scope.</summary>
    public required MemoryScope Scope { get; init; }

    /// <summary>Workspace owner for Workspace memories; null for Global memories.</summary>
    public string? WorkspacePath { get; init; }

    /// <summary>Raw memory text.</summary>
    public required string Text { get; init; }

    /// <summary>Monotonic version incremented on each memory update.</summary>
    public required int Version { get; init; }

    /// <summary>UTC timestamp when the memory was created.</summary>
    public required DateTimeOffset CreatedAtUtc { get; init; }

    /// <summary>UTC timestamp when the memory was last changed.</summary>
    public required DateTimeOffset UpdatedAtUtc { get; init; }

    /// <summary>Optional actor or subsystem that last changed the memory.</summary>
    public string? UpdatedBy { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Multi-layer title when persisted.</summary>
    public string? Title { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Multi-layer summary when persisted.</summary>
    public string? Summary { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Multi-layer content. Legacy text maps here after backfill.</summary>
    public string? Content { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Memory type token.</summary>
    public string? Type { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Tags persisted with the memory.</summary>
    public IReadOnlyList<string>? Tags { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Confidence in [0,1].</summary>
    public double? Confidence { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Provenance kind.</summary>
    public string? SourceKind { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Provenance reference.</summary>
    public string? SourceRef { get; init; }

    /// <summary>FR-MCP-MEMORY-010: Create attribution.</summary>
    public string? CreatedBy { get; init; }
}

/// <summary>Result of listing memories.</summary>
public sealed record MemoryQueryResult(IReadOnlyList<MemoryItem> Items, int TotalCount);

/// <summary>Failure mode for memory mutations.</summary>
[JsonConverter(typeof(JsonStringEnumConverter))]
public enum MemoryMutationFailureKind
{
    /// <summary>No failure classification applies.</summary>
    None = 0,

    /// <summary>The request is invalid.</summary>
    Validation = 1,

    /// <summary>The request conflicts with existing memory state.</summary>
    Conflict = 2,

    /// <summary>The target memory does not exist or is not visible in the active scope.</summary>
    NotFound = 3,
}

/// <summary>Result of creating, updating, or removing a memory.</summary>
public sealed record MemoryMutationResult(
    bool Success,
    string? Error = null,
    MemoryItem? Memory = null,
    MemoryMutationFailureKind FailureKind = MemoryMutationFailureKind.None);
