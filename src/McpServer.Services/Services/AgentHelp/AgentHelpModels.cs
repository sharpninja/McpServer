namespace McpServer.Support.Mcp.Services.AgentHelp;

/// <summary>
/// FR-MCP-HELP-001: Request to create an Agent Help session.
/// TR-MCP-HELP-002: Session create contract for the help conversation API.
/// </summary>
public sealed record AgentHelpSessionCreateRequest
{
    /// <summary>
    /// Optional client/device identifier for diagnostics and session affinity.
    /// </summary>
    public string? DeviceId { get; init; }

    /// <summary>
    /// Optional client display name.
    /// </summary>
    public string? ClientName { get; init; }

    /// <summary>
    /// Workspace root path used as CWD when launching helper agents.
    /// </summary>
    public string? WorkspacePath { get; init; }

    /// <summary>
    /// Optional pooled-agent name used for routing and session reuse.
    /// </summary>
    public string? AgentName { get; init; }

    /// <summary>
    /// Optional agent binary path override for this session.
    /// </summary>
    public string? AgentPath { get; init; }

    /// <summary>
    /// Optional model override for this session.
    /// </summary>
    public string? AgentModel { get; init; }

    /// <summary>
    /// Optional seed prompt prepended to the first turn for this session.
    /// </summary>
    public string? AgentSeed { get; init; }

    /// <summary>
    /// Optional key-value parameters forwarded as environment variables for this session.
    /// </summary>
    public Dictionary<string, string>? AgentParameters { get; init; }

    /// <summary>
    /// Optional execution strategy name used to create the helper session.
    /// </summary>
    public string? ExecutionStrategy { get; init; }

    /// <summary>
    /// Optional active TODO id providing bounded execution context.
    /// </summary>
    public string? TodoId { get; init; }

    /// <summary>
    /// Optional topic label used for corpus bootstrap and outcome triage.
    /// </summary>
    public string? Topic { get; init; }

    /// <summary>
    /// Optional caller agent identity for session linkage and incident correlation.
    /// </summary>
    public string? CallerAgent { get; init; }

    /// <summary>
    /// Optional caller session id for linkage.
    /// </summary>
    public string? CallerSessionId { get; init; }

    /// <summary>
    /// Optional caller request/turn id for linkage.
    /// </summary>
    public string? CallerRequestId { get; init; }

    /// <summary>
    /// Optional factual issue summary (observation vs inference separated).
    /// </summary>
    public string? IssueSummary { get; init; }
}

/// <summary>
/// FR-MCP-HELP-001: Response returned when an Agent Help session is created.
/// TR-MCP-HELP-002: Session create response contract.
/// </summary>
public sealed record AgentHelpSessionCreateResponse
{
    /// <summary>
    /// Help session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Current status (e.g. <c>idle</c>).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Model identifier requested for the helper agent.
    /// </summary>
    public string? ModelRequested { get; init; }

    /// <summary>
    /// Model identifier actually used for the session.
    /// </summary>
    public string? ModelResolved { get; init; }

    /// <summary>
    /// Execution strategy backing the created session.
    /// </summary>
    public required string ExecutionStrategy { get; init; }

    /// <summary>
    /// Summary of the bootstrapped context pack, when corpus bootstrap is enabled.
    /// </summary>
    public AgentHelpCorpusSummary? CorpusSummary { get; init; }
}

/// <summary>
/// FR-MCP-HELP-001: Request body for a single Agent Help turn.
/// TR-MCP-HELP-002: Turn request contract.
/// </summary>
public sealed record AgentHelpTurnRequest
{
    /// <summary>
    /// User message text for this turn.
    /// </summary>
    public required string UserMessage { get; init; }

    /// <summary>
    /// Optional client timestamp in ISO 8601 format.
    /// </summary>
    public string? ClientTimestampUtc { get; init; }
}

