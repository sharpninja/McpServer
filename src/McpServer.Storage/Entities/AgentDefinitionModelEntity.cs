using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// 4NF default-model row for an agent definition. One row per model id, replacing the
/// definition's <c>DefaultModelsJson</c> column.
/// </summary>
public sealed class AgentDefinitionModelEntity
{
    /// <summary>Auto-generated primary key.</summary>
    [Key]
    public long Id { get; set; }

    /// <summary>Workspace discriminator (mirrors the owning definition's workspace; empty for global built-ins).</summary>
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Foreign key to the owning agent definition.</summary>
    public string AgentDefinitionId { get; set; } = string.Empty;

    /// <summary>Ordinal position within the definition's default-model list.</summary>
    public int Ordinal { get; set; }

    /// <summary>AI model identifier.</summary>
    public required string Model { get; set; }

    /// <summary>Navigation to the owning agent definition.</summary>
    public AgentDefinitionEntity? AgentDefinition { get; set; }
}
