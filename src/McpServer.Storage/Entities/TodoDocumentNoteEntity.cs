using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TODO-005: 4NF top-level TODO document note. One row per note line, replacing the
/// document metadata's <c>NotesJson</c> column. Written/read from the dependent side because the
/// composite (WorkspaceId, SingletonId) parent key includes the tenant column.
/// </summary>
public sealed class TodoDocumentNoteEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Owning document-metadata workspace discriminator (part of the composite parent key).</summary>
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Owning document-metadata singleton key (always 1; part of the composite parent key).</summary>
    public int SingletonId { get; set; } = 1;

    /// <summary>Ordinal position within the notes block.</summary>
    public int Ordinal { get; set; }

    /// <summary>The note text.</summary>
    public required string Value { get; set; }

    /// <summary>Navigation to the owning document metadata singleton.</summary>
    public TodoDocumentMetadataEntity? DocumentMetadata { get; set; }
}