/// <summary>
/// FR-MCP-HELP-001: Response body for a completed Agent Help turn.
/// TR-MCP-HELP-002: Turn response contract.
/// </summary>
public sealed record AgentHelpTurnResponse
{
    /// <summary>
    /// Help session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Turn identifier within the session.
    /// </summary>
    public required string TurnId { get; init; }

    /// <summary>
    /// Final turn status (e.g. <c>completed</c>, <c>blocked</c>, <c>error</c>).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Text intended for display in the UI transcript.
    /// </summary>
    public string? AssistantDisplayText { get; init; }

    /// <summary>
    /// User-visible error message when the turn fails or is blocked.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Turn latency in milliseconds.
    /// </summary>
    public int LatencyMs { get; init; }

    /// <summary>
    /// Guard evaluation result when inbound guardrails are enabled.
    /// </summary>
    public AgentHelpGuardResult? GuardResult { get; init; }

    /// <summary>
    /// Guard incident identifier when a turn terminates the session.
    /// </summary>
    public string? IncidentId { get; init; }
}

/// <summary>
/// FR-MCP-HELP-001: Streaming event emitted during an Agent Help turn.
/// TR-MCP-HELP-002: SSE stream event contract.
/// </summary>
public sealed record AgentHelpStreamEvent
{
    /// <summary>
    /// Event type (e.g. <c>chunk</c>, <c>done</c>, <c>error</c>, <c>blocked</c>).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// Turn identifier associated with the event.
    /// </summary>
    public string? TurnId { get; init; }

    /// <summary>
    /// Text chunk for <c>chunk</c> events.
    /// </summary>
    public string? Text { get; init; }

    /// <summary>
    /// Terminal status for <c>done</c> events.
    /// </summary>
    public string? Status { get; init; }

    /// <summary>
    /// Error or block message for <c>error</c> and <c>blocked</c> events.
    /// </summary>
    public string? Message { get; init; }

    /// <summary>
    /// Turn latency in milliseconds for terminal events.
    /// </summary>
    public int? LatencyMs { get; init; }

    /// <summary>
    /// Guard evaluation result for blocked turns.
    /// </summary>
    public AgentHelpGuardResult? GuardResult { get; init; }

    /// <summary>
    /// Guard incident identifier for <c>session_terminated</c> events.
    /// </summary>
    public string? IncidentId { get; init; }
}

/// <summary>
/// FR-MCP-HELP-002: Deterministic inbound guard evaluation result.
/// TR-MCP-HELP-004: Guard decision contract returned before helper execution.
/// </summary>
public sealed record AgentHelpGuardResult
{
    /// <summary>
    /// Whether the inbound message is allowed to proceed.
    /// </summary>
    public bool Allowed { get; init; }

    /// <summary>
    /// Stable rule identifier when the message is blocked (e.g. <c>injection.ignore-instructions</c>).
    /// </summary>
    public string? RuleId { get; init; }

    /// <summary>
    /// Human-readable reason for the decision.
    /// </summary>
    public string? Reason { get; init; }

    /// <summary>
    /// Short normalized snippet of the matched content for incident logging.
    /// </summary>
    public string? MatchedSnippet { get; init; }
}

/// <summary>
/// FR-MCP-HELP-003: Append-only transcript entry persisted as JSONL.
/// TR-MCP-HELP-003: Transcript line contract.
/// </summary>
public sealed record AgentHelpTranscriptEntry
{
    /// <summary>
    /// Timestamp in ISO 8601 UTC format.
    /// </summary>
    public required string TimestampUtc { get; init; }

    /// <summary>
    /// Help session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Turn identifier associated with the entry.
    /// </summary>
    public string? TurnId { get; init; }

    /// <summary>
    /// Entry role (e.g. user, assistant, system, guard).
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// Entry category (e.g. transcript, guard_block, incident, corpus).
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Entry text content.
    /// </summary>
    public required string Text { get; init; }

