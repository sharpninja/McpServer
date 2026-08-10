using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-001: Basic, alternative, or exception flow for a use case.
/// </summary>
public sealed class UseCaseFlowEntity
{
    /// <summary>Surrogate primary key.</summary>
    [Key]
    public long FlowId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Parent use case id.</summary>
    public long UseCaseId { get; set; }

    /// <summary>Flow type: Basic, Alternative, or Exception.</summary>
    [Required]
    [StringLength(20)]
    public string FlowType { get; set; } = "Basic";

    /// <summary>Optional flow name.</summary>
    [StringLength(100)]
    public string? Name { get; set; }

    /// <summary>Order among flows on the use case.</summary>
    public int SequenceNumber { get; set; }

    /// <summary>Parent use case.</summary>
    public UseCaseEntity UseCase { get; set; } = null!;

    /// <summary>Steps in this flow.</summary>
    public ICollection<UseCaseStepEntity> Steps { get; set; } = new List<UseCaseStepEntity>();
}
