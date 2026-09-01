using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-PRODUCT-MODEL-001 / FR-MCP-PRODUCT-002: Host-global product membership.
/// Soft-delete metadata is applied as shadow properties by <c>McpDbContext</c>.
/// Do not add a workspace query filter.
/// </summary>
public sealed class ProductWorkspaceMembershipEntity
{
    /// <summary>Owning product id.</summary>
    public long ProductId { get; set; }

    /// <summary>Member workspace id (FK to Workspaces).</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Membership role: <c>Owner</c> or <c>Member</c>.</summary>
    [Required]
    [StringLength(32)]
    public string Role { get; set; } = "Member";

    /// <summary>UTC timestamp when the membership was added.</summary>
    public DateTimeOffset AddedAtUtc { get; set; }

    /// <summary>Workspace id of the actor that added this membership.</summary>
    [Required]
    [StringLength(1024)]
    public string AddedBy { get; set; } = string.Empty;

    /// <summary>Owning product.</summary>
    public ProductEntity? Product { get; set; }
}