    /// <summary>
    /// Optional guard rule identifier when the entry records a block.
    /// </summary>
    public string? GuardRuleId { get; init; }
}

/// <summary>
/// FR-MCP-HELP-004: Guard incident persisted as JSON.
/// TR-MCP-HELP-005: Incident record contract.
/// </summary>
public sealed record AgentHelpIncidentRecord
{
    /// <summary>
    /// Unique incident identifier.
    /// </summary>
    public required string IncidentId { get; init; }

    /// <summary>
    /// Help session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Turn identifier when the incident occurred during a turn.
    /// </summary>
    public string? TurnId { get; init; }

    /// <summary>
    /// Stable guard rule identifier.
    /// </summary>
    public required string RuleId { get; init; }

    /// <summary>
    /// Human-readable incident reason.
    /// </summary>
    public required string Reason { get; init; }

    /// <summary>
    /// Short normalized snippet of the blocked content.
    /// </summary>
    public string? MatchedSnippet { get; init; }

    /// <summary>
    /// Incident timestamp in ISO 8601 UTC format.
    /// </summary>
    public required string TimestampUtc { get; init; }

    /// <summary>
    /// Workspace path associated with the session, when available.
    /// </summary>
    public string? WorkspacePath { get; init; }
}

/// <summary>
/// FR-MCP-HELP-005: Corpus bootstrap summary returned to helper sessions.
/// TR-MCP-HELP-006: Context pack summary contract.
/// </summary>
public sealed record AgentHelpCorpusSummary
{
    /// <summary>
    /// Workspace path used for corpus bootstrap.
    /// </summary>
    public required string WorkspacePath { get; init; }

    /// <summary>
    /// Estimated number of markdown/yaml documents under docs/.
    /// </summary>
    public int DocumentCount { get; init; }

    /// <summary>
    /// Number of context excerpts loaded into the pack.
    /// </summary>
    public int ChunkCount { get; init; }

    /// <summary>
    /// Topic labels included in the context pack.
    /// </summary>
    public IReadOnlyList<string> Topics { get; init; } = [];

    /// <summary>
    /// Short human-readable summary of the bootstrapped context pack.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Bootstrap timestamp in ISO 8601 UTC format.
    /// </summary>
    public required string BootstrappedUtc { get; init; }

    /// <summary>
    /// Source keys represented in the seeded context pack.
    /// </summary>
    public IReadOnlyList<string> SourceKeys { get; init; } = [];

    /// <summary>
    /// Character length of the seeded context pack text injected into prompts.
    /// </summary>
    public int ContextCharacterCount { get; init; }
}

/// <summary>
/// FR-MCP-HELP-001: Status snapshot for an Agent Help session.
/// TR-MCP-HELP-002: Session status contract.
/// </summary>
public sealed record AgentHelpSessionStatusDto
{
    /// <summary>
    /// Help session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Current session status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Creation timestamp (UTC).
    /// </summary>
    public required string CreatedUtc { get; init; }

    /// <summary>
    /// Last-updated timestamp (UTC).
    /// </summary>
    public required string LastUpdatedUtc { get; init; }

    /// <summary>
    /// Whether a turn is actively being processed.
    /// </summary>
    public bool IsTurnActive { get; init; }

    /// <summary>
    /// Latest error, if any.
    /// </summary>
    public string? LastError { get; init; }

    /// <summary>
    /// Most recent turn identifier, if any.
    /// </summary>
    public string? LastTurnId { get; init; }

    /// <summary>
    /// Number of turns completed in this session.
    /// </summary>
    public int TurnCounter { get; init; }

    /// <summary>
    /// Execution strategy currently backing the session.
    /// </summary>
    public required string ExecutionStrategy { get; init; }

    /// <summary>
    /// Optional active TODO id bound to the session.
    /// </summary>
    public string? TodoId { get; init; }

    /// <summary>
    /// Optional topic label for the session.
    /// </summary>
    public string? Topic { get; init; }

