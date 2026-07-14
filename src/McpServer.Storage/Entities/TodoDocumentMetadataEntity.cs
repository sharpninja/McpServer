using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TODO-005 / TR-MCP-TODO-006 (provider-agnostic): Singleton row capturing
/// document-level TODO state (top-level notes, completed archive, projection
/// health). Routed through <c>McpDbContext</c> to whichever provider
/// <c>Mcp:Database:Provider</c> selects.
/// </summary>
/// <remarks>
/// Singleton pattern: <see cref="SingletonId"/> is fixed at <c>1</c>. The service
/// layer ensures a seed row exists before reads or writes; the check constraint
/// is enforced via FluentAPI configuration in <c>McpDbContext</c>.
/// </remarks>
public sealed class TodoDocumentMetadataEntity
{
    /// <summary>
    /// TR-MCP-TODO-008 workspace discriminator (the absolute workspace path
    /// resolved from <c>WorkspaceContext</c>). Part of the composite primary
    /// key with <see cref="SingletonId"/> so each workspace owns exactly one
    /// document-metadata singleton without collision.
    /// </summary>
    [Required]
    [StringLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Fixed singleton key (always <c>1</c>). Composite-PK component.</summary>
    public int SingletonId { get; set; } = 1;

    /// <summary>TR-MCP-TODO-005: 4NF top-level note rows (former <c>NotesJson</c>).</summary>
    /// <remarks>
    /// Not an EF navigation: loaded/attached explicitly and written from the dependent side
    /// (the composite (WorkspaceId, SingletonId) parent key includes the tenant column; see
    /// <c>RequirementAcceptanceCriterionEntity</c> for the rationale).
    /// </remarks>
    [NotMapped]
    public List<TodoDocumentNoteEntity> Notes { get; set; } = [];

    /// <summary>TR-MCP-TODO-005: 4NF completed-archive group rows (former <c>CompletedJson</c>).</summary>
    /// <remarks>Not an EF navigation; same dependent-side handling as <see cref="Notes"/>.</remarks>
    [NotMapped]
    public List<TodoCompletedGroupEntity> CompletedGroups { get; set; } = [];

    /// <summary>Free-text reference to the code-review document anchor.</summary>
    [StringLength(1024)]
    public string? CodeReviewReference { get; set; }

    /// <summary>UTC timestamp of last successful import from <c>TODO.yaml</c> (ISO-8601).</summary>
    [StringLength(64)]
    public string? LastImportedFromYamlUtc { get; set; }

    /// <summary>UTC timestamp of last successful projection to <c>TODO.yaml</c> (ISO-8601).</summary>
    [StringLength(64)]
    public string? LastProjectedToYamlUtc { get; set; }

    /// <summary>UTC timestamp of the most recent projection failure (ISO-8601).</summary>
    [StringLength(64)]
    public string? LastProjectionFailureUtc { get; set; }

    /// <summary>Free-text error message captured from the most recent projection failure.</summary>
    public string? LastProjectionFailureMessage { get; set; }
}
