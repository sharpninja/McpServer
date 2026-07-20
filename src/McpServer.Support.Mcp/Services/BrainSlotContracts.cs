using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-129: Well-known external brain-slot role names.
/// </summary>
public static class BrainSlotRoles
{
    /// <summary>Generative creativity slot.</summary>
    public const string Creativity = "Creativity";

    /// <summary>Deterministic logic slot.</summary>
    public const string Logic = "Logic";

    /// <summary>Research and gap-detection Curiosity slot.</summary>
    public const string CuriosityEngine = "CuriosityEngine";

    /// <summary>Arbiter-of-Truth oversight slot.</summary>
    public const string ArbiterOfTruth = "ArbiterOfTruth";

    /// <summary>All valid brain-slot roles.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        Creativity,
        Logic,
        CuriosityEngine,
        ArbiterOfTruth,
    ];
}

/// <summary>
/// FR-MCP-129: Structured reason codes for brain-slot failures.
/// </summary>
public static class BrainSlotReasonCodes
{
    /// <summary>No failure.</summary>
    public const string None = "None";

    /// <summary>Request validation failed.</summary>
    public const string ValidationFailed = "ValidationFailed";

    /// <summary>Requested slot was not found.</summary>
    public const string SlotNotFound = "SlotNotFound";

    /// <summary>Requested role is not valid.</summary>
    public const string InvalidRole = "InvalidRole";

    /// <summary>Another enabled slot already serves the role.</summary>
    public const string EnabledRoleConflict = "EnabledRoleConflict";

    /// <summary>Brain-slot execution is disabled by configuration.</summary>
    public const string ExecutionDisabled = "ExecutionDisabled";

    /// <summary>Requested slot is disabled.</summary>
    public const string SlotDisabled = "SlotDisabled";

    /// <summary>Endpoint policy rejected the configured endpoint.</summary>
    public const string EndpointNotAllowed = "EndpointNotAllowed";

    /// <summary>Credential reference could not be resolved.</summary>
    public const string CredentialUnavailable = "CredentialUnavailable";

    /// <summary>Trusted party/key mapping is missing or disabled.</summary>
    public const string PartyMappingInvalid = "PartyMappingInvalid";

    /// <summary>Required turn transactions are not enabled.</summary>
    public const string TransactionsRequired = "TransactionsRequired";

    /// <summary>External provider call failed.</summary>
    public const string ProviderFailed = "ProviderFailed";

    /// <summary>Subscriber commit did not complete successfully.</summary>
    public const string CommitFailed = "CommitFailed";

    /// <summary>The workspace is not ready for full quad orchestration.</summary>
    public const string QuadNotReady = "QuadNotReady";

    /// <summary>Quad orchestration failed before a final decision was committed.</summary>
    public const string OrchestrationFailed = "OrchestrationFailed";

    /// <summary>Weight update gates or payload validation rejected the request.</summary>
    public const string WeightUpdateRejected = "WeightUpdateRejected";

    /// <summary>A supplied expected weight version did not match the persisted version.</summary>
    public const string WeightVersionConflict = "WeightVersionConflict";

    /// <summary>Requested operation remains intentionally disabled by containment policy.</summary>
    public const string DeferredFeatureDisabled = "DeferredFeatureDisabled";
}

/// <summary>
/// TR-MCP-QUAD-001 and TR-MCP-QUAD-002: Brain-slot runtime options.
/// </summary>
public sealed class BrainSlotOptions
{
    /// <summary>Configuration section path.</summary>
    public const string SectionName = "Mcp:BrainSlots";

    /// <summary>Whether live brain-slot execution is enabled.</summary>
    public bool ExecutionEnabled { get; set; }

    /// <summary>Allowed custom endpoint hosts.</summary>
    public List<string> AllowedEndpointHosts { get; set; } = [];

    /// <summary>Whether loopback or private-address endpoint hosts are allowed.</summary>
    public bool AllowLoopbackEndpoints { get; set; }

    /// <summary>Default timeout in seconds when a slot does not specify one.</summary>
    public int DefaultTimeoutSeconds { get; set; } = 30;

    /// <summary>Maximum accepted timeout in seconds.</summary>
    public int MaxTimeoutSeconds { get; set; } = 300;

    /// <summary>
    /// FR-MCP-QBSEED-001: Brain-slot definitions provisioned into the durable registry at startup
    /// when <see cref="ExecutionEnabled"/> is true. Empty disables startup provisioning.
    /// </summary>
    public List<BrainSlotSeedDefinition> Slots { get; set; } = [];
}

