using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// Authoritative database row for one functional, technical, or testing
/// requirement. Markdown files are import/export projections only.
/// </summary>
public sealed class RequirementEntity
{
    /// <summary>Resolved workspace discriminator, normally the absolute workspace path.</summary>
    [Required]
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Requirement kind: <c>fr</c>, <c>tr</c>, or <c>test</c>.</summary>
    [Required]
    [MaxLength(16)]
    public string Kind { get; set; } = string.Empty;

    /// <summary>Canonical requirement identifier.</summary>
    [Required]
    [MaxLength(128)]
    public string Id { get; set; } = string.Empty;

    /// <summary>Human-readable title for FR/TR rows.</summary>
    [MaxLength(1024)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Requirement body or testing condition text.</summary>
    public string Body { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the row was first created.</summary>
    [Required]
    [MaxLength(64)]
    public string CreatedAtUtc { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the row was last modified.</summary>
    [Required]
    [MaxLength(64)]
    public string UpdatedAtUtc { get; set; } = string.Empty;
}
