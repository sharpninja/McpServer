using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// Registered tool definition that agents can discover via keyword search.
/// Tools are either <b>global</b> (<see cref="WorkspacePath"/> is <c>null</c>) or
/// <b>workspace-scoped</b> (tied to a specific workspace). Keyword queries return
/// the union of global tools and tools belonging to the queried workspace.
/// </summary>
public sealed class ToolDefinitionEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>Unique tool name (e.g. <c>screenshot</c>, <c>clipboard_copy</c>).</summary>
    [Required]
    [MaxLength(128)]
    public required string Name { get; set; }

    /// <summary>Short human-readable description shown to agents.</summary>
    [Required]
    [MaxLength(1024)]
    public required string Description { get; set; }

    /// <summary>JSON schema describing the tool's input parameters (optional).</summary>
    [MaxLength(8192)]
    public string? ParameterSchema { get; set; }

    /// <summary>Command or executable template for invocation (e.g. <c>powershell -File Take-Screenshot.ps1 -Path {path}</c>).</summary>
    [MaxLength(2048)]
    public string? CommandTemplate { get; set; }

    /// <summary>
    /// Optional workspace scope. When <c>null</c> the tool is global (available to all workspaces).
    /// When set, the tool is visible only within that workspace (plus all global tools).
    /// </summary>
    [MaxLength(2048)]
    public string? WorkspacePath { get; set; }

    /// <summary>Navigation to the owning workspace (null for global tools).</summary>
    public WorkspaceEntity? Workspace { get; set; }

    /// <summary>
    /// Name of the bucket this tool was installed from, or <c>null</c> if created manually.
    /// Used to track provenance and enable updates from the source bucket.
    /// </summary>
    [MaxLength(128)]
    public string? BucketName { get; set; }

    /// <summary>When the tool was registered.</summary>
    public DateTimeOffset DateTimeCreated { get; set; }

    /// <summary>When the tool was last modified.</summary>
    public DateTimeOffset DateTimeModified { get; set; }

    /// <summary>Tags for keyword-based discovery. EF Core requires mutable collection for relationship fixup.</summary>
    [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "CA2227:Collection properties should be read only", Justification = "EF Core navigation collection")]
    public ICollection<ToolDefinitionTagEntity> Tags { get; set; } = new List<ToolDefinitionTagEntity>();
}
