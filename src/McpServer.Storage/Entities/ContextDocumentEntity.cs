using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-CORE-013: Persisted document record for MCP indexing.
/// FR-SUPPORT-010: Stored in SQLite for full-text and metadata.
/// </summary>
public sealed class ContextDocumentEntity
{
    /// <summary>TR-PLANNED-CORE-013: Unique document identifier.</summary>
    [Key]
    [MaxLength(256)]
    public required string Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>FR-SUPPORT-010: Source type (repo, session-log, external-doc, issue, pr).</summary>
    [Required]
    [MaxLength(64)]
    public required string SourceType { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Source path or URL.</summary>
    [Required]
    [MaxLength(2048)]
    public required string SourceKey { get; set; }

    /// <summary>FR-SUPPORT-010: Last ingestion timestamp (UTC).</summary>
    public DateTime IngestedAt { get; set; }

    /// <summary>TR-PLANNED-CORE-013: Hash for change detection.</summary>
    [Required]
    [MaxLength(64)]
    public required string ContentHash { get; set; }

    /// <summary>Navigation to chunks. EF Core requires mutable collection for relationship fixup.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF Core navigation collection")]
    public ICollection<ContextChunkEntity> Chunks { get; set; } = new List<ContextChunkEntity>();
}