    /// <summary>
    /// Whether the session was terminated due to a guardrail violation.
    /// </summary>
    public bool Terminated { get; init; }
}

/// <summary>
/// FR-MCP-HELP-003: Transcript response for an Agent Help session.
/// TR-MCP-HELP-003: Transcript retrieval contract.
/// </summary>
public sealed record AgentHelpTranscriptResponse
{
    /// <summary>
    /// Help session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Transcript entries captured for the session.
    /// </summary>
    public required IReadOnlyList<AgentHelpTranscriptEntry> Items { get; init; }
}

/// <summary>
/// FR-MCP-HELP-001: WebSocket client message frame for Agent Help streaming.
/// TR-MCP-HELP-002: Bidirectional stream client message contract.
/// </summary>
public sealed record AgentHelpWebSocketClientMessage
{
    /// <summary>
    /// Message type (e.g. <c>turn</c>).
    /// </summary>
    public required string Type { get; init; }

    /// <summary>
    /// User message text for <c>turn</c> frames.
    /// </summary>
    public string? UserMessage { get; init; }
}

/// <summary>
/// FR-MCP-HELP-005: Outcome triage recommendation for a completed help session.
/// TR-MCP-HELP-008: Triage recommendation contract.
/// </summary>
public sealed record AgentHelpTriageRecommendation
{
    /// <summary>
    /// Stable recommendation identifier.
    /// </summary>
    public required string Id { get; init; }

    /// <summary>
    /// Recommendation category (e.g. <c>documentation</c>, <c>todo</c>, <c>requirements</c>).
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Short title for the recommendation.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Detailed recommendation text.
    /// </summary>
    public required string Detail { get; init; }

    /// <summary>
    /// Suggested priority (e.g. <c>low</c>, <c>medium</c>, <c>high</c>).
    /// </summary>
    public string Priority { get; init; } = "medium";
}

/// <summary>
/// FR-MCP-HELP-005: Documentation TODO recommendation produced from a help session outcome.
/// TR-MCP-HELP-008: Doc TODO recommendation contract.
/// </summary>
public sealed record AgentHelpDocTodoRecommendation
{
    /// <summary>
    /// Suggested TODO identifier.
    /// </summary>
    public required string SuggestedTodoId { get; init; }

    /// <summary>
    /// Suggested TODO title.
    /// </summary>
    public required string Title { get; init; }

    /// <summary>
    /// Suggested TODO section.
    /// </summary>
    public required string Section { get; init; }

    /// <summary>
    /// Suggested TODO description lines.
    /// </summary>
    public IReadOnlyList<string> Description { get; init; } = [];

    /// <summary>
    /// Related functional requirement identifiers, when known.
    /// </summary>
    public IReadOnlyList<string> FunctionalRequirements { get; init; } = [];

    /// <summary>
    /// Related technical requirement identifiers, when known.
    /// </summary>
    public IReadOnlyList<string> TechnicalRequirements { get; init; } = [];
}

/// <summary>
/// FR-MCP-HELP-005: Aggregated outcome analysis for a help session.
/// TR-MCP-HELP-008: Outcome analysis response contract.
/// </summary>
public sealed record AgentHelpOutcomeAnalysis
{
    /// <summary>
    /// Help session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// High-level outcome summary.
    /// </summary>
    public required string Summary { get; init; }

    /// <summary>
    /// Triage recommendations derived from the session.
    /// </summary>
    public IReadOnlyList<AgentHelpTriageRecommendation> TriageRecommendations { get; init; } = [];

    /// <summary>
    /// Documentation TODO recommendations derived from the session.
    /// </summary>
    public IReadOnlyList<AgentHelpDocTodoRecommendation> DocTodoRecommendations { get; init; } = [];

    /// <summary>
    /// Analysis timestamp in ISO 8601 UTC format.
    /// </summary>
    public required string AnalyzedUtc { get; init; }
}