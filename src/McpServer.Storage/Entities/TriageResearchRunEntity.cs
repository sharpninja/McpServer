using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-TRIAGE-001 and TR-MCP-TRIAGE-003: Durable audit row for asynchronous triage research attempts.
/// </summary>
public sealed class TriageResearchRunEntity
{
    /// <summary>Workspace discriminator used by MCP multi-tenant filters.</summary>
    [Required]
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Durable research run id.</summary>
    [Key]
    [MaxLength(128)]
    public required string RunId { get; set; }

    /// <summary>Triage group id researched by this run.</summary>
    [Required]
    [MaxLength(128)]
    public required string GroupId { get; set; }

    /// <summary>Run status.</summary>
    [Required]
    [MaxLength(64)]
    public required string Status { get; set; }

    /// <summary>Prompt template id used for this run.</summary>
    [MaxLength(256)]
    public string? PromptTemplateId { get; set; }

    /// <summary>Rendered prompt sent to the triage agent.</summary>
    public string? Prompt { get; set; }

    /// <summary>Serialized group JSON supplied to the triage agent.</summary>
    public string? GroupJson { get; set; }

    /// <summary>Raw agent output.</summary>
    public string? RawOutput { get; set; }

    /// <summary>Raw stdout stream captured from the launched triage agent process.</summary>
    public string? AgentStdout { get; set; }

    /// <summary>Raw stderr stream captured from the launched triage agent process.</summary>
    public string? AgentStderr { get; set; }

    /// <summary>Exit code returned by the launched triage agent process.</summary>
    public int? AgentExitCode { get; set; }

    /// <summary>Schema-valid agent JSON after validation.</summary>
    public string? ResponseJson { get; set; }

    /// <summary>Failure text if the run failed.</summary>
    public string? Error { get; set; }

    /// <summary>UTC timestamp when the run started.</summary>
    public DateTimeOffset StartedUtc { get; set; }

    /// <summary>UTC timestamp when the run completed.</summary>
    public DateTimeOffset? CompletedUtc { get; set; }

    /// <summary>Created TODO id, if any.</summary>
    [MaxLength(128)]
    public string? CreatedTodoId { get; set; }
}
