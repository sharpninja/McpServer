using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-DB-005: Normalized link from a TODO lifecycle anchor to an FR, TR, or
/// TEST requirement. Existing TODO JSON requirement fields are compatibility
/// projections of these rows.
/// </summary>
public sealed class TodoRequirementLinkEntity
{
    /// <summary>Workspace discriminator for both TODO and requirement.</summary>
    [Required]
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Canonical TODO identifier.</summary>
    [Required]
    [MaxLength(128)]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>Requirement kind: fr, tr, or test.</summary>
    [Required]
    [MaxLength(16)]
    public string RequirementKind { get; set; } = string.Empty;

    /// <summary>Requirement identifier.</summary>
    [Required]
    [MaxLength(128)]
    public string RequirementId { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the link was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Canonical TODO item navigation.</summary>
    public TodoItemEntity? TodoItem { get; set; }

    /// <summary>Requirement navigation.</summary>
    public RequirementEntity? Requirement { get; set; }
}
