using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Storage.Entities;

/// <summary>
/// TR-MCP-QUAD-001: Durable workspace-scoped external brain-slot definition.
/// </summary>
public sealed class BrainSlotDefinitionEntity
{
    /// <summary>Workspace discriminator.</summary>
    [MaxLength(1024)]
    public string WorkspaceId { get; set; } = string.Empty;

    /// <summary>Stable slot identifier.</summary>
    [MaxLength(128)]
    public string SlotId { get; set; } = string.Empty;

    /// <summary>Quad role served by the slot.</summary>
    [Required]
    [MaxLength(64)]
    public string Role { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    [MaxLength(256)]
    public string? DisplayName { get; set; }

    /// <summary>Provider kind, such as OpenAI or OpenAICompatible.</summary>
    [Required]
    [MaxLength(64)]
    public string ProviderKind { get; set; } = string.Empty;

    /// <summary>Provider model identifier.</summary>
    [Required]
    [MaxLength(256)]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>Provider endpoint URI, when configured.</summary>
    [MaxLength(2048)]
    public string? Endpoint { get; set; }

    /// <summary>Credential reference. Raw API keys are never stored.</summary>
    [Required]
    [MaxLength(1024)]
    public string CredentialReference { get; set; } = string.Empty;

    /// <summary>Transaction-security party identifier mapped to this slot.</summary>
    [Required]
    [MaxLength(256)]
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Whether this slot may be invoked.</summary>
    public bool Enabled { get; set; }

    /// <summary>Per-call timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>Maximum output tokens requested from the provider.</summary>
    public int MaxOutputTokens { get; set; }

    /// <summary>Optional system prompt.</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>Relative orchestration weight used by the quad decision loop.</summary>
    public double OrchestrationWeight { get; set; } = 1.0;

    /// <summary>Optimistic concurrency version for orchestration weight updates.</summary>
    public int WeightVersion { get; set; }

    /// <summary>UTC timestamp for the most recent orchestration weight update.</summary>
    public DateTimeOffset? WeightUpdatedAtUtc { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    public DateTimeOffset UpdatedAtUtc { get; set; }
}
