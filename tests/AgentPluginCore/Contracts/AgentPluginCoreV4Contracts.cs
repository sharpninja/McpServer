// Agent Plugin Core v4 Contracts (Phase 1 contract tests per PARITY-RESUME-004)
// These interfaces define the single source of truth for shared enforcement + bootstrap core
// per TR-MCP-AGENT-PARITY-010, -011, -012, -013 and FR-MCP-AGENT-PARITY-001/002.
// 
// All behavior must be identical across TS core, shell shims, and any .NET ports.
// Tests use these + stubs/mocks to validate BEFORE any real core implementation (Byrd v4 TDD).
// 
// v4 aspects emphasized: marker signature/nonce (HMAC + health challenge), 
// enforcement state machine with strict build verification gates,
// cache/failsafe with scoped recovery semantics, REPL JSON envelopes + circuit breaker.

namespace McpServer.AgentPluginCore.Tests.Contracts;

/// <summary>
/// v4 marker trust bootstrap contract (TR-MCP-AGENT-PARITY-012).
/// Upward discovery of AGENTS-README-FIRST.yaml, HMAC-SHA256 signature verification (v4 includes nonce binding),
/// health nonce challenge, MCP_UNTRUSTED on any failure, env var setup on success.
/// </summary>
public interface IV4MarkerTrustService
{
    /// <summary>
    /// Finds AGENTS-README-FIRST.yaml (or marker) by walking upward from startPath.
    /// </summary>
    Task<string?> FindMarkerFileAsync(string startPath, CancellationToken ct = default);

    /// <summary>
    /// Verifies the marker signature using HMAC-SHA256 over (apiKey + canonicalPath + nonce) or v4 equivalent.
    /// Returns parsed marker data on success.
    /// </summary>
    Task<IV4MarkerData> VerifySignatureAndParseAsync(string markerPath, CancellationToken ct = default);

    /// <summary>
    /// Performs nonce health challenge against server health endpoint derived from marker.
    /// Throws or returns false on failure (triggers MCP_UNTRUSTED).
    /// </summary>
    Task<bool> PerformNonceHealthChallengeAsync(IV4MarkerData marker, CancellationToken ct = default);

    /// <summary>
    /// Full bootstrap: find + verify + nonce + set trust env vars. On failure sets MCP_UNTRUSTED.
    /// </summary>
    Task<IV4TrustResult> BootstrapTrustAsync(string workspacePath, CancellationToken ct = default);
}

/// <summary>
/// Parsed marker data (v4 fields).
/// </summary>
public interface IV4MarkerData
{
    string WorkspacePath { get; }
    string ServerUrl { get; }
    string ApiKey { get; }
    string? Signature { get; }
    string? Nonce { get; }
    IReadOnlyDictionary<string, string> Metadata { get; }
}

/// <summary>
/// Trust result (v4).
/// </summary>
public interface IV4TrustResult
{
    bool IsTrusted { get; }
    string TrustMethod { get; } // "signature_verified", "nonce_challenge", "registry_cached", "MCP_UNTRUSTED"
    string? DenialReason { get; }
    IV4MarkerData? MarkerData { get; }
}

/// <summary>
/// v4 enforcement protocol state machine (TR-MCP-AGENT-PARITY-011).
/// Canonical 3-phase per-turn with explicit states and build verification gates.
/// No escape hatches for failed builds. Self-heal for missing completeTurn.
/// </summary>
public interface IV4EnforcementStateMachine
{
    V4EnforcementState CurrentState { get; }
    string? CurrentTurnId { get; }
    DateTimeOffset? TurnOpenedAt { get; }

    /// <summary>
    /// Phase 1: beginTurn on first user message. Transitions NoTurn -> TurnOpen.
    /// </summary>
    Task<V4EnforcementTransitionResult> BeginTurnAsync(string requestId, CancellationToken ct = default);

    /// <summary>
    /// After source edit: append action + invoke code-verify (build gate).
    /// On failed build -> BlockedOnBuild. EditsInProgress otherwise.
    /// </summary>
    Task<V4EnforcementTransitionResult> RecordCodeEditAndVerifyBuildAsync(string filePath, string buildStatus, CancellationToken ct = default);

    /// <summary>
    /// Phase 3 / stop-gate: completeTurn. Blocks if in_progress or failed build.
    /// Self-heal path available.
    /// </summary>
    Task<V4EnforcementTransitionResult> CompleteTurnAsync(string requestId, bool forceSelfHeal = false, CancellationToken ct = default);

