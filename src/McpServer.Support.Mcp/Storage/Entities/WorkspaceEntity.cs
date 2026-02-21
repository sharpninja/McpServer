using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// Workspace registration entity. Each workspace maps to a hosted MCP instance
/// on a dedicated port, keyed by its root folder path.
/// </summary>
public sealed class WorkspaceEntity
{
    /// <summary>Absolute path to the workspace root folder (primary key).</summary>
    [Key]
    [MaxLength(2048)]
    public required string WorkspacePath { get; set; }

    /// <summary>Human-readable workspace name. Defaults to the last segment of WorkspacePath.</summary>
    [Required]
    [MaxLength(256)]
    public required string Name { get; set; }

    /// <summary>Relative path to todo.yaml within the workspace.</summary>
    [Required]
    [MaxLength(2048)]
    public string TodoPath { get; set; } = "docs/todo.yaml";

    /// <summary>HTTP port for this workspace's hosted MCP instance.</summary>
    public int WorkspacePort { get; set; }

    /// <summary>Tunnel provider key (ngrok, cloudflare, frp) or null if disabled.</summary>
    [MaxLength(64)]
    public string? TunnelProvider { get; set; }

    /// <summary>When the workspace was registered.</summary>
    public DateTimeOffset DateTimeCreated { get; set; }

    /// <summary>When the workspace was last updated.</summary>
    public DateTimeOffset DateTimeModified { get; set; }

    /// <summary>Identity for the child process (null = current Windows user).</summary>
    [MaxLength(256)]
    public string? RunAs { get; set; }
}
