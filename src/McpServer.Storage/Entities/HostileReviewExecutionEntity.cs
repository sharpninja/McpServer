using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>TR-MCP-HOSTILEREVIEW-003: Reviewer execution metadata. Token counts are omitted when not supplied.</summary>
public sealed class HostileReviewExecutionEntity
{
    /// <summary>Execution identifier.</summary>
    [Key]
    [StringLength(128)]
    public required string ExecutionId { get; set; }

    /// <summary>Parent request id.</summary>
    [Required]
    [StringLength(128)]
    public required string RequestId { get; set; }

    /// <summary>Workspace discriminator.</summary>
    [Required]
    [StringLength(1024)]
    public required string WorkspaceId { get; set; }

    /// <summary>Reviewer agent identity.</summary>
    [Required]
    [StringLength(128)]
    public required string ReviewerAgent { get; set; }

    /// <summary>Model string actually used.</summary>
    [Required]
    [StringLength(128)]
    public required string Model { get; set; }

    /// <summary>Effort level actually used.</summary>
    [Required]
    [StringLength(32)]
    public required string Effort { get; set; }

    /// <summary>Plugin or source surface.</summary>
    [StringLength(64)]
    public string? SourceSurface { get; set; }

    /// <summary>Prompt template id.</summary>
    [StringLength(128)]
    public string? PromptTemplateId { get; set; }

    /// <summary>Prompt version.</summary>
    [StringLength(128)]
    public string? PromptVersion { get; set; }

    /// <summary>UTC start.</summary>
    public DateTimeOffset StartedUtc { get; set; }

    /// <summary>UTC end.</summary>
    public DateTimeOffset? EndedUtc { get; set; }

    /// <summary>Input tokens when the reviewer surface supplied them; otherwise null (never fabricated).</summary>
    public int? InputTokens { get; set; }

    /// <summary>Output tokens when supplied; otherwise null.</summary>
    public int? OutputTokens { get; set; }

    /// <summary>Parent request.</summary>
    [ForeignKey(nameof(RequestId))]
    public HostileReviewRequestEntity? Request { get; set; }
}
