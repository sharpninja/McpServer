using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-PLANNED-013: 4NF processing dialog entity. One row per model reasoning/processing note
/// appended during request execution. The AI model can independently append entries
/// to capture its internal reasoning, tool-use decisions, and execution trace.
/// </summary>
public sealed class SessionLogProcessingDialogEntity
{
    /// <summary>TR-PLANNED-013: Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>TR-MCP-MT-003: Workspace discriminator for multi-tenant data isolation.</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>TR-PLANNED-013: Foreign key to parent entry.</summary>
    public long SessionLogTurnId { get; set; }

    /// <summary>TR-PLANNED-013: Ordinal position within the dialog sequence.</summary>
    public int Ordinal { get; set; }

    /// <summary>TR-PLANNED-013: ISO 8601 timestamp when this dialog item was recorded.</summary>
    public DateTimeOffset Timestamp { get; set; }

    /// <summary>TR-PLANNED-013: Role of the speaker (e.g. model, tool, system, user).</summary>
    [Required]
    [MaxLength(64)]
    public required string Role { get; set; }

    /// <summary>TR-PLANNED-013: Content of the processing dialog entry.</summary>
    [Required]
    public required string Content { get; set; }

    /// <summary>TR-PLANNED-013: Optional category (e.g. reasoning, tool_call, tool_result, observation, decision).</summary>
    [MaxLength(128)]
    public string? Category { get; set; }

    /// <summary>TR-PLANNED-013: Navigation to parent entry.</summary>
    public SessionLogTurnEntity? SessionLogTurn { get; set; }
}

