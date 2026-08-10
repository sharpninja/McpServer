using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-001: Extension point on a use case.
/// </summary>
public sealed class UseCaseExtensionPointEntity
{
    /// <summary>Surrogate primary key.</summary>
    [Key]
    public long ExtensionPointId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Parent use case id.</summary>
    public long UseCaseId { get; set; }

    /// <summary>Extension point name.</summary>
    [Required]
    [StringLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>Optional description.</summary>
    public string? Description { get; set; }

    /// <summary>Parent use case.</summary>
    public UseCaseEntity UseCase { get; set; } = null!;
}