    /// <summary>
    /// Stop gate check (called before emitting final response).
    /// Returns blocked reason if cannot complete (build fail or missing complete).
    /// </summary>
    V4StopGateDecision EvaluateStopGate();
}

/// <summary>
/// States per v4 state machine.
/// </summary>
public enum V4EnforcementState
{
    NoTurn,
    TurnOpen,
    EditsInProgress,
    TurnComplete,
    BlockedOnBuild,
    BlockedOnMissingComplete
}

/// <summary>
/// Result of a state transition (includes any error or gate info).
/// </summary>
public class V4EnforcementTransitionResult
{
    public bool Success { get; set; }
    public V4EnforcementState NewState { get; set; }
    public string? ErrorCode { get; set; } // e.g. "BUILD_FAILED", "MISSING_COMPLETE", "MCP_UNTRUSTED"
    public string? Message { get; set; }
}

/// <summary>
/// Stop gate output (standardized per plan).
/// </summary>
public class V4StopGateDecision
{
    public bool CanEmitFinalResponse { get; set; }
    public string? BlockReason { get; set; }
    public V4EnforcementState State { get; set; }
}

/// <summary>
/// v4 failsafe cache + recovery (TR-MCP-AGENT-PARITY-013).
/// Scoped by workspaceKey (base64url of abs path) + agentId.
/// YAML pending queue, 3 retries, failed/ dir, idempotent recovery that replays to REPL producing golden artifacts.
/// </summary>
public interface IV4CacheManager
{
    string GetScopedCachePath(string workspaceKey, string agentId);

    /// <summary>
    /// Write pending entry (yaml serialized). Survives outages.
    /// </summary>
    Task WritePendingAsync(string workspaceKey, string agentId, string entryId, object payload, CancellationToken ct = default);

    /// <summary>
    /// Flush pending (opportunistic / session-end / manual). Retries <=3 then moves to failed/.
    /// </summary>
    Task<V4CacheFlushResult> FlushPendingAsync(string workspaceKey, string agentId, int maxRetries = 3, CancellationToken ct = default);

    /// <summary>
    /// Recovery: read pending/failed, replay via replBridge, produce identical artifacts to direct path.
    /// Idempotent.
    /// </summary>
    Task<V4CacheRecoveryResult> RecoverAndReplayAsync(string workspaceKey, string agentId, IV4ReplBridge replBridge, CancellationToken ct = default);
}

public class V4CacheFlushResult
{
    public bool Success { get; set; }
    public int RetriesUsed { get; set; }
    public int MovedToFailed { get; set; }
    public string? Error { get; set; }
}

public class V4CacheRecoveryResult
{
    public bool Success { get; set; }
    public int EntriesReplayed { get; set; }
    public string[] ProducedArtifacts { get; set; } = Array.Empty<string>(); // e.g. session-log ids, todo updates
    public string? Error { get; set; }
}

/// <summary>
/// v4 REPL bridge (single-line JSON envelopes, streaming, timeouts, retries, circuit breaker).
/// (TR-MCP-AGENT-PARITY-010)
/// </summary>
public interface IV4ReplBridge
{
    /// <summary>
    /// Send single-line JSON envelope (no newlines in payload).
    /// </summary>
    Task<V4ReplResponse> SendEnvelopeAsync(V4ReplEnvelope envelope, TimeSpan? timeout = null, CancellationToken ct = default);

    /// <summary>
    /// Streaming variant for long workflows (events fired).
    /// </summary>
    IAsyncEnumerable<V4ReplEvent> SendEnvelopeStreamingAsync(V4ReplEnvelope envelope, CancellationToken ct = default);

    /// <summary>
    /// Circuit state for breaker (OPEN after repeated failures).
    /// </summary>
    V4CircuitState CircuitState { get; }
}

public class V4ReplEnvelope
{
    public string Type { get; set; } = ""; // "workflow.sessionlog.beginTurn", "workflow.todo.create" etc.
    public string RequestId { get; set; } = Guid.NewGuid().ToString("N");
    public object? Payload { get; set; }
    public string AgentId { get; set; } = "unknown";
}

public class V4ReplResponse
{
    public bool Success { get; set; }
    public object? Result { get; set; }
    public string? ErrorCode { get; set; }
    public string? ErrorMessage { get; set; }
    public IReadOnlyList<V4ReplEvent>? Events { get; set; }
}

public class V4ReplEvent
{
    public string EventType { get; set; } = "";
    public object? Data { get; set; }
    public DateTimeOffset Timestamp { get; set; }
}

public enum V4CircuitState
{
    Closed,
    Open,
    HalfOpen
}
