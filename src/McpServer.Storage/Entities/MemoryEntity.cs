using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-MEMORY-001: Authoritative raw-text memory row shared by MCP agents.
/// </summary>
/// <remarks>
/// Memory ids are globally unique across both scopes. Global memories have no
/// workspace owner (<see cref="WorkspaceId"/> is <c>null</c>); Workspace
/// memories carry the normalized workspace path and are clamped by the
/// <c>McpDbContext</c> workspace query filter.
/// </remarks>
public sealed class MemoryEntity
{
    /// <summary>Scope name used for memories visible to every workspace.</summary>
    public const string GlobalScope = "Global";

    /// <summary>Scope name used for memories visible only to one workspace.</summary>
    public const string WorkspaceScope = "Workspace";

    /// <summary>Stable globally unique memory id, such as <c>MEMORY-USER-001</c>.</summary>
    [Key]
    [StringLength(128)]
    public required string Id { get; set; }

    /// <summary>Normalized category token used in generated ids and filtering.</summary>
    [Required]
    [StringLength(128)]
    public required string Category { get; set; }

    /// <summary>Memory scope: <c>Global</c> or <c>Workspace</c>.</summary>
    [Required]
    [StringLength(16)]
    public required string Scope { get; set; }

    /// <summary>
    /// Optional workspace owner. Null for Global memories; required for
    /// Workspace memories.
    /// </summary>
    [StringLength(1024)]
    public string? WorkspaceId { get; set; }

    /// <summary>Raw memory text stored exactly as the service receives it after line sanitization.</summary>
    [Required]
    public required string Text { get; set; }

    /// <summary>Monotonic version incremented on each memory update.</summary>
    public int Version { get; set; } = 1;

    /// <summary>UTC timestamp when the memory row was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the memory row was last changed.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Optional actor or subsystem that last changed the memory.</summary>
    [StringLength(256)]
    public string? UpdatedBy { get; set; }

    /// <summary>FR-MCP-MEMORY-010: Optional multi-layer title.</summary>
    [StringLength(256)]
    public string? Title { get; set; }

    /// <summary>FR-MCP-MEMORY-010: Optional multi-layer summary.</summary>
    public string? Summary { get; set; }

    /// <summary>FR-MCP-MEMORY-010: Multi-layer content. Legacy <see cref="Text"/> backfills this field.</summary>
    public string? Content { get; set; }

    /// <summary>FR-MCP-MEMORY-010: Memory type token (fact, decision, preference, procedure, entity, other).</summary>
    [StringLength(64)]
    public string? Type { get; set; }

    /// <summary>FR-MCP-MEMORY-010: JSON-serialized tags array.</summary>
    public string? TagsJson { get; set; }

    /// <summary>FR-MCP-MEMORY-010: Confidence in [0,1].</summary>
    public double? Confidence { get; set; }

    /// <summary>FR-MCP-MEMORY-010: Optional provenance kind.</summary>
    [StringLength(64)]
    public string? SourceKind { get; set; }

    /// <summary>FR-MCP-MEMORY-010: Optional provenance reference.</summary>
    [StringLength(1024)]
    public string? SourceRef { get; set; }

    /// <summary>FR-MCP-MEMORY-010: Create attribution.</summary>
    [StringLength(256)]
    public string? CreatedBy { get; set; }

    /// <summary>TR-MCP-MEMORY-MODEL-002: Embedding indexer status (pending/ready/failed).</summary>
    [StringLength(32)]
    public string? EmbeddingStatus { get; set; }
}
