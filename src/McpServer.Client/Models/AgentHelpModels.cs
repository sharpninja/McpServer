using System.Text.Json.Serialization;

namespace McpServer.Client.Models;

/// <summary>FR-MCP-HELP-001: Request to create an Agent Help session.</summary>
public sealed record AgentHelpSessionCreateRequest
{
    /// <summary>Optional client/device identifier.</summary>
    [JsonPropertyName("deviceId")]
    public string? DeviceId { get; init; }

    /// <summary>Optional client display name.</summary>
    [JsonPropertyName("clientName")]
    public string? ClientName { get; init; }

    /// <summary>Workspace root path for helper execution.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; init; }

    /// <summary>Optional pooled-agent name.</summary>
    [JsonPropertyName("agentName")]
    public string? AgentName { get; init; }

    /// <summary>Optional agent binary path override.</summary>
    [JsonPropertyName("agentPath")]
    public string? AgentPath { get; init; }

    /// <summary>Optional model override.</summary>
    [JsonPropertyName("agentModel")]
    public string? AgentModel { get; init; }

    /// <summary>Optional seed prompt for the first turn.</summary>
    [JsonPropertyName("agentSeed")]
    public string? AgentSeed { get; init; }

    /// <summary>Optional key-value parameters forwarded as environment variables.</summary>
    [JsonPropertyName("agentParameters")]
    public Dictionary<string, string>? AgentParameters { get; init; }

    /// <summary>Optional execution strategy name.</summary>
    [JsonPropertyName("executionStrategy")]
    public string? ExecutionStrategy { get; init; }

    /// <summary>Optional active TODO id.</summary>
    [JsonPropertyName("todoId")]
    public string? TodoId { get; init; }

    /// <summary>Optional topic label.</summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; init; }

    /// <summary>Optional caller agent identity.</summary>
    [JsonPropertyName("callerAgent")]
    public string? CallerAgent { get; init; }

    /// <summary>Optional caller session id.</summary>
    [JsonPropertyName("callerSessionId")]
    public string? CallerSessionId { get; init; }

    /// <summary>Optional caller request/turn id.</summary>
    [JsonPropertyName("callerRequestId")]
    public string? CallerRequestId { get; init; }

    /// <summary>Optional factual issue summary.</summary>
    [JsonPropertyName("issueSummary")]
    public string? IssueSummary { get; init; }
}

/// <summary>FR-MCP-HELP-001: Response returned when an Agent Help session is created.</summary>
public sealed record AgentHelpSessionCreateResponse
{
    /// <summary>Help session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>Current session status.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Requested helper model.</summary>
    [JsonPropertyName("modelRequested")]
    public string? ModelRequested { get; init; }

    /// <summary>Resolved helper model.</summary>
    [JsonPropertyName("modelResolved")]
    public string? ModelResolved { get; init; }

    /// <summary>Execution strategy backing the session.</summary>
    [JsonPropertyName("executionStrategy")]
    public required string ExecutionStrategy { get; init; }

    /// <summary>Bootstrapped corpus summary when enabled.</summary>
    [JsonPropertyName("corpusSummary")]
    public AgentHelpCorpusSummary? CorpusSummary { get; init; }
}

/// <summary>FR-MCP-HELP-005: Corpus bootstrap summary.</summary>
public sealed record AgentHelpCorpusSummary
{
    /// <summary>Workspace path used for corpus bootstrap.</summary>
    [JsonPropertyName("workspacePath")]
    public string? WorkspacePath { get; init; }

    /// <summary>Estimated document count under docs/.</summary>
    [JsonPropertyName("documentCount")]
    public int DocumentCount { get; init; }

    /// <summary>Number of context excerpts loaded.</summary>
    [JsonPropertyName("chunkCount")]
    public int ChunkCount { get; init; }

    /// <summary>Topic labels included in the context pack.</summary>
    [JsonPropertyName("topics")]
    public IReadOnlyList<string> Topics { get; init; } = [];

    /// <summary>Human-readable corpus summary.</summary>
    [JsonPropertyName("summary")]
    public required string Summary { get; init; }

    /// <summary>Bootstrap timestamp in ISO 8601 UTC format.</summary>
    [JsonPropertyName("bootstrappedUtc")]
    public required string BootstrappedUtc { get; init; }

    /// <summary>Source keys represented in the context pack.</summary>
    [JsonPropertyName("sourceKeys")]
    public IReadOnlyList<string> SourceKeys { get; init; } = [];

    /// <summary>Character length of seeded context injected into prompts.</summary>
    [JsonPropertyName("contextCharacterCount")]
    public int ContextCharacterCount { get; init; }
}

/// <summary>FR-MCP-HELP-001: Request body for a single Agent Help turn.</summary>
public sealed record AgentHelpTurnRequest
{
    /// <summary>User message text for this turn.</summary>
    [JsonPropertyName("userMessage")]
    public required string UserMessage { get; init; }

    /// <summary>Optional client timestamp in ISO 8601 format.</summary>
    [JsonPropertyName("clientTimestampUtc")]
    public string? ClientTimestampUtc { get; init; }
}

