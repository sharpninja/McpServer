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

    /// <summary>Relative orchestration weight used by the quad decision loop.</summary>
    [JsonPropertyName("orchestrationWeight")]
    public double OrchestrationWeight { get; set; } = 1.0;

    /// <summary>Optimistic concurrency version for orchestration weight updates.</summary>
    [JsonPropertyName("weightVersion")]
    public int WeightVersion { get; set; }

    /// <summary>UTC timestamp for the most recent orchestration weight update.</summary>
    [JsonPropertyName("weightUpdatedAtUtc")]
    public DateTimeOffset? WeightUpdatedAtUtc { get; set; }

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

    /// <summary>Initial orchestration weight. Defaults to 1.0 when omitted or invalid.</summary>
    [JsonPropertyName("orchestrationWeight")]
    public double OrchestrationWeight { get; set; } = 1.0;

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

    /// <summary>Optional provider temperature override for this invocation.</summary>
    [JsonPropertyName("temperature")]
    public double? Temperature { get; set; }

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

/// <summary>
/// FR-MCP-134: Request to run the full Quad-Brain decision loop.
/// </summary>
public sealed class QuadBrainOrchestrationRequest
{
    /// <summary>User input to evaluate.</summary>
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    /// <summary>Session-log turn identifier that owns this orchestration.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>Whether committed Curiosity output should be admitted to GraphRAG/context.</summary>
    [JsonPropertyName("admitCuriosityToGraphRag")]
    public bool AdmitCuriosityToGraphRag { get; set; }

    /// <summary>Caller metadata preserved in transaction evidence.</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

    /// <summary>Optional explicit, approved weight update to apply after the final decision commits.</summary>
    [JsonPropertyName("weightUpdate")]
    public QuadBrainWeightUpdateRequest? WeightUpdate { get; set; }
}

/// <summary>
/// FR-MCP-134: Request to run Arbiter-of-Truth reconciliation.
/// </summary>
public sealed class AotReconciliationRequest
{
    /// <summary>Original user input.</summary>
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    /// <summary>LeftHemisphere committed output.</summary>
    [JsonPropertyName("leftOutput")]
    public string LeftOutput { get; set; } = string.Empty;

    /// <summary>RightHemisphere committed output.</summary>
    [JsonPropertyName("rightOutput")]
    public string RightOutput { get; set; } = string.Empty;

    /// <summary>CuriosityEngine committed output.</summary>
    [JsonPropertyName("curiosityOutput")]
    public string CuriosityOutput { get; set; } = string.Empty;

    /// <summary>Session-log turn identifier that owns this reconciliation.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>Caller metadata preserved in transaction evidence.</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// FR-MCP-134: Per-role output captured during quad orchestration.
/// </summary>
public sealed class QuadBrainRoleResult
{
    /// <summary>Quad role.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Slot identifier.</summary>
    [JsonPropertyName("slotId")]
    public string SlotId { get; set; } = string.Empty;

    /// <summary>Invocation status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured reason code.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Provider model identifier.</summary>
    [JsonPropertyName("modelId")]
    public string? ModelId { get; set; }

    /// <summary>Committed transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    /// <summary>Committed diffgram identifier.</summary>
    [JsonPropertyName("diffgramId")]
    public string? DiffgramId { get; set; }

    /// <summary>Committed model output.</summary>
    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>Weight applied to this role for the decision loop.</summary>
    [JsonPropertyName("orchestrationWeight")]
    public double OrchestrationWeight { get; set; }

    /// <summary>Persisted weight version used by the decision loop.</summary>
    [JsonPropertyName("weightVersion")]
    public int WeightVersion { get; set; }
}

/// <summary>
/// FR-MCP-134: Response from direct AoT reconciliation execution.
/// </summary>
public sealed class AotReconciliationResponse
{
    /// <summary>Execution status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured reason code.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Committed transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    /// <summary>Committed diffgram identifier.</summary>
    [JsonPropertyName("diffgramId")]
    public string? DiffgramId { get; set; }

    /// <summary>Arbiter slot identifier.</summary>
    [JsonPropertyName("slotId")]
    public string SlotId { get; set; } = string.Empty;

    /// <summary>Provider model identifier.</summary>
    [JsonPropertyName("modelId")]
    public string? ModelId { get; set; }

