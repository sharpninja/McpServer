using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TODO-005 (provider-agnostic) + TR-MCP-TODO-008 (workspace-scoped):
/// Authoritative TODO item row. Stored via <c>McpDbContext</c> and routed to
/// whichever provider <c>Mcp:Database:Provider</c> selects (TR-MCP-CFG-007).
/// </summary>
/// <remarks>
/// TODO items are workspace-scoped per TR-MCP-TODO-008: every row carries a
/// <c>WorkspaceId</c> stamped by <c>StampWorkspaceId</c> and is clamped by the
/// global query filter installed in <c>McpDbContext.OnModelCreating</c>. The
/// primary key is the composite <c>(WorkspaceId, Id)</c> so the same canonical
/// TODO id may coexist across workspaces without collision (matches the
/// TR-MCP-MT-003 multi-tenant pattern used by context, session-log, agent,
/// tool, and graph entities).
/// </remarks>
public sealed class TodoItemEntity
{
    /// <summary>
    /// Workspace discriminator (TR-MCP-MT-003 pattern); the absolute workspace
    /// path resolved from <c>WorkspaceContext</c>. Part of the composite
    /// primary key with <see cref="Id"/>.
    /// </summary>
    [Required]
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Canonical TODO identifier (e.g. <c>MVP-APP-001</c>, <c>ISSUE-17</c>).</summary>
    [MaxLength(128)]
    public required string Id { get; set; }

    /// <summary>Short human-readable title.</summary>
    [Required]
    [MaxLength(1024)]
    public required string Title { get; set; }

    /// <summary>Section key (e.g. <c>mvp-app</c>, <c>mvp-support</c>).</summary>
    [Required]
    [MaxLength(128)]
    public required string Section { get; set; }

    /// <summary>Priority level: <c>high</c> | <c>medium</c> | <c>low</c>.</summary>
    [Required]
    [MaxLength(16)]
    public required string Priority { get; set; }

    /// <summary>Done flag.</summary>
    public bool Done { get; set; }

    /// <summary>Effort estimate (free text, e.g. <c>96-128 hours</c>).</summary>
    [MaxLength(128)]
    public string? Estimate { get; set; }

    /// <summary>Optional inline note.</summary>
    [MaxLength(4096)]
    public string? Note { get; set; }

    /// <summary>Description lines serialized as JSON array of strings.</summary>
    public string? DescriptionJson { get; set; }

    /// <summary>Technical detail lines serialized as JSON array of strings.</summary>
    public string? TechnicalDetailsJson { get; set; }

    /// <summary>Implementation sub-tasks serialized as JSON array of <c>{task, done}</c> objects.</summary>
    public string? ImplementationTasksJson { get; set; }

    /// <summary>Completion date (ISO-8601 or free text) when Done.</summary>
    [MaxLength(64)]
    public string? CompletedDate { get; set; }

    /// <summary>Done summary text.</summary>
    public string? DoneSummary { get; set; }

    /// <summary>Remaining work text.</summary>
    public string? Remaining { get; set; }

    /// <summary>Priority note override.</summary>
    [MaxLength(1024)]
    public string? PriorityNote { get; set; }

    /// <summary>Reference link or document path.</summary>
    [MaxLength(1024)]
    public string? Reference { get; set; }

    /// <summary>Depends-on IDs serialized as JSON array of strings.</summary>
    public string? DependsOnJson { get; set; }

    /// <summary>Functional requirement IDs serialized as JSON array of strings.</summary>
    public string? FunctionalRequirementsJson { get; set; }

    /// <summary>Technical requirement IDs serialized as JSON array of strings.</summary>
    public string? TechnicalRequirementsJson { get; set; }

    /// <summary>Item kind discriminator (default <c>standard</c>).</summary>
    [Required]
    [MaxLength(32)]
    public string ItemKind { get; set; } = "standard";

    /// <summary>Sort order of the containing section.</summary>
    public int SectionOrder { get; set; }

    /// <summary>Sort order of the item inside its section.</summary>
    public int ItemOrder { get; set; }

    /// <summary>Code-review phase label for remediation items.</summary>
    [MaxLength(128)]
    public string? PhaseLabel { get; set; }
}
