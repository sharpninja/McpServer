using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TODO-005: 4NF implementation-task entity for a TODO item. One row per <c>{task, done}</c>
/// sub-task, replacing the item's <c>ImplementationTasksJson</c> column. Written/read from the
/// dependent side (composite (WorkspaceId, TodoId) parent key includes the tenant column).
/// </summary>
public sealed class TodoItemTaskEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Owning TODO workspace discriminator (part of the composite parent key).</summary>
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Owning TODO id (part of the composite parent key).</summary>
    [StringLength(128)]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Ordinal position within the implementation-tasks list.</summary>
    public int Ordinal { get; set; }

    /// <summary>Sub-task text.</summary>
    public required string Task { get; set; }

    /// <summary>Whether the sub-task is done.</summary>
    public bool Done { get; set; }

    /// <summary>Navigation to the owning TODO item.</summary>
    public TodoItemEntity? TodoItem { get; set; }
}