/// <summary>FR-MCP-HELP-001: Response body for a completed Agent Help turn.</summary>
public sealed record AgentHelpTurnResponse
{
    /// <summary>Help session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>Turn identifier within the session.</summary>
    [JsonPropertyName("turnId")]
    public required string TurnId { get; init; }

    /// <summary>Final turn status.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Text intended for display in the transcript.</summary>
    [JsonPropertyName("assistantDisplayText")]
    public string? AssistantDisplayText { get; init; }

    /// <summary>User-visible error message when the turn fails or is blocked.</summary>
    [JsonPropertyName("error")]
    public string? Error { get; init; }

    /// <summary>Turn latency in milliseconds.</summary>
    [JsonPropertyName("latencyMs")]
    public int LatencyMs { get; init; }

    /// <summary>Guard evaluation result when inbound guardrails are enabled.</summary>
    [JsonPropertyName("guardResult")]
    public AgentHelpGuardResult? GuardResult { get; init; }

    /// <summary>Guard incident identifier when a turn terminates the session.</summary>
    [JsonPropertyName("incidentId")]
    public string? IncidentId { get; init; }
}

/// <summary>FR-MCP-HELP-002: Deterministic inbound guard evaluation result.</summary>
public sealed record AgentHelpGuardResult
{
    /// <summary>Whether the inbound message is allowed.</summary>
    [JsonPropertyName("allowed")]
    public bool Allowed { get; init; }

    /// <summary>Matched guard rule identifier when blocked.</summary>
    [JsonPropertyName("ruleId")]
    public string? RuleId { get; init; }

    /// <summary>Human-readable block reason.</summary>
    [JsonPropertyName("reason")]
    public string? Reason { get; init; }

    /// <summary>Matched snippet from the inbound message.</summary>
    [JsonPropertyName("matchedSnippet")]
    public string? MatchedSnippet { get; init; }
}

/// <summary>FR-MCP-HELP-001: Status snapshot for an Agent Help session.</summary>
public sealed record AgentHelpSessionStatusDto
{
    /// <summary>Help session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>Current session status.</summary>
    [JsonPropertyName("status")]
    public required string Status { get; init; }

    /// <summary>Creation timestamp (UTC).</summary>
    [JsonPropertyName("createdUtc")]
    public required string CreatedUtc { get; init; }

    /// <summary>Last-updated timestamp (UTC).</summary>
    [JsonPropertyName("lastUpdatedUtc")]
    public required string LastUpdatedUtc { get; init; }

    /// <summary>Whether a turn is actively being processed.</summary>
    [JsonPropertyName("isTurnActive")]
    public bool IsTurnActive { get; init; }

    /// <summary>Latest error, if any.</summary>
    [JsonPropertyName("lastError")]
    public string? LastError { get; init; }

    /// <summary>Most recent turn identifier, if any.</summary>
    [JsonPropertyName("lastTurnId")]
    public string? LastTurnId { get; init; }

    /// <summary>Number of turns completed in this session.</summary>
    [JsonPropertyName("turnCounter")]
    public int TurnCounter { get; init; }

    /// <summary>Execution strategy currently backing the session.</summary>
    [JsonPropertyName("executionStrategy")]
    public required string ExecutionStrategy { get; init; }

    /// <summary>Optional active TODO id bound to the session.</summary>
    [JsonPropertyName("todoId")]
    public string? TodoId { get; init; }

    /// <summary>Optional topic label for the session.</summary>
    [JsonPropertyName("topic")]
    public string? Topic { get; init; }

    /// <summary>Whether the session was terminated due to a guardrail violation.</summary>
    [JsonPropertyName("terminated")]
    public bool Terminated { get; init; }
}

/// <summary>FR-MCP-HELP-003: Transcript response for an Agent Help session.</summary>
public sealed record AgentHelpTranscriptResponse
{
    /// <summary>Help session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>Transcript entries captured for the session.</summary>
    [JsonPropertyName("items")]
    public required IReadOnlyList<AgentHelpTranscriptEntry> Items { get; init; }
}

/// <summary>FR-MCP-HELP-003: Append-only transcript entry.</summary>
public sealed record AgentHelpTranscriptEntry
{
    /// <summary>Entry timestamp in ISO 8601 UTC format.</summary>
    [JsonPropertyName("timestampUtc")]
    public required string TimestampUtc { get; init; }

    /// <summary>Help session identifier.</summary>
    [JsonPropertyName("sessionId")]
    public required string SessionId { get; init; }

    /// <summary>Optional turn identifier.</summary>
    [JsonPropertyName("turnId")]
    public string? TurnId { get; init; }

    /// <summary>Transcript role (user, assistant, system).</summary>
    [JsonPropertyName("role")]
    public required string Role { get; init; }

    /// <summary>Transcript category (transcript, guardrail_violation, corpus).</summary>
    [JsonPropertyName("category")]
    public required string Category { get; init; }

    /// <summary>Transcript text.</summary>
    [JsonPropertyName("text")]
    public required string Text { get; init; }
}