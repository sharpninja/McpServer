using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-PRODUCT-MODEL-001 / FR-MCP-PRODUCT-001: Host-global product grouping.
/// Soft-delete metadata is applied as shadow properties by <c>McpDbContext</c>.
/// Do not add a workspace query filter.
/// </summary>
public sealed class ProductEntity
{
    /// <summary>Surrogate primary key.</summary>
    [Key]
    public long ProductId { get; set; }

    /// <summary>Canonical product key (example <c>PROD-MCPSERVER</c>).</summary>
    [Required]
    [StringLength(128)]
    public string Key { get; set; } = string.Empty;

    /// <summary>Display name.</summary>
    [Required]
    [StringLength(512)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Owner workspace id (FK to Workspaces).</summary>
    [Required]
    [StringLength(1024)]
    public string OwnerWorkspaceId { get; set; } = string.Empty;

    /// <summary>UTC create timestamp.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>Membership rows, including the owner.</summary>
    public ICollection<ProductWorkspaceMembershipEntity> Memberships { get; set; } = new List<ProductWorkspaceMembershipEntity>();
}
