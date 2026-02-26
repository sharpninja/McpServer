using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// A tool bucket is a GitHub repository containing tool manifest files (JSON),
/// similar to Scoop package manager buckets. Each manifest defines a tool's
/// name, description, tags, command template, and parameter schema.
/// </summary>
public sealed class ToolBucketEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Short unique name for this bucket (e.g. <c>official</c>, <c>community</c>).</summary>
    [Required]
    [MaxLength(128)]
    public required string Name { get; set; }

    /// <summary>GitHub repository owner (user or org).</summary>
    [Required]
    [MaxLength(256)]
    public required string Owner { get; set; }

    /// <summary>GitHub repository name.</summary>
    [Required]
    [MaxLength(256)]
    public required string Repo { get; set; }

    /// <summary>Branch to read manifests from (default: <c>main</c>).</summary>
    [Required]
    [MaxLength(128)]
    public string Branch { get; set; } = "main";

    /// <summary>Path within the repo where tool manifests live (default: root <c>/</c>).</summary>
    [Required]
    [MaxLength(512)]
    public string ManifestPath { get; set; } = "/";

    /// <summary>When the bucket was added.</summary>
    public DateTimeOffset DateTimeCreated { get; set; }

    /// <summary>When the bucket was last synced (manifests refreshed).</summary>
    public DateTimeOffset? DateTimeLastSynced { get; set; }
}