/// <summary>
/// FR-MCP-QBSEED-001 and TR-MCP-QBSEED-002: A configuration-declared brain-slot definition applied at
/// startup. Carries a stable slot id plus the same fields as <see cref="UpsertBrainSlotRequest"/>.
/// Credentials are referenced only by safe reference (env:, config:, or file:), never inline.
/// </summary>
public sealed class BrainSlotSeedDefinition
{
    /// <summary>Stable slot identifier used as the upsert key.</summary>
    public string SlotId { get; set; } = string.Empty;

    /// <summary>Quad role served by the slot.</summary>
    public string Role { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    public string? DisplayName { get; set; }

    /// <summary>Provider kind (OpenAI or OpenAICompatible).</summary>
    public string ProviderKind { get; set; } = string.Empty;

    /// <summary>Provider model identifier.</summary>
    public string ModelId { get; set; } = string.Empty;

    /// <summary>Optional provider endpoint.</summary>
    public string? Endpoint { get; set; }

    /// <summary>Credential reference (env:, config:, or file:).</summary>
    public string CredentialReference { get; set; } = string.Empty;

    /// <summary>Trusted transaction-security party identifier.</summary>
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Whether this slot should be enabled.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>Per-call timeout in seconds.</summary>
    public int TimeoutSeconds { get; set; }

    /// <summary>Maximum output tokens.</summary>
    public int MaxOutputTokens { get; set; }

    /// <summary>Optional system prompt.</summary>
    public string? SystemPrompt { get; set; }

    /// <summary>Initial orchestration weight. Defaults to 1.0 when omitted or invalid.</summary>
    public double OrchestrationWeight { get; set; } = 1.0;

    /// <summary>Whether enabling may replace another enabled slot for the same role.</summary>
    public bool ReplaceExisting { get; set; } = true;

    /// <summary>Projects this seed definition onto a registry upsert request.</summary>
    /// <returns>The equivalent <see cref="UpsertBrainSlotRequest"/>.</returns>
    public UpsertBrainSlotRequest ToUpsertRequest()
        => new()
        {
            Role = Role,
            DisplayName = DisplayName,
            ProviderKind = ProviderKind,
            ModelId = ModelId,
            Endpoint = Endpoint,
            CredentialReference = CredentialReference,
            PartyId = PartyId,
            Enabled = Enabled,
            TimeoutSeconds = TimeoutSeconds,
            MaxOutputTokens = MaxOutputTokens,
            SystemPrompt = SystemPrompt,
            OrchestrationWeight = OrchestrationWeight,
            ReplaceExisting = ReplaceExisting,
        };
}

/// <summary>
/// FR-MCP-129: Server DTO for a durable brain-slot definition.
/// </summary>
public sealed class BrainSlotDto
{
    /// <summary>Stable slot identifier.</summary>
    [JsonPropertyName("slotId")]
    public string SlotId { get; set; } = string.Empty;

    /// <summary>Quad role served by the slot.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>Provider kind.</summary>
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

    /// <summary>Trusted transaction-security party identifier.</summary>
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Whether this slot may be invoked.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Per-call timeout in seconds.</summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; }

    /// <summary>Maximum output tokens.</summary>
    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; set; }

    /// <summary>Optional system prompt.</summary>
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
/// TR-MCP-QUAD-001: Server request for creating or updating a brain slot.
/// </summary>
public sealed class UpsertBrainSlotRequest
{
    /// <summary>Quad role served by the slot.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Human-readable display name.</summary>
    [JsonPropertyName("displayName")]
    public string? DisplayName { get; set; }

    /// <summary>Provider kind.</summary>
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

    /// <summary>Trusted transaction-security party identifier.</summary>
    [JsonPropertyName("partyId")]
    public string PartyId { get; set; } = string.Empty;

    /// <summary>Whether this slot should be enabled.</summary>
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    /// <summary>Per-call timeout in seconds.</summary>
    [JsonPropertyName("timeoutSeconds")]
    public int TimeoutSeconds { get; set; }

    /// <summary>Maximum output tokens.</summary>
    [JsonPropertyName("maxOutputTokens")]
    public int MaxOutputTokens { get; set; }

    /// <summary>Optional system prompt.</summary>
    [JsonPropertyName("systemPrompt")]
    public string? SystemPrompt { get; set; }

