using McpServer.Client.Models;

namespace McpServer.McpAgent.SessionLog;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: In-memory workflow context that holds session metadata and
/// strongly typed turn state for a single session-log workflow run.
/// <para>
/// The context is created by <see cref="ISessionLogWorkflow.BootstrapAsync"/> and mutated by
/// subsequent workflow calls. Continue a session within the current host process by holding on to
/// the returned <see cref="SessionLogWorkflowContext"/> and <see cref="SessionLogTurnContext"/>
/// instances; cross-process resume by session ID alone is not currently supported because
/// <c>McpServer.Client</c> does not expose direct session lookup by identifier.
/// </para>
/// </summary>
public sealed class SessionLogWorkflowContext
{
    private readonly List<SessionLogTurnContext> _turns = [];

    internal SessionLogWorkflowContext(string sessionId, string sourceType)
    {
        SessionId = sessionId ?? throw new ArgumentNullException(nameof(sessionId));
        SourceType = sourceType ?? throw new ArgumentNullException(nameof(sourceType));
    }

    /// <summary>
    /// Gets the canonical session identifier assigned at bootstrap.
    /// </summary>
    public string SessionId { get; }

    /// <summary>
    /// Gets the canonical source type prefix for this session (e.g. <c>McpAgent</c>).
    /// </summary>
    public string SourceType { get; }

    /// <summary>
    /// Gets or sets the human-readable session title.
    /// </summary>
    public string? Title { get; internal set; }

    /// <summary>
    /// Gets or sets the AI model identifier used for this session.
    /// </summary>
    public string? Model { get; internal set; }

    /// <summary>
    /// Gets or sets the session status string (e.g. <c>in_progress</c>, <c>completed</c>).
    /// </summary>
    public string Status { get; internal set; } = "in_progress";

    /// <summary>
    /// Gets or sets optional workspace metadata associated with the session.
    /// </summary>
    public WorkspaceInfoDto? Workspace { get; internal set; }

    /// <summary>
    /// Gets or sets the session start time as an ISO 8601 string.
    /// </summary>
    public string? Started { get; internal set; }

    /// <summary>
    /// Gets or sets the last update time as an ISO 8601 string; updated automatically on each submit.
    /// </summary>
    public string? LastUpdated { get; internal set; }

    /// <summary>
    /// Gets the strongly typed turn state accumulated during this session.
    /// </summary>
    public IReadOnlyList<SessionLogTurnContext> Turns => _turns;

    /// <summary>
    /// Gets a DTO projection of the current turn state. This is primarily useful for diagnostics,
    /// assertions, and inspecting the payload shape submitted through <see cref="McpServer.Client.SessionLogClient"/>.
    /// </summary>
    public IReadOnlyList<UnifiedRequestEntryDto> TurnDtos => _turns.Count == 0
        ? Array.Empty<UnifiedRequestEntryDto>()
        : _turns.Select(static turn => turn.ToDto()).ToList();

    /// <summary>
    /// Gets the number of turns in this session, computed from <see cref="Turns"/>.
    /// </summary>
    public int TurnCount => _turns.Count;

    /// <summary>
    /// Gets the total token count across all turns that have a token count set, or
    /// <see langword="null"/> when no entry carries token information.
    /// </summary>
    public int? TotalTokens => _turns.Any(e => e.TokenCount.HasValue)
        ? _turns.Sum(e => e.TokenCount ?? 0)
        : null;

    /// <summary>
    /// Finds a turn by request identifier.
    /// </summary>
    /// <param name="requestId">Canonical request identifier to locate.</param>
    /// <returns>The matching turn when found; otherwise <see langword="null"/>.</returns>
    public SessionLogTurnContext? FindTurn(string requestId) =>
        _turns.FirstOrDefault(turn => string.Equals(turn.RequestId, requestId, StringComparison.Ordinal));

    internal void AddTurn(SessionLogTurnContext turn) => _turns.Add(turn);

    internal UnifiedSessionLogDto ToSubmitDto() => new()
    {
        SourceType = SourceType,
        SessionId = SessionId,
        Title = Title,
        Model = Model,
        Status = Status,
        Workspace = Workspace,
        Started = Started,
        LastUpdated = LastUpdated,
        TurnCount = TurnCount,
        TotalTokens = TotalTokens,
        Turns = _turns.Count > 0 ? _turns.Select(static turn => turn.ToDto()).ToList() : null,
    };
}
