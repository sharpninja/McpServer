using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// FR-MCP-105 / TR-MCP-DB-001: Canonical database row for a registered MCP
/// workspace. Appsettings workspace entries are informational projections of
/// this table, not the source of truth.
/// </summary>
public sealed class WorkspaceEntity
{
    /// <summary>
    /// Stable workspace identifier. For local workspaces this is the normalized
    /// absolute workspace path; the empty string is reserved for global rows.
    /// </summary>
    [Key]
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Normalized workspace root path, or empty for the global row.</summary>
    [MaxLength(2048)]
    public string WorkspacePath { get; set; } = string.Empty;

    /// <summary>Human-readable workspace name.</summary>
    [Required]
    [MaxLength(512)]
    public string Name { get; set; } = "workspace";

    /// <summary>Relative TODO file path for the workspace.</summary>
    [Required]
    [MaxLength(1024)]
    public string TodoPath { get; set; } = "docs/todo.yaml";

    /// <summary>Optional data directory override.</summary>
    [MaxLength(2048)]
    public string? DataDirectory { get; set; }

    /// <summary>Optional tunnel provider key.</summary>
    [MaxLength(128)]
    public string? TunnelProvider { get; set; }

    /// <summary>Optional process identity for child workspace processes.</summary>
    [MaxLength(512)]
    public string? RunAs { get; set; }

    /// <summary>True when the host serves this workspace directly.</summary>
    public bool IsPrimary { get; set; }

    /// <summary>True when the workspace should be started or shown by default.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Optional marker prompt template override.</summary>
    public string? PromptTemplate { get; set; }

    /// <summary>Optional Copilot status prompt override.</summary>
    public string? StatusPrompt { get; set; }

    /// <summary>Optional Copilot implement prompt override.</summary>
    public string? ImplementPrompt { get; set; }

    /// <summary>Optional Copilot plan prompt override.</summary>
    public string? PlanPrompt { get; set; }

    /// <summary>Workspace banned license list serialized as JSON.</summary>
    public string? BannedLicensesJson { get; set; }

    /// <summary>Workspace banned country list serialized as JSON.</summary>
    public string? BannedCountriesOfOriginJson { get; set; }

    /// <summary>Workspace banned organization list serialized as JSON.</summary>
    public string? BannedOrganizationsJson { get; set; }

    /// <summary>Workspace banned individual list serialized as JSON.</summary>
    public string? BannedIndividualsJson { get; set; }

    /// <summary>Optional absolute path to the agent executable.</summary>
    [MaxLength(2048)]
    public string? AgentPath { get; set; }

    /// <summary>UTC timestamp when this workspace was first registered.</summary>
    public DateTimeOffset DateTimeCreated { get; set; }

    /// <summary>UTC timestamp when this workspace was last modified.</summary>
    public DateTimeOffset DateTimeModified { get; set; }

    /// <summary>True when the row has been soft-deleted.</summary>
    public bool IsDeleted { get; set; }

    /// <summary>UTC timestamp when the row was soft-deleted.</summary>
    public DateTimeOffset? DeletedAtUtc { get; set; }

    /// <summary>Actor or subsystem that soft-deleted the row.</summary>
    [MaxLength(256)]
    public string? DeletedBy { get; set; }

    /// <summary>Optional reason recorded for the soft delete.</summary>
    [MaxLength(1024)]
    public string? DeleteReason { get; set; }
}
