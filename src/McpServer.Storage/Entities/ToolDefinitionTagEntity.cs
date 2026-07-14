using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// Keyword tag associated with a <see cref="ToolDefinitionEntity"/>.
/// Agents query tools by keyword; tags are matched case-insensitively.
/// A tool may have many tags (e.g. <c>screenshot</c>, <c>capture</c>, <c>image</c>).
/// </summary>
public sealed class ToolDefinitionTagEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public int Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Foreign key to the parent tool definition.</summary>
    public int ToolDefinitionId { get; set; }

    /// <summary>Navigation to the parent tool definition.</summary>
    public ToolDefinitionEntity? ToolDefinition { get; set; }

    /// <summary>Lowercase keyword tag (e.g. <c>screenshot</c>, <c>clipboard</c>).</summary>
    [Required]
    [StringLength(128)]
    public required string Tag { get; set; }
}
