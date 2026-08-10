using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-USECASE-003 / TR-MCP-USECASE-001: Link from a use case to a functional requirement
/// identified by string FR id (<see cref="RequirementEntity"/> with Kind = fr).
/// </summary>
public sealed class UseCaseFrLinkEntity
{
    /// <summary>Surrogate primary key.</summary>
    [Key]
    public long LinkId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Parent use case id.</summary>
    public long UseCaseId { get; set; }

    /// <summary>Functional requirement id (string, e.g. FR-MCP-001).</summary>
    [Required]
    [StringLength(128)]
    public string FrId { get; set; } = string.Empty;

    /// <summary>Requirement kind; fixed to <c>fr</c> for FK clarity.</summary>
    [Required]
    [StringLength(16)]
    public string FrKind { get; set; } = "fr";

    /// <summary>Link type; default Realizes.</summary>
    [Required]
    [StringLength(20)]
    public string LinkType { get; set; } = "Realizes";

    /// <summary>Optional ordering among links.</summary>
    public int LinkOrder { get; set; }

    /// <summary>Optional notes.</summary>
    public string? Notes { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>Parent use case.</summary>
    public UseCaseEntity UseCase { get; set; } = null!;

    /// <summary>Linked functional requirement row.</summary>
    public RequirementEntity? FunctionalRequirement { get; set; }
}
