using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-001: Ordered step within a use case flow.
/// </summary>
public sealed class UseCaseStepEntity
{
    /// <summary>Surrogate primary key.</summary>
    [Key]
    public long StepId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Parent flow id.</summary>
    public long FlowId { get; set; }

    /// <summary>Order among steps on the flow.</summary>
    public int StepNumber { get; set; }

    /// <summary>Optional acting actor id.</summary>
    public long? ActorId { get; set; }

    /// <summary>Actor action text.</summary>
    [Required]
    public string Action { get; set; } = string.Empty;

    /// <summary>Optional system response text.</summary>
    public string? SystemResponse { get; set; }

    /// <summary>Optional data-entity notes.</summary>
    public string? DataEntities { get; set; }

    /// <summary>Parent flow.</summary>
    public UseCaseFlowEntity Flow { get; set; } = null!;

    /// <summary>Optional actor navigation.</summary>
    public ActorEntity? Actor { get; set; }
}
