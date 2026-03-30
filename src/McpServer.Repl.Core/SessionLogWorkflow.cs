// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Session log workflow operations
// FR-MCP-REPL-002: REPL Lifecycle Management - Session state management and turn lifecycle
// FR-MCP-REPL-003: Command Namespace Parity - Session log operations via REPL commands
// TR-MCP-REPL-001: YAML Envelope Protocol - Session log command/response framing
// TR-MCP-REPL-002: DI-Integrated REPL Host - Workflow service DI registration
// TR-MCP-REPL-003: Command Loop Lifecycle - Session log turn lifecycle state management
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Session log command handlers
// TEST-MCP-REPL-006: Session log workflow operation parity with REST endpoints

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Session log workflow implementation
// FR-MCP-REPL-002: REPL Lifecycle Management - Turn lifecycle and state management
// FR-MCP-REPL-003: Command Namespace Parity - Session log operation implementation
// TR-MCP-REPL-004: Command Registry and Dispatcher - Session log workflow handler
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Session log workflow delegation
// TEST-MCP-REPL-007: Session log REPL commands match REST endpoint semantics
// TEST-MCP-REPL-020: Session state and turn context properly isolated

using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>
/// Production implementation of <see cref="ISessionLogWorkflow"/> that maintains thread-safe
/// in-memory session state and persists all operations through <see cref="ISessionLogClientAdapter"/>.
/// </summary>
/// <remarks>
/// This implementation provides:
/// <list type="bullet">
/// <item>Thread-safe session and turn state management via <see cref="SessionLogState"/></item>
/// <item>Real <see cref="SessionLogClient"/> operations (SubmitAsync, AppendDialogAsync, QueryAsync)</item>
/// <item>Turn lifecycle enforcement (in_progress → completed/failed, with immutability)</item>
/// <item>Canonical identifier validation (session ID and request ID format rules)</item>
/// <item>Structured error handling with YAML error envelope codes</item>
/// </list>
/// </remarks>
public sealed class SessionLogWorkflow : ISessionLogWorkflow
{
    private readonly ISessionLogClientAdapter _client;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private SessionLogState? _state;

