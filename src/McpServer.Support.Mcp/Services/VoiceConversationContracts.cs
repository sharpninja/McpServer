namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Request to create a voice conversation session.
/// </summary>
public sealed record VoiceSessionCreateRequest
{
    /// <summary>
    /// Preferred language tag for STT/TTS text (default <c>en-US</c>).
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Optional client/device identifier for diagnostics.
    /// </summary>
    public string? DeviceId { get; init; }

    /// <summary>
    /// Optional client display name.
    /// </summary>
    public string? ClientName { get; init; }

    /// <summary>
    /// Workspace root path to use as CWD when launching Copilot. Typically resolved from X-Workspace-Path header.
    /// </summary>
    public string? WorkspacePath { get; set; }
}

/// <summary>
/// Response returned when a voice session is created.
/// </summary>
public sealed record VoiceSessionCreateResponse
{
    /// <summary>
    /// Voice session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Current status (e.g. <c>idle</c>).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Requested language for the session.
    /// </summary>
    public required string Language { get; init; }

    /// <summary>
    /// Model identifier requested for Copilot CLI.
    /// </summary>
    public string? ModelRequested { get; init; }

    /// <summary>
    /// Model identifier actually used (same as requested in this implementation).
    /// </summary>
    public string? ModelResolved { get; init; }
}

/// <summary>
/// Request body for a single voice conversation turn.
/// </summary>
public sealed record VoiceTurnRequest
{
    /// <summary>
    /// Final transcript text captured on the Android client for this turn.
    /// </summary>
    public required string UserTranscriptText { get; init; }

    /// <summary>
    /// Optional language tag for the transcript.
    /// </summary>
    public string? Language { get; init; }

    /// <summary>
    /// Optional client timestamp in ISO 8601 format.
    /// </summary>
    public string? ClientTimestampUtc { get; init; }
}

/// <summary>
/// Response body for a completed voice turn.
/// </summary>
public sealed record VoiceTurnResponse
{
    /// <summary>
    /// Voice session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Turn identifier within the session.
    /// </summary>
    public required string TurnId { get; init; }

    /// <summary>
    /// Final turn status (e.g. <c>completed</c>, <c>interrupted</c>, <c>error</c>).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Text intended for display in the UI transcript.
    /// </summary>
    public string? AssistantDisplayText { get; init; }

    /// <summary>
    /// Text intended for TTS playback (shorter/speak-friendly).
    /// </summary>
    public string? AssistantSpeakText { get; init; }

    /// <summary>
    /// Tool calls attempted/executed during the turn.
    /// </summary>
    public IReadOnlyList<VoiceToolCallRecordDto>? ToolCalls { get; init; }

    /// <summary>
    /// User-visible error message when the turn fails.
    /// </summary>
    public string? Error { get; init; }

    /// <summary>
    /// Turn latency in milliseconds.
    /// </summary>
    public int LatencyMs { get; init; }

    /// <summary>
    /// Model identifier requested for this turn.
    /// </summary>
    public string? ModelRequested { get; init; }

    /// <summary>
    /// Model identifier actually used for this turn.
    /// </summary>
    public string? ModelResolved { get; init; }
}

/// <summary>
/// Response returned when interrupting a voice session turn.
/// </summary>
public sealed record VoiceInterruptResponse
{
    /// <summary>
    /// Voice session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Whether an active turn was found and cancellation was signaled.
    /// </summary>
    public bool Interrupted { get; init; }

    /// <summary>
    /// Current session status after interrupt processing.
    /// </summary>
    public required string Status { get; init; }
}

/// <summary>
/// Status snapshot for a voice conversation session.
/// </summary>
public sealed record VoiceSessionStatusDto
{
    /// <summary>
    /// Voice session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Current session status.
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Session language.
    /// </summary>
    public required string Language { get; init; }

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
}

/// <summary>
/// Transcript entry stored for a voice session.
/// </summary>
public sealed record VoiceTranscriptEntryDto
{
    /// <summary>
    /// Timestamp in ISO 8601 UTC format.
    /// </summary>
    public required string TimestampUtc { get; init; }

    /// <summary>
    /// Turn identifier associated with the entry.
    /// </summary>
    public string? TurnId { get; init; }

    /// <summary>
    /// Entry role (e.g. user, assistant, tool, system).
    /// </summary>
    public required string Role { get; init; }

    /// <summary>
    /// Entry category (e.g. transcript, tool_call, tool_result, error).
    /// </summary>
    public required string Category { get; init; }

    /// <summary>
    /// Entry text content.
    /// </summary>
    public required string Text { get; init; }
}

/// <summary>
/// Tool-call execution record captured during a voice turn.
/// </summary>
public sealed record VoiceToolCallRecordDto
{
    /// <summary>
    /// Turn identifier.
    /// </summary>
    public required string TurnId { get; init; }

    /// <summary>
    /// Tool name.
    /// </summary>
    public required string ToolName { get; init; }

    /// <summary>
    /// Tool-call step number (1-based) within the turn.
    /// </summary>
    public int Step { get; init; }

    /// <summary>
    /// Raw JSON arguments string after normalization.
    /// </summary>
    public required string ArgumentsJson { get; init; }

    /// <summary>
    /// Execution status (e.g. executed, blocked, failed).
    /// </summary>
    public required string Status { get; init; }

    /// <summary>
    /// Whether the tool call is a mutating todo operation.
    /// </summary>
    public bool IsMutation { get; init; }

    /// <summary>
    /// Short summary of the tool result.
    /// </summary>
    public string? ResultSummary { get; init; }

    /// <summary>
    /// Policy rejection or failure reason, when applicable.
    /// </summary>
    public string? Error { get; init; }
}

/// <summary>
/// Transcript query response for a voice session.
/// </summary>
public sealed record VoiceTranscriptResponse
{
    /// <summary>
    /// Voice session identifier.
    /// </summary>
    public required string SessionId { get; init; }

    /// <summary>
    /// Transcript items recorded for the session.
    /// </summary>
    public required IReadOnlyList<VoiceTranscriptEntryDto> Items { get; init; }
}

/// <summary>
/// Voice conversation service contract for session and turn management.
/// </summary>
public interface IVoiceConversationService
{
    /// <summary>
    /// Creates a new voice session.
    /// </summary>
    Task<VoiceSessionCreateResponse> CreateSessionAsync(VoiceSessionCreateRequest? request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Processes a voice turn for an existing session.
    /// </summary>
    Task<VoiceTurnResponse?> SubmitTurnAsync(string sessionId, VoiceTurnRequest request, CancellationToken cancellationToken = default);

    /// <summary>
    /// Interrupts any active turn for the session.
    /// </summary>
    Task<VoiceInterruptResponse?> InterruptAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets session status details.
    /// </summary>
    Task<VoiceSessionStatusDto?> GetStatusAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets transcript entries for the session.
    /// </summary>
    Task<VoiceTranscriptResponse?> GetTranscriptAsync(string sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes a voice session and its in-memory transcript history.
    /// </summary>
    Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default);
}
