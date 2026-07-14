using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-105 / TR-MCP-DB-001: 4NF generic policy-list entity for a workspace. One row per banned
/// value across the license, country-of-origin, organization, and individual policy lists.
/// Eliminates the multi-valued dependencies previously stored as the workspace's
/// <c>BannedLicensesJson</c>, <c>BannedCountriesOfOriginJson</c>, <c>BannedOrganizationsJson</c>,
/// and <c>BannedIndividualsJson</c> columns.
/// </summary>
public sealed class WorkspaceBannedItemEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>
    /// Owning workspace id. Doubles as the multi-tenant discriminator and the foreign key to
    /// <see cref="WorkspaceEntity.WorkspaceId"/>.
    /// </summary>
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Discriminator identifying which policy list this item belongs to (License, Country, Organization, Individual).</summary>
    [Required]
    [StringLength(32)]
    public required string Category { get; set; }

    /// <summary>Ordinal position within the policy list.</summary>
    public int Ordinal { get; set; }

    /// <summary>The banned value (SPDX id, country code, organization, or individual name/handle).</summary>
    public required string Value { get; set; }

    /// <summary>Navigation to the owning workspace.</summary>
    public WorkspaceEntity? Workspace { get; set; }
}
