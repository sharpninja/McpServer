using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-USECASE-002 / TR-MCP-USECASE-001: Join of use case to actor within a workspace.
/// </summary>
public sealed class UseCaseActorEntity
{
    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Parent use case id.</summary>
    public long UseCaseId { get; set; }

    /// <summary>Linked actor id.</summary>
    public long ActorId { get; set; }

    /// <summary>Whether this actor is primary for the use case.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>Parent use case.</summary>
    public UseCaseEntity UseCase { get; set; } = null!;

    /// <summary>Linked actor.</summary>
    public ActorEntity Actor { get; set; } = null!;
}
