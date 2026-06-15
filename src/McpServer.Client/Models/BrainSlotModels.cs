using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>
/// FR-MCP-129: Public DTO for a durable external brain-slot definition.
/// </summary>
public sealed class BrainSlotDto
{
    /// <summary>Stable slot identifier.</summary>
    [JsonPropertyName("slotId")]
    public string SlotId { get; set; } = string.Empty;

    /// <summary>Quad role served by the slot.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Human-readable slot display name.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>Provider kind, such as OpenAI or OpenAICompatible.</summary>
    [JsonPropertyName("providerKind")]
    public string ProviderKind { get; set; } = string.Empty;

    /// <summary>Provider model identifier.</summary>
    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>Optional provider endpoint.</summary>
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    /// <summary>Credential reference. Raw credentials are never returned.</summary>
    [JsonPropertyName("credentialReference")]
    public string CredentialReference { get; set; } = string.Empty;

    /// <summary>Trusted transaction-security party mapped to this slot.</summary>
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Whether the slot is enabled for invocation.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Per-invocation timeout in seconds.</summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; }

    /// <summary>Maximum output token budget for the provider call.</summary>
    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; set; }

    /// <summary>Optional system prompt for the slot.</summary>
    [JsonPropertyName("systemPrompt")]
    public string? SystemPrompt { get; set; }

    /// <summary>UTC creation timestamp.</summary>
    [JsonPropertyName("createdAtUtc")]
    public DateTimeOffset CreatedAtUtc { get; set; }

    /// <summary>UTC last-update timestamp.</summary>
    [JsonPropertyName("updatedAtUtc")]
    public DateTimeOffset UpdatedAtUtc { get; set; }
}

/// <summary>
/// TR-MCP-QUAD-001: Request for creating or updating a durable brain slot.
/// </summary>
public sealed class UpsertBrainSlotRequest
{
    /// <summary>Quad role served by the slot.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Human-readable slot display name.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>Provider kind, such as OpenAI or OpenAICompatible.</summary>
    [JsonPropertyName("providerKind")]
    public string ProviderKind { get; set; } = string.Empty;

    /// <summary>Provider model identifier.</summary>
    [JsonPropertyName("modelId")]
    public string ModelId { get; set; } = string.Empty;

    /// <summary>Optional provider endpoint.</summary>
    [JsonPropertyName("endpoint")]
    public string? Endpoint { get; set; }

    /// <summary>Credential reference. Raw credentials must not be supplied.</summary>
    [JsonPropertyName("credentialReference")]
    public string CredentialReference { get; set; } = string.Empty;

    /// <summary>Trusted transaction-security party mapped to this slot.</summary>
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Whether the slot should be enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Per-invocation timeout in seconds.</summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; }

    /// <summary>Maximum output token budget for the provider call.</summary>
    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; set; }

    /// <summary>Optional system prompt for the slot.</summary>
    [JsonPropertyName("systemPrompt")]
    public string? SystemPrompt { get; set; }

    /// <summary>Whether enabling this slot may replace another enabled slot for the same role.</summary>
    [JsonPropertyName("replaceExisting")]
    public bool ReplaceExisting { get; set; }
}

/// <summary>
/// FR-MCP-129: Readiness projection for all four brain-slot roles.
/// </summary>
public sealed class BrainSlotStatusResponse
{
    /// <summary>Whether all four roles are enabled and valid.</summary>
    [JsonPropertyName("quadReady")]
    public bool QuadReady { get; set; }

    /// <summary>Per-role readiness flags.</summary>
    [JsonPropertyName("roleReadiness")]
    public IReadOnlyDictionary<string, bool> RoleReadiness { get; set; } = new Dictionary<string, bool>();

    /// <summary>Roles without a visible slot.</summary>
    [JsonPropertyName("missingRoles")]
    public IReadOnlyList<string> MissingRoles { get; set; } = [];

    /// <summary>Roles whose slots are disabled.</summary>
    [JsonPropertyName("disabledRoles")]
    public IReadOnlyList<string> DisabledRoles { get; set; } = [];

    /// <summary>Readiness validation errors.</summary>
    [JsonPropertyName("validationErrors")]
    public IReadOnlyList<string> ValidationErrors { get; set; } = [];
}

/// <summary>
/// FR-MCP-129 and FR-MCP-130: Request to invoke a configured brain slot.
/// </summary>
public sealed class BrainSlotInvokeRequest
{
    /// <summary>User input sent to the external slot model.</summary>
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    /// <summary>Session-log turn identifier that owns this invocation.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>Whether committed Curiosity output should be admitted to GraphRAG/context.</summary>
    [JsonPropertyName("admitToGraphRag")]
    public bool AdmitToGraphRag { get; set; }

    /// <summary>Caller metadata preserved in transaction evidence.</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// FR-MCP-129 and FR-MCP-130: Response from a brain-slot invocation attempt.
/// </summary>
public sealed class BrainSlotInvokeResponse
{
    /// <summary>Invocation status, such as committed or rejected.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured reason code.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Stable slot identifier.</summary>
    [JsonPropertyName("slotId")]
    public string SlotId { get; set; } = string.Empty;

    /// <summary>Quad role served by the slot.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Committed transaction identifier, when available.</summary>
    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    /// <summary>Committed diffgram identifier, when available.</summary>
    [JsonPropertyName("diffgramId")]
    public string? DiffgramId { get; set; }

    /// <summary>Provider model identifier.</summary>
    [JsonPropertyName("modelId")]
    public string? ModelId { get; set; }

    /// <summary>Model output. This is populated only after subscriber commit succeeds.</summary>
    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>UTC invocation start timestamp.</summary>
    [JsonPropertyName("startedAtUtc")]
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>UTC invocation completion timestamp.</summary>
    [JsonPropertyName("completedAtUtc")]
    public DateTimeOffset CompletedAtUtc { get; set; }
}
