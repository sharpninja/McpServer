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

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Unique tool name (e.g. <c>screenshot</c>, <c>clipboard_copy</c>).</summary>
    [Required]
    [StringLength(128)]
    public required string Name { get; set; }

    /// <summary>Short human-readable description shown to agents.</summary>
    [Required]
    [StringLength(1024)]
    public required string Description { get; set; }

    /// <summary>JSON schema describing the tool's input parameters (optional).</summary>
    [StringLength(8192)]
    public string? ParameterSchema { get; set; }

    /// <summary>Command or executable template for invocation (e.g. <c>pwsh.exe -File Take-Screenshot.ps1 -Path {path}</c>).</summary>
    [StringLength(2048)]
    public string? CommandTemplate { get; set; }

    /// <summary>
    /// Optional workspace scope. When <c>null</c> the tool is global (available to all workspaces).
    /// When set, the tool is visible only within that workspace (plus all global tools).
    /// </summary>
    [StringLength(2048)]
    public string? WorkspacePath { get; set; }

    /// <summary>
    /// Name of the bucket this tool was installed from, or <c>null</c> if created manually.
    /// Used to track provenance and enable updates from the source bucket.
    /// </summary>
    [StringLength(128)]
    public string? BucketName { get; set; }

    /// <summary>When the tool was registered.</summary>
    public DateTimeOffset DateTimeCreated { get; set; }

    /// <summary>When the tool was last modified.</summary>
    public DateTimeOffset DateTimeModified { get; set; }

    /// <summary>Tags for keyword-based discovery. EF Core populates the initialized collection during relationship fixup.</summary>
    public ICollection<ToolDefinitionTagEntity> Tags { get; } = new List<ToolDefinitionTagEntity>();
}