    /// <summary>Initial orchestration weight. Defaults to 1.0 when omitted or invalid.</summary>
    [JsonPropertyName("orchestrationWeight")]
    public double OrchestrationWeight { get; set; } = 1.0;

    /// <summary>Whether enabling may replace another enabled slot for the same role.</summary>
    [JsonPropertyName("replaceExisting")]
    public bool ReplaceExisting { get; set; }
}

/// <summary>
/// FR-MCP-129: Per-role readiness response.
/// </summary>
public sealed class BrainSlotStatusResponse
{
    /// <summary>Whether the workspace has all four enabled and valid roles.</summary>
    [JsonPropertyName("quadReady")]
    public bool QuadReady { get; set; }

    /// <summary>Per-role readiness flags.</summary>
    [JsonPropertyName("roleReadiness")]
    public IReadOnlyDictionary<string, bool> RoleReadiness { get; set; } = new Dictionary<string, bool>();

    /// <summary>Missing roles.</summary>
    [JsonPropertyName("missingRoles")]
    public IReadOnlyList<string> MissingRoles { get; set; } = [];

    /// <summary>Disabled roles.</summary>
    [JsonPropertyName("disabledRoles")]
    public IReadOnlyList<string> DisabledRoles { get; set; } = [];

    /// <summary>Validation errors.</summary>
    [JsonPropertyName("validationErrors")]
    public IReadOnlyList<string> ValidationErrors { get; set; } = [];
}

/// <summary>
/// FR-MCP-129 and FR-MCP-130: Brain-slot invocation request.
/// </summary>
public sealed class BrainSlotInvokeRequest
{
    /// <summary>User input for the model.</summary>
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    /// <summary>Owning session-log turn identifier.</summary>
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
/// FR-MCP-129 and FR-MCP-130: Brain-slot invocation response.
/// </summary>
public sealed class BrainSlotInvokeResponse
{
    /// <summary>Invocation status.</summary>
    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    /// <summary>Structured reason code.</summary>
    [JsonPropertyName("reason")]
    public string Reason { get; set; } = string.Empty;

    /// <summary>Slot identifier.</summary>
    [JsonPropertyName("slotId")]
    public string SlotId { get; set; } = string.Empty;

    /// <summary>Quad role.</summary>
    [JsonPropertyName("role")]
    public string Role { get; set; } = string.Empty;

    /// <summary>Transaction identifier.</summary>
    [JsonPropertyName("transactionId")]
    public string? TransactionId { get; set; }

    /// <summary>Diffgram identifier.</summary>
    [JsonPropertyName("diffgramId")]
    public string? DiffgramId { get; set; }

    /// <summary>Provider model identifier.</summary>
    [JsonPropertyName("modelId")]
    public string? ModelId { get; set; }

    /// <summary>Model output, populated only after subscriber commit succeeds.</summary>
    [JsonPropertyName("output")]
    public string? Output { get; set; }

    /// <summary>UTC start timestamp.</summary>
    [JsonPropertyName("startedAtUtc")]
    public DateTimeOffset StartedAtUtc { get; set; }

    /// <summary>UTC completion timestamp.</summary>
    [JsonPropertyName("completedAtUtc")]
    public DateTimeOffset CompletedAtUtc { get; set; }
}

/// <summary>
/// FR-MCP-134 and TR-MCP-QUAD-005: Request to run the full Quad-Brain decision loop.
/// </summary>
public sealed class QuadBrainOrchestrationRequest
{
    /// <summary>User input to evaluate.</summary>
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    /// <summary>Owning session-log turn identifier.</summary>
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
/// FR-MCP-134 and TR-MCP-QUAD-006: Request to run Arbiter-of-Truth reconciliation over role evidence.
/// </summary>
public sealed class AotReconciliationRequest
{
    /// <summary>Original user input.</summary>
    [JsonPropertyName("input")]
    public string Input { get; set; } = string.Empty;

    /// <summary>Creativity committed output.</summary>
    [JsonPropertyName("creativityOutput")]
    public string CreativityOutput { get; set; } = string.Empty;

    /// <summary>Logic committed output.</summary>
    [JsonPropertyName("logicOutput")]
    public string LogicOutput { get; set; } = string.Empty;

    /// <summary>Optional CuriosityEngine research/context output for escalated reconciliation.</summary>
    [JsonPropertyName("curiosityOutput")]
    public string? CuriosityOutput { get; set; }

    /// <summary>Owning session-log turn identifier.</summary>
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

    /// <summary>Owning session-log turn identifier.</summary>
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