    /// <summary>
    /// Initializes a new <see cref="SessionLogWorkflow"/> with the specified client adapter and time provider.
    /// </summary>
    /// <param name="client">The client adapter for persisting session data.</param>
    /// <param name="timeProvider">The TimeProvider for generating timestamps.</param>
    /// <exception cref="ArgumentNullException">Thrown if client or timeProvider is null.</exception>
    public SessionLogWorkflow(ISessionLogClientAdapter client, TimeProvider timeProvider)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
    }

    /// <summary>
    /// Initializes a new <see cref="SessionLogWorkflow"/> with the specified SessionLogClient and time provider.
    /// </summary>
    /// <param name="client">The SessionLogClient for persisting session data.</param>
    /// <param name="timeProvider">The TimeProvider for generating timestamps.</param>
    /// <exception cref="ArgumentNullException">Thrown if client or timeProvider is null.</exception>
    public SessionLogWorkflow(SessionLogClient client, TimeProvider timeProvider)
        : this(new SessionLogClientAdapter(client), timeProvider)
    {
    }

    /// <inheritdoc />
    public async Task BootstrapAsync(CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // Bootstrap is idempotent - no-op if already bootstrapped
            // No initialization required for this implementation
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task OpenSessionAsync(string agent, string sessionId, string title, string model, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(agent, nameof(agent));
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId, nameof(sessionId));
        ArgumentException.ThrowIfNullOrWhiteSpace(title, nameof(title));
        ArgumentException.ThrowIfNullOrWhiteSpace(model, nameof(model));

        ValidateSessionId(sessionId, agent);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (_state != null && _state.SessionId == sessionId)
            {
                throw new InvalidOperationException($"Session with ID {sessionId} already exists");
            }

            var now = _timeProvider.GetUtcNow();
            var sessionLog = new UnifiedSessionLogDto
            {
                SourceType = agent,
                SessionId = sessionId,
                Title = title,
                Model = model,
                Started = now.ToString("o"),
                LastUpdated = now.ToString("o"),
                Status = "in_progress",
                TurnCount = 0,
                Turns = new List<UnifiedRequestEntryDto>()
            };

            await _client.SubmitAsync(sessionLog, cancellationToken).ConfigureAwait(false);

            _state = new SessionLogState(agent, sessionId, title, model, now);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public ISessionLogState? CurrentSession()
    {
        return _state;
    }

    /// <inheritdoc />
    public async Task BeginTurnAsync(string requestId, string queryTitle, string queryText, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(requestId, nameof(requestId));
        ArgumentException.ThrowIfNullOrWhiteSpace(queryTitle, nameof(queryTitle));
        ArgumentException.ThrowIfNullOrWhiteSpace(queryText, nameof(queryText));

        ValidateRequestId(requestId);

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = EnsureSessionActive();
            state.BeginTurn(requestId, queryTitle, queryText, _timeProvider.GetUtcNow());

            await SubmitCurrentStateAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task UpdateTurnAsync(
        string? response = null,
        string? interpretation = null,
        int? tokenCount = null,
        IReadOnlyList<string>? tags = null,
        IReadOnlyList<string>? contextList = null,
        CancellationToken cancellationToken = default)
    {
        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = EnsureSessionActive();
            state.UpdateTurn(response, interpretation, tokenCount, tags, contextList, _timeProvider.GetUtcNow());

            await SubmitCurrentStateAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task CompleteTurnAsync(string response, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(response, nameof(response));

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = EnsureSessionActive();
            state.CompleteTurn(response, _timeProvider.GetUtcNow());

            await SubmitCurrentStateAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task FailTurnAsync(string errorMessage, string? errorCode = null, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(errorMessage, nameof(errorMessage));

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = EnsureSessionActive();
            state.FailTurn(errorMessage, errorCode, _timeProvider.GetUtcNow());

            await SubmitCurrentStateAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task AppendDialogAsync(IReadOnlyList<IDialogItem> dialogItems, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dialogItems, nameof(dialogItems));
        if (dialogItems.Count == 0)
        {
            throw new ArgumentException("DialogItems cannot be empty", nameof(dialogItems));
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = EnsureSessionActive();
            var requestId = state.EnsureTurnActive();

            var items = dialogItems.Select(d => new ProcessingDialogItemDto
            {
                Timestamp = d.Timestamp.ToString("o"),
                Role = d.Role,
                Content = d.Content,
                Category = d.Category
            }).ToList();

            await _client.AppendDialogAsync(
                state.Agent,
                state.SessionId,
                requestId,
                items,
                cancellationToken).ConfigureAwait(false);

            state.AppendDialog(dialogItems, _timeProvider.GetUtcNow());
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task AppendActionsAsync(IReadOnlyList<ISessionAction> actions, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(actions, nameof(actions));
        if (actions.Count == 0)
        {
            throw new ArgumentException("Actions cannot be empty", nameof(actions));
        }

        await _lock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var state = EnsureSessionActive();
            state.AppendActions(actions, _timeProvider.GetUtcNow());

            await SubmitCurrentStateAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            _lock.Release();
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ISessionLogSummary>> QueryHistoryAsync(
        string? agent = null,
        int limit = 10,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        if (limit < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(limit), "Limit cannot be negative");
        }
        if (offset < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(offset), "Offset cannot be negative");
        }

        var result = await _client.QueryAsync(
            agent: agent,
            limit: limit,
            offset: offset,
            cancellationToken: cancellationToken).ConfigureAwait(false);

        return result.Items.Select(item => new SessionLogSummary(item)).ToList();
    }

    private SessionLogState EnsureSessionActive()
    {
        if (_state == null)
        {
            throw new InvalidOperationException("No active session exists");
        }
        return _state;
    }

    private async Task SubmitCurrentStateAsync(CancellationToken cancellationToken)
    {
        if (_state == null)
        {
            return;
        }

        var sessionLog = _state.ToDto();
        await _client.SubmitAsync(sessionLog, cancellationToken).ConfigureAwait(false);
    }

    private static void ValidateSessionId(string sessionId, string agent)
    {
        // Format: <Agent>-<yyyyMMddTHHmmssZ>-<suffix>
        // Regex: ^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$
        if (!System.Text.RegularExpressions.Regex.IsMatch(sessionId,
            @"^[A-Z][A-Za-z0-9]*-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            throw new ArgumentException($"Invalid session ID format: {sessionId}", nameof(sessionId));
        }

        // Ensure prefix matches agent name
        var prefix = sessionId.Split('-')[0];
        if (prefix != agent)
        {
            throw new ArgumentException("Session ID prefix must match agent name", nameof(sessionId));
        }
    }

    private static void ValidateRequestId(string requestId)
    {
        // Format: req-<yyyyMMddTHHmmssZ>-<slugOrOrdinal>
        // Regex: ^req-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$
        if (!System.Text.RegularExpressions.Regex.IsMatch(requestId,
            @"^req-\d{8}T\d{6}Z-[a-z0-9]+(?:-[a-z0-9]+)*$"))
        {
            throw new ArgumentException($"Invalid request ID format: {requestId}", nameof(requestId));
        }
    }
}

/// <summary>
/// Thread-safe in-memory state for the active session and current turn.
/// Tracks session metadata, turn lifecycle, and enforces turn immutability rules.
/// </summary>
internal sealed class SessionLogState : ISessionLogState
{
    private readonly List<TurnState> _turns = new();
    private TurnState? _activeTurn;

    public SessionLogState(string agent, string sessionId, string title, string model, DateTimeOffset started)
    {
        Agent = agent;
        SessionId = sessionId;
        Title = title;
        Model = model;
        Started = started;
        LastUpdated = started;
        Status = "in_progress";
    }

    public string Agent { get; }
    public string SessionId { get; }
    public string Title { get; }
    public string Model { get; }
    public DateTimeOffset Started { get; }
    public DateTimeOffset LastUpdated { get; private set; }
    public string Status { get; private set; }
    public string? CurrentTurnRequestId => _activeTurn?.RequestId;
    public string? CurrentTurnStatus => _activeTurn?.Status;
    public int TurnCount => _turns.Count;

    public void BeginTurn(string requestId, string queryTitle, string queryText, DateTimeOffset timestamp)
    {
        // Check for duplicate turn
        if (_turns.Any(t => t.RequestId == requestId))
        {
            throw new InvalidOperationException($"Turn with request ID {requestId} already exists");
        }

        // Check if another turn is already active
        if (_activeTurn != null)
        {
            throw new InvalidOperationException("A turn is already in progress");
        }

        _activeTurn = new TurnState(requestId, queryTitle, queryText, timestamp);
        LastUpdated = timestamp;
    }

    public void UpdateTurn(string? response, string? interpretation, int? tokenCount,
        IReadOnlyList<string>? tags, IReadOnlyList<string>? contextList, DateTimeOffset timestamp)
    {
        EnsureTurnMutable();

        if (response != null) _activeTurn!.Response = response;
        if (interpretation != null) _activeTurn!.Interpretation = interpretation;
        if (tokenCount.HasValue) _activeTurn!.TokenCount = tokenCount.Value;
        if (tags != null) _activeTurn!.Tags = new List<string>(tags);
        if (contextList != null) _activeTurn!.ContextList = new List<string>(contextList);

        LastUpdated = timestamp;
    }

    public void CompleteTurn(string response, DateTimeOffset timestamp)
    {
        EnsureTurnMutable();

        _activeTurn!.Response = response;
        _activeTurn.Status = "completed";
        _turns.Add(_activeTurn);

        _activeTurn = null;
        LastUpdated = timestamp;
    }

    public void FailTurn(string errorMessage, string? errorCode, DateTimeOffset timestamp)
    {
        EnsureTurnMutable();

        _activeTurn!.Status = "failed";
        _activeTurn.FailureNote = errorCode != null ? $"{errorCode}: {errorMessage}" : errorMessage;
        _turns.Add(_activeTurn);

        _activeTurn = null;
        LastUpdated = timestamp;
    }

    public void AppendDialog(IReadOnlyList<IDialogItem> dialogItems, DateTimeOffset timestamp)
    {
        EnsureTurnMutable();

        _activeTurn!.ProcessingDialog.AddRange(dialogItems);
        LastUpdated = timestamp;
    }

    public void AppendActions(IReadOnlyList<ISessionAction> actions, DateTimeOffset timestamp)
    {
        EnsureTurnMutable();

        _activeTurn!.Actions.AddRange(actions);
        LastUpdated = timestamp;
    }

    public string EnsureTurnActive()
    {
        if (_activeTurn == null)
        {
            throw new InvalidOperationException("No active turn");
        }
        return _activeTurn.RequestId;
    }

    private void EnsureTurnMutable()
    {
        if (_activeTurn == null)
        {
            throw new InvalidOperationException("No active turn");
        }
        if (_activeTurn.Status != "in_progress")
        {
            throw new InvalidOperationException($"Turn is immutable (status: {_activeTurn.Status})");
        }
    }

    public UnifiedSessionLogDto ToDto()
    {
        var allTurns = _turns.Select(t => t.ToDto()).ToList();
        
        // Add active turn if present
        if (_activeTurn != null)
        {
            allTurns.Add(_activeTurn.ToDto());
        }

        return new UnifiedSessionLogDto
        {
            SourceType = Agent,
            SessionId = SessionId,
            Title = Title,
            Model = Model,
            Started = Started.ToString("o"),
            LastUpdated = LastUpdated.ToString("o"),
            Status = Status,
            TurnCount = _turns.Count + (_activeTurn != null ? 1 : 0),
            Turns = allTurns
        };
    }
}

/// <summary>
/// Internal state for a single turn within a session.
/// </summary>
internal sealed class TurnState
{
    public TurnState(string requestId, string queryTitle, string queryText, DateTimeOffset timestamp)
    {
        RequestId = requestId;
        QueryTitle = queryTitle;
        QueryText = queryText;
        Timestamp = timestamp;
        Status = "in_progress";
        ProcessingDialog = new List<IDialogItem>();
        Actions = new List<ISessionAction>();
        Tags = new List<string>();
        ContextList = new List<string>();
    }

    public string RequestId { get; }
    public string QueryTitle { get; }
    public string QueryText { get; }
    public DateTimeOffset Timestamp { get; }
    public string Status { get; set; }
    public string? Response { get; set; }
    public string? Interpretation { get; set; }
    public int? TokenCount { get; set; }
    public string? FailureNote { get; set; }
    public List<string> Tags { get; set; }
    public List<string> ContextList { get; set; }
    public List<IDialogItem> ProcessingDialog { get; }
    public List<ISessionAction> Actions { get; }

    public UnifiedRequestEntryDto ToDto()
    {
        return new UnifiedRequestEntryDto
        {
            RequestId = RequestId,
            Timestamp = Timestamp.ToString("o"),
            QueryText = QueryText,
            QueryTitle = QueryTitle,
            Response = Response,
            Interpretation = Interpretation,
            Status = Status,
            TokenCount = TokenCount,
            FailureNote = FailureNote,
            Tags = Tags.Count > 0 ? Tags : null,
            ContextList = ContextList.Count > 0 ? ContextList : null,
            ProcessingDialog = ProcessingDialog.Count > 0 
                ? ProcessingDialog.Select(d => new ProcessingDialogItemDto
                {
                    Timestamp = d.Timestamp.ToString("o"),
                    Role = d.Role,
                    Content = d.Content,
                    Category = d.Category
                }).ToList()
                : null,
            Actions = Actions.Count > 0
                ? Actions.Select(a => new UnifiedActionDto
                {
                    Order = a.Order,
                    Description = a.Description,
                    Type = a.Type,
                    Status = a.Status,
                    FilePath = a.FilePath
                }).ToList()
                : null
        };
    }
}

/// <summary>
/// Implementation of <see cref="ISessionLogSummary"/> for query results.
/// </summary>
internal sealed class SessionLogSummary : ISessionLogSummary
{
    public SessionLogSummary(UnifiedSessionLogDto dto)
    {
        Agent = dto.SourceType ?? string.Empty;
        SessionId = dto.SessionId ?? string.Empty;
        Title = dto.Title ?? string.Empty;
        Model = dto.Model ?? string.Empty;
        Started = DateTimeOffset.TryParse(dto.Started, out var started) ? started : DateTimeOffset.MinValue;
        LastUpdated = DateTimeOffset.TryParse(dto.LastUpdated, out var updated) ? updated : DateTimeOffset.MinValue;
        Status = dto.Status ?? "unknown";
        TurnCount = dto.TurnCount;
        
        // Extract tags from turns
        var tags = new HashSet<string>();
        if (dto.Turns != null)
        {
            foreach (var turn in dto.Turns)
            {
                if (turn.Tags != null)
                {
                    foreach (var tag in turn.Tags)
                    {
                        tags.Add(tag);
                    }
                }
            }
        }
        Tags = tags.ToList();

        // Count unique files modified
        var files = new HashSet<string>();
        if (dto.Turns != null)
        {
            foreach (var turn in dto.Turns)
            {
                if (turn.FilesModified != null)
                {
                    foreach (var file in turn.FilesModified)
                    {
                        files.Add(file);
                    }
                }
                if (turn.Actions != null)
                {
                    foreach (var action in turn.Actions)
                    {
                        if (!string.IsNullOrWhiteSpace(action.FilePath))
                        {
                            files.Add(action.FilePath);
                        }
                    }
                }
            }
        }
        FilesModifiedCount = files.Count;
    }

    public string Agent { get; }
    public string SessionId { get; }
    public string Title { get; }
    public string Model { get; }
    public DateTimeOffset Started { get; }
    public DateTimeOffset LastUpdated { get; }
    public string Status { get; }
    public int TurnCount { get; }
    public IReadOnlyList<string> Tags { get; }
    public int FilesModifiedCount { get; }
}

/// <summary>
/// Adapter interface for SessionLogClient operations.
/// Allows for testing with stub implementations.
/// </summary>
public interface ISessionLogClientAdapter
{
    /// <summary>
    /// Submit (upsert) a session log entry.
    /// </summary>
    Task<SessionLogSubmitResult> SubmitAsync(UnifiedSessionLogDto sessionLog, CancellationToken cancellationToken = default);

    /// <summary>
    /// Query session logs with optional filters.
    /// </summary>
    Task<SessionLogQueryResult> QueryAsync(
        string? agent = null,
        string? model = null,
        string? text = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Append processing dialog items to a session log turn.
    /// </summary>
    Task<DialogAppendResult> AppendDialogAsync(
        string agent,
        string sessionId,
        string requestId,
        List<ProcessingDialogItemDto> items,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// Production adapter for SessionLogClient.
/// </summary>
internal sealed class SessionLogClientAdapter : ISessionLogClientAdapter
{
    private readonly SessionLogClient _client;

    public SessionLogClientAdapter(SessionLogClient client)
    {
        _client = client ?? throw new ArgumentNullException(nameof(client));
    }

    public Task<SessionLogSubmitResult> SubmitAsync(UnifiedSessionLogDto sessionLog, CancellationToken cancellationToken = default)
    {
        return _client.SubmitAsync(sessionLog, cancellationToken);
    }

    public Task<SessionLogQueryResult> QueryAsync(
        string? agent = null,
        string? model = null,
        string? text = null,
        DateTimeOffset? from = null,
        DateTimeOffset? to = null,
        int limit = 100,
        int offset = 0,
        CancellationToken cancellationToken = default)
    {
        return _client.QueryAsync(agent, model, text, from, to, limit, offset, cancellationToken);
    }

    public Task<DialogAppendResult> AppendDialogAsync(
        string agent,
        string sessionId,
        string requestId,
        List<ProcessingDialogItemDto> items,
        CancellationToken cancellationToken = default)
    {
        return _client.AppendDialogAsync(agent, sessionId, requestId, items, cancellationToken);
    }
}