    /// <summary>Final committed Arbiter output.</summary>
    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>UTC execution start timestamp.</summary>
    [JsonPropertyName("startedAtUtc")]
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>UTC execution completion timestamp.</summary>
    [JsonPropertyName("completedAtUtc")]
    public DateTimeOffset CompletedAtUtc { get; set; }
}

/// <summary>
/// FR-MCP-134: Response from full Quad-Brain orchestration.
/// </summary>
public sealed class QuadBrainOrchestrationResponse
{
    /// <summary>Execution status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured reason code.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Final committed decision output.</summary>
    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>Final Arbiter transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    /// <summary>Final Arbiter diffgram identifier.</summary>
    [JsonPropertyName("diffgramId")]
    public string? DiffgramId { get; set; }

    /// <summary>Role outputs collected during orchestration.</summary>
    [JsonPropertyName("roleResults")]
    public IReadOnlyList<QuadBrainRoleResult> RoleResults { get; set; } = [];

    /// <summary>Optional explicit weight update result.</summary>
    [JsonPropertyName("weightUpdate")]
    public QuadBrainWeightUpdateResponse? WeightUpdate { get; set; }

    /// <summary>UTC execution start timestamp.</summary>
    [JsonPropertyName("startedAtUtc")]
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>UTC execution completion timestamp.</summary>
    [JsonPropertyName("completedAtUtc")]
    public DateTimeOffset CompletedAtUtc { get; set; }
}

/// <summary>
/// FR-MCP-135: Request to update durable Quad-Brain role weights.
/// </summary>
public sealed class QuadBrainWeightUpdateRequest
{
    /// <summary>Role-to-weight updates.</summary>
    [JsonPropertyName("roleWeights")]
    public IReadOnlyDictionary<string, double> RoleWeights { get; set; } = new Dictionary<string, double>();

    /// <summary>Optional expected role weight versions for optimistic concurrency.</summary>
    [JsonPropertyName("expectedVersions")]
    public IReadOnlyDictionary<string, int> ExpectedVersions { get; set; } = new Dictionary<string, int>();

    /// <summary>Session-log turn identifier that owns this update.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; set; }

    /// <summary>Who proposed the update.</summary>
    [JsonPropertyName("proposedBy")]
    public string? ProposedBy { get; set; }

    /// <summary>Required human-readable reason for the update.</summary>
    [JsonPropertyName("reasonText")]
    public string ReasonText { get; set; } = string.Empty;

    /// <summary>Whether Arbiter-of-Truth approval has been recorded.</summary>
    [JsonPropertyName("aotApproved")]
    public bool AotApproved { get; set; }

    /// <summary>Whether admin approval has been recorded.</summary>
    [JsonPropertyName("adminApproved")]
    public bool AdminApproved { get; set; }

    /// <summary>Whether safety gates have passed.</summary>
    [JsonPropertyName("safetyGatesPassed")]
    public bool SafetyGatesPassed { get; set; }

    /// <summary>Caller metadata preserved in transaction evidence.</summary>
    [JsonPropertyName("metadata")]
    public IReadOnlyDictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();
}

/// <summary>
/// FR-MCP-135: Before/after snapshot for a role weight update.
/// </summary>
public sealed class QuadBrainWeightSnapshot
{
    /// <summary>Quad role.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Slot identifier.</summary>
    [JsonPropertyName("slotId")]
    public string SlotId { get; set; } = string.Empty;

    /// <summary>Previous role weight.</summary>
    [JsonPropertyName("previousWeight")]
    public double PreviousWeight { get; set; }

    /// <summary>New role weight.</summary>
    [JsonPropertyName("newWeight")]
    public double NewWeight { get; set; }

    /// <summary>Previous weight version.</summary>
    [JsonPropertyName("previousVersion")]
    public int PreviousVersion { get; set; }

    /// <summary>New weight version.</summary>
    [JsonPropertyName("newVersion")]
    public int NewVersion { get; set; }
}

/// <summary>
/// FR-MCP-135: Response from a durable weight update attempt.
/// </summary>
public sealed class QuadBrainWeightUpdateResponse
{
    /// <summary>Execution status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured reason code.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Committed transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    /// <summary>Committed diffgram identifier.</summary>
    [JsonPropertyName("diffgramId")]
    public string? DiffgramId { get; set; }

    /// <summary>Before/after weight snapshots.</summary>
    [JsonPropertyName("snapshots")]
    public IReadOnlyList<QuadBrainWeightSnapshot> Snapshots { get; set; } = [];

    /// <summary>UTC execution start timestamp.</summary>
    [JsonPropertyName("startedAtUtc")]
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>UTC execution completion timestamp.</summary>
    [JsonPropertyName("completedAtUtc")]
    public DateTimeOffset CompletedAtUtc { get; set; }
}
