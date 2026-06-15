using System.Text.Json.Serialization;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-129: Well-known external brain-slot role names.
/// </summary>
public static class BrainSlotRoles
{
    /// <summary>Analytical left-hemisphere slot.</summary>
    public const string LeftHemisphere = "LeftHemisphere";

    /// <summary>Associative right-hemisphere slot.</summary>
    public const string RightHemisphere = "RightHemisphere";

    /// <summary>Research and gap-detection Curiosity slot.</summary>
    public const string CuriosityEngine = "CuriosityEngine";

    /// <summary>Arbiter-of-Truth oversight slot.</summary>
    public const string ArbiterOfTruth = "ArbiterOfTruth";

    /// <summary>All valid brain-slot roles.</summary>
    public static readonly IReadOnlyList<string> All =
    [
        LeftHemisphere,
        RightHemisphere,
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

    /// <summary>Requested operation is intentionally disabled for this slice.</summary>
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
/// TR-MCP-QUAD-004: Deferred branch execution request.
/// </summary>
public sealed class BrainSlotDeferredRequest
{
    /// <summary>Optional turn identifier.</summary>
    public string? TurnId { get; set; }
}
