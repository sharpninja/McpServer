using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-001: Special requirement attached to a use case.
/// </summary>
public sealed class UseCaseSpecialRequirementEntity
{
    /// <summary>Surrogate primary key.</summary>
    [Key]
    public long SpecialReqId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Parent use case id.</summary>
    public long UseCaseId { get; set; }

    /// <summary>Optional category.</summary>
    [StringLength(50)]
    public string? Category { get; set; }

    /// <summary>Requirement text.</summary>
    [Required]
    public string RequirementText { get; set; } = string.Empty;

    /// <summary>Optional priority.</summary>
    public int? Priority { get; set; }

    /// <summary>Parent use case.</summary>
    public UseCaseEntity UseCase { get; set; } = null!;
}
