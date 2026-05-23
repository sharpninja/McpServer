using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-DB-005: Durable TODO lifecycle anchor used by relational TODO
/// requirement links while legacy TODO item JSON projections remain available.
/// </summary>
public sealed class TodoRecordEntity
{
    /// <summary>Workspace discriminator for the TODO anchor.</summary>
    [Required]
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Canonical TODO identifier.</summary>
    [Required]
    [MaxLength(128)]
    public string TodoId { get; set; } = string.Empty;

    /// <summary>UTC timestamp when the anchor was created.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC timestamp when the anchor was last updated.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
