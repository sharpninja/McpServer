using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TODO-005: 4NF completed-archive item. One row per completed item summary within a
/// <see cref="TodoCompletedGroupEntity"/>, replacing the inner level of the document metadata's
/// <c>CompletedJson</c> column.
/// </summary>
public sealed class TodoCompletedItemEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Workspace discriminator (mirrors the owning group's workspace).</summary>
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Foreign key to the owning completed group.</summary>
    public long GroupId { get; set; }

    /// <summary>Ordinal position within the group.</summary>
    public int Ordinal { get; set; }

    /// <summary>Completed TODO identifier.</summary>
    [StringLength(128)]
    public string? ItemId { get; set; }

    /// <summary>Qualifier or category for the completed item.</summary>
    [StringLength(256)]
    public string? Qualifier { get; set; }

    /// <summary>Summary of what was accomplished.</summary>
    public string? Summary { get; set; }

    /// <summary>Navigation to the owning completed group.</summary>
    public TodoCompletedGroupEntity? Group { get; set; }
}
