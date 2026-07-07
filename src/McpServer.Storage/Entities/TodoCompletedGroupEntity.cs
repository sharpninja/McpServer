using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TODO-005: 4NF completed-archive group. One row per completion date group, replacing the
/// outer level of the document metadata's <c>CompletedJson</c> column. Written/read from the
/// dependent side because the composite (WorkspaceId, SingletonId) parent key includes the tenant
/// column. Items hang off <see cref="TodoCompletedItemEntity"/>.
/// </summary>
public sealed class TodoCompletedGroupEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Owning document-metadata workspace discriminator (part of the composite parent key).</summary>
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Owning document-metadata singleton key (always 1; part of the composite parent key).</summary>
    public int SingletonId { get; set; } = 1;

    /// <summary>Ordinal position of the group within the completed archive.</summary>
    public int Ordinal { get; set; }

    /// <summary>Completion date label for the group.</summary>
    [MaxLength(64)]
    public string? Date { get; set; }

    /// <summary>Navigation to the owning document metadata singleton.</summary>
    public TodoDocumentMetadataEntity? DocumentMetadata { get; set; }

    /// <summary>The completed items in this group (single-column FK, safe as a real navigation).</summary>
    public List<TodoCompletedItemEntity> Items { get; set; } = [];
}
