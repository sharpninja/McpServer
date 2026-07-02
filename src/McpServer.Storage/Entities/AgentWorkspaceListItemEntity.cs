using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// 4NF override-list row for a per-workspace agent configuration. One row per value across the
/// model-override and instruction-file-override lists (discriminated by <see cref="ListType"/>),
/// replacing the configuration's <c>ModelsOverrideJson</c> and <c>InstructionFilesOverrideJson</c>
/// columns. Row presence is the override signal: no rows for a list type means "use the
/// definition default" (the former null column).
/// </summary>
public sealed class AgentWorkspaceListItemEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Workspace discriminator (mirrors the owning configuration's workspace).</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Foreign key to the owning per-workspace agent configuration.</summary>
    public int AgentWorkspaceId { get; set; }

    /// <summary>Discriminator identifying which override list this row belongs to (ModelOverride, InstructionFileOverride).</summary>
    [Required]
    [MaxLength(32)]
    public required string ListType { get; set; }

    /// <summary>Ordinal position within the override list.</summary>
    public int Ordinal { get; set; }

    /// <summary>The override value (model id or instruction file path).</summary>
    public required string Value { get; set; }

    /// <summary>Navigation to the owning per-workspace agent configuration.</summary>
    public AgentWorkspaceEntity? AgentWorkspace { get; set; }
}
