using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-103: Durable hub-side mapping of a workspace hosted by a local proxy.
/// </summary>
public sealed class FederationWorkspaceEntity
{
    /// <summary>Database-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Hub-wide workspace identifier used in federation headers.</summary>
    [Required]
    [MaxLength(512)]
    public required string GlobalWorkspaceId { get; set; }

    /// <summary>Proxy that hosts the workspace.</summary>
    [Required]
    [MaxLength(256)]
    public required string ProxyId { get; set; }

    /// <summary>Workspace display name reported by the proxy.</summary>
    [MaxLength(512)]
    public string? WorkspaceName { get; set; }

    /// <summary>Proxy-local workspace path.</summary>
    [Required]
    [MaxLength(2048)]
    public required string WorkspacePath { get; set; }

    /// <summary>Whether the workspace is currently enabled on the proxy.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Opaque workspace metadata JSON reported by the proxy.</summary>
    public string? MetadataJson { get; set; }

    /// <summary>Monotonic proxy-reported workspace version, when available.</summary>
    [MaxLength(256)]
    public string? Version { get; set; }

    /// <summary>UTC timestamp when the workspace was first registered with the hub.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the workspace was last seen or updated.</summary>
    public DateTimeOffset LastSeenUtc { get; set; }
}
