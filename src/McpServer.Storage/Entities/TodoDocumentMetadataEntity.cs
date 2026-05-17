using System.ComponentModel.DataAnnotations;

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
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Fixed singleton key (always <c>1</c>). Composite-PK component.</summary>
    public int SingletonId { get; set; } = 1;

    /// <summary>JSON-serialized top-level notes block.</summary>
    public string? NotesJson { get; set; }

    /// <summary>JSON-serialized archive of completed TODO items.</summary>
    public string? CompletedJson { get; set; }

    /// <summary>Free-text reference to the code-review document anchor.</summary>
    [MaxLength(1024)]
    public string? CodeReviewReference { get; set; }

    /// <summary>UTC timestamp of last successful import from <c>TODO.yaml</c> (ISO-8601).</summary>
    [MaxLength(64)]
    public string? LastImportedFromYamlUtc { get; set; }

    /// <summary>UTC timestamp of last successful projection to <c>TODO.yaml</c> (ISO-8601).</summary>
    [MaxLength(64)]
    public string? LastProjectedToYamlUtc { get; set; }

    /// <summary>UTC timestamp of the most recent projection failure (ISO-8601).</summary>
    [MaxLength(64)]
    public string? LastProjectionFailureUtc { get; set; }

    /// <summary>Free-text error message captured from the most recent projection failure.</summary>
    public string? LastProjectionFailureMessage { get; set; }
}
