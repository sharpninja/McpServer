using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-001: Workspace-scoped actor participating in use cases.
/// </summary>
public sealed class ActorEntity
{
    /// <summary>Surrogate primary key.</summary>
    [Key]
    public long ActorId { get; set; }

    /// <summary>Normalized workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Actor display name.</summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Actor type: Primary, Secondary, System, or External.</summary>
    [Required]
    [StringLength(20)]
    public string Type { get; set; } = "Primary";

    /// <summary>Use case associations.</summary>
    public ICollection<UseCaseActorEntity> UseCaseActors { get; set; } = new List<UseCaseActorEntity>();
}
