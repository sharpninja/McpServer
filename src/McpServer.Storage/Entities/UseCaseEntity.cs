using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-USECASE-001 / TR-MCP-USECASE-001: Workspace-scoped use case header row.
/// Soft-delete metadata is applied as shadow properties by <c>McpDbContext</c>.
/// </summary>
public sealed class UseCaseEntity
{
    /// <summary>Surrogate primary key.</summary>
    [Key]
    public long UseCaseId { get; set; }

    /// <summary>Normalized workspace discriminator (FK to Workspaces).</summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Use case title.</summary>
    [Required]
    [StringLength(200)]
    public string Title { get; set; } = string.Empty;

    /// <summary>Optional brief description.</summary>
    public string? BriefDescription { get; set; }

    /// <summary>Optional precondition text.</summary>
    public string? Precondition { get; set; }

    /// <summary>Optional postcondition text.</summary>
    public string? Postcondition { get; set; }

    /// <summary>Optional scope label.</summary>
    [StringLength(50)]
    public string? Scope { get; set; }

    /// <summary>Numeric priority (higher means more important unless callers define otherwise).</summary>
    public int Priority { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }

    /// <summary>FR-MCP-USECASE-008: Monotonic version number; increments on approved revisions.</summary>
    public int VersionNumber { get; set; } = 1;

    /// <summary>FR-MCP-USECASE-008: Approval status (Draft, Submitted, Approved, Rejected).</summary>
    [Required]
    [StringLength(32)]
    public string ApprovalStatus { get; set; } = "Draft";

    /// <summary>FR-MCP-USECASE-009: Optional product membership key for multi-workspace sharing.</summary>
    [StringLength(128)]
    public string? ProductKey { get; set; }

    /// <summary>
    /// FR-MCP-USECASE-012 / TR-MCP-USECASE-011: UML use-case diagram graph JSON (schema v1).
    /// Null means no saved canvas graph yet.
    /// </summary>
    public string? DiagramGraphJson { get; set; }

    /// <summary>Actor associations.</summary>
    public ICollection<UseCaseActorEntity> UseCaseActors { get; set; } = new List<UseCaseActorEntity>();

    /// <summary>Flows for this use case.</summary>
    public ICollection<UseCaseFlowEntity> Flows { get; set; } = new List<UseCaseFlowEntity>();

    /// <summary>Special requirements.</summary>
    public ICollection<UseCaseSpecialRequirementEntity> SpecialRequirements { get; set; } = new List<UseCaseSpecialRequirementEntity>();

    /// <summary>Extension points.</summary>
    public ICollection<UseCaseExtensionPointEntity> ExtensionPoints { get; set; } = new List<UseCaseExtensionPointEntity>();

    /// <summary>FR links.</summary>
    public ICollection<UseCaseFrLinkEntity> FrLinks { get; set; } = new List<UseCaseFrLinkEntity>();
}
