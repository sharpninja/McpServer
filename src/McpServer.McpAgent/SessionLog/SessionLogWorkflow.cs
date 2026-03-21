using McpServer.Client;
using McpServer.Client.Models;

namespace McpServer.McpAgent.SessionLog;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Default implementation of <see cref="ISessionLogWorkflow"/> that
/// maintains a strongly typed in-memory <see cref="SessionLogWorkflowContext"/> and persists session
/// mutations through <see cref="SessionLogClient"/>.
/// <para>
/// Canonical session and request identifiers are generated or validated through the
/// <see cref="IMcpSessionIdentifierFactory"/> supplied at construction so all identifiers are
/// consistent with the configured source type.
/// </para>
/// </summary>
public sealed class SessionLogWorkflow : ISessionLogWorkflow
{
    private readonly IMcpSessionIdentifierFactory _identifiers;
    private readonly SessionLogClient _sessionLogClient;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _mutationGate = new(1, 1);
    private SessionLogWorkflowContext? _context;

    /// <summary>
    /// Initializes a new <see cref="SessionLogWorkflow"/> with the supplied dependencies.
    /// </summary>
    /// <param name="client">
    /// MCP Server transport client whose <see cref="McpServerClient.SessionLog"/> surface is used
    /// to persist workflow state.
    /// </param>
    /// <param name="identifiers">Canonical identifier factory bound to the agent's source type.</param>
    /// <param name="timeProvider">Time provider used to stamp session and request turns.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when any parameter is <see langword="null"/>.
    /// </exception>
    public SessionLogWorkflow(
        McpServerClient client,
        IMcpSessionIdentifierFactory identifiers,
        TimeProvider timeProvider)
    {
        ArgumentNullException.ThrowIfNull(client);
        _identifiers = identifiers ?? throw new ArgumentNullException(nameof(identifiers));
        _timeProvider = timeProvider ?? throw new ArgumentNullException(nameof(timeProvider));
        _sessionLogClient = client.SessionLog;
    }

    /// <inheritdoc />
    public SessionLogWorkflowContext? Context => _context;

    /// <inheritdoc />
    public async Task<SessionLogWorkflowContext> BootstrapAsync(
        SessionLogBootstrapRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var sessionId = ResolveSessionId(request);
            var now = GetUtcTimestamp();
            var context = new SessionLogWorkflowContext(sessionId, _identifiers.SourceType)
            {
                Title = request.Title,
                Model = request.Model,
                Status = request.Status,
                Workspace = CloneWorkspace(request.Workspace),
                Started = request.Started ?? now,
                LastUpdated = now,
            };

            _context = context;
            await SubmitContextAsync(context, cancellationToken).ConfigureAwait(false);
            return context;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SessionLogWorkflowContext> UpdateSessionAsync(
        SessionLogSessionUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var context = EnsureBootstrapped();
            if (request.Title is not null)
                context.Title = request.Title;
            if (request.Model is not null)
                context.Model = request.Model;
            if (request.Status is not null)
                context.Status = request.Status;
            if (request.Workspace is not null)
                context.Workspace = CloneWorkspace(request.Workspace);

            Touch(context);
            await SubmitContextAsync(context, cancellationToken).ConfigureAwait(false);
            return context;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SessionLogWorkflowContext> PersistAsync(CancellationToken cancellationToken = default)
    {
        await _mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var context = EnsureBootstrapped();
            Touch(context);
            await SubmitContextAsync(context, cancellationToken).ConfigureAwait(false);
            return context;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SessionLogTurnContext> BeginTurnAsync(
        SessionLogTurnCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await _mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var context = EnsureBootstrapped();
            var requestId = ResolveRequestId(request, context);
            if (context.FindTurn(requestId) is not null)
            {
                throw new InvalidOperationException(
                    $"Request turn '{requestId}' already exists in the current session-log workflow context.");
            }

            var turn = new SessionLogTurnContext(requestId, GetUtcTimestamp())
            {
                QueryText = request.QueryText,
                QueryTitle = request.QueryTitle,
                Interpretation = request.Interpretation,
                Response = request.Response,
                Status = request.Status,
                Model = request.Model ?? context.Model,
                TokenCount = request.TokenCount,
                ModelProvider = request.ModelProvider,
                FailureNote = request.FailureNote,
                Score = request.Score,
                IsPremium = request.IsPremium,
            };

            turn.ReplaceTags(request.Tags);
            turn.ReplaceContextList(request.ContextList);
            turn.ReplaceDesignDecisions(request.DesignDecisions);
            turn.ReplaceRequirementsDiscovered(request.RequirementsDiscovered);
            turn.ReplaceFilesModified(request.FilesModified);
            turn.ReplaceBlockers(request.Blockers);

            context.AddTurn(turn);
            Touch(context);
            await SubmitContextAsync(context, cancellationToken).ConfigureAwait(false);
            return turn;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SessionLogWorkflowContext> CreateTurnAsync(
        SessionLogTurnCreateRequest request,
        CancellationToken cancellationToken = default)
    {
        await BeginTurnAsync(request, cancellationToken).ConfigureAwait(false);
        return EnsureBootstrapped();
    }

    /// <inheritdoc />
    public async Task<SessionLogTurnContext> AppendDialogAsync(
        SessionLogDialogAppendRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequestId(request.RequestId, nameof(request));
        if (request.Items.Count == 0)
            throw new ArgumentException("At least one dialog item is required.", nameof(request));

        await _mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var context = EnsureBootstrapped();
            var turn = FindTurnOrThrow(context, request.RequestId);
            var items = request.Items.Select(CloneDialogItem).ToList();

            await _sessionLogClient.AppendDialogAsync(
                context.SourceType,
                context.SessionId,
                request.RequestId,
                items,
                cancellationToken).ConfigureAwait(false);

            turn.AppendProcessingDialog(items);
            Touch(context);
            return turn;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SessionLogTurnContext> AppendActionsAsync(
        SessionLogActionAppendRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequestId(request.RequestId, nameof(request));
        if (request.Actions.Count == 0)
            throw new ArgumentException("At least one action is required.", nameof(request));

        await _mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var context = EnsureBootstrapped();
            var turn = FindTurnOrThrow(context, request.RequestId);
            turn.AppendActions(request.Actions);
            Touch(context);
            await SubmitContextAsync(context, cancellationToken).ConfigureAwait(false);
            return turn;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SessionLogWorkflowContext> UpdateTurnAsync(
        SessionLogTurnUpdateRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ValidateRequestId(request.RequestId, nameof(request));

        await _mutationGate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            var context = EnsureBootstrapped();
            var turn = FindTurnOrThrow(context, request.RequestId);

            if (request.Response is not null)
                turn.Response = request.Response;
            if (request.Interpretation is not null)
                turn.Interpretation = request.Interpretation;
            if (request.Status is not null)
                turn.Status = request.Status;
            if (request.Model is not null)
                turn.Model = request.Model;
            if (request.TokenCount is not null)
                turn.TokenCount = request.TokenCount;
            if (request.ModelProvider is not null)
                turn.ModelProvider = request.ModelProvider;
            if (request.FailureNote is not null)
                turn.FailureNote = request.FailureNote;
            if (request.Score is not null)
                turn.Score = request.Score;
            if (request.IsPremium is not null)
                turn.IsPremium = request.IsPremium;
            if (request.Tags is not null)
                turn.ReplaceTags(request.Tags);
            if (request.ContextList is not null)
                turn.ReplaceContextList(request.ContextList);
            if (request.Actions is not null)
                turn.ReplaceActions(request.Actions);
            if (request.ProcessingDialog is not null)
                turn.ReplaceProcessingDialog(request.ProcessingDialog);
            if (request.FilesModified is not null)
                turn.ReplaceFilesModified(request.FilesModified);
            if (request.DesignDecisions is not null)
                turn.ReplaceDesignDecisions(request.DesignDecisions);
            if (request.RequirementsDiscovered is not null)
                turn.ReplaceRequirementsDiscovered(request.RequirementsDiscovered);
            if (request.Blockers is not null)
                turn.ReplaceBlockers(request.Blockers);

            Touch(context);
            await SubmitContextAsync(context, cancellationToken).ConfigureAwait(false);
            return context;
        }
        finally
        {
            _mutationGate.Release();
        }
    }

    /// <inheritdoc />
    public async Task<SessionLogTurnContext> CompleteTurnAsync(
        SessionLogTurnCompleteRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        await UpdateTurnAsync(
            new SessionLogTurnUpdateRequest
            {
                RequestId = request.RequestId,
                Response = request.Response,
                Interpretation = request.Interpretation,
                Status = "completed",
                Model = request.Model,
                TokenCount = request.TokenCount,
                ModelProvider = request.ModelProvider,
                Score = request.Score,
                IsPremium = request.IsPremium,
                Tags = request.Tags,
                ContextList = request.ContextList,
                FilesModified = request.FilesModified,
                DesignDecisions = request.DesignDecisions,
                RequirementsDiscovered = request.RequirementsDiscovered,
                Blockers = request.Blockers,
            },
            cancellationToken).ConfigureAwait(false);

        return FindTurnOrThrow(EnsureBootstrapped(), request.RequestId);
    }

    /// <inheritdoc />
    public async Task<SessionLogTurnContext> FailTurnAsync(
        SessionLogTurnFailureRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentException.ThrowIfNullOrWhiteSpace(request.FailureNote);

        await UpdateTurnAsync(
            new SessionLogTurnUpdateRequest
            {
                RequestId = request.RequestId,
                Response = request.Response,
                Interpretation = request.Interpretation,
                Status = "failed",
                Model = request.Model,
                TokenCount = request.TokenCount,
                ModelProvider = request.ModelProvider,
                FailureNote = request.FailureNote,
                Score = request.Score,
                IsPremium = request.IsPremium,
                Tags = request.Tags,
                ContextList = request.ContextList,
                FilesModified = request.FilesModified,
                DesignDecisions = request.DesignDecisions,
                RequirementsDiscovered = request.RequirementsDiscovered,
                Blockers = request.Blockers,
            },
            cancellationToken).ConfigureAwait(false);

        return FindTurnOrThrow(EnsureBootstrapped(), request.RequestId);
    }

    private static WorkspaceInfoDto? CloneWorkspace(WorkspaceInfoDto? workspace)
        => workspace is null
            ? null
            : new WorkspaceInfoDto
            {
                Project = workspace.Project,
                TargetFramework = workspace.TargetFramework,
                Repository = workspace.Repository,
                Branch = workspace.Branch,
            };

    private static ProcessingDialogItemDto CloneDialogItem(ProcessingDialogItemDto item) => new()
    {
        Timestamp = item.Timestamp,
        Role = item.Role,
        Content = item.Content,
        Category = item.Category,
    };

    private SessionLogTurnContext FindTurnOrThrow(SessionLogWorkflowContext context, string requestId) =>
        context.FindTurn(requestId)
        ?? throw new InvalidOperationException(
            $"Request turn '{requestId}' was not found in the current session-log workflow context.");

    private string GetUtcTimestamp() => _timeProvider.GetUtcNow().ToString("o");

    private string ResolveSessionId(SessionLogBootstrapRequest request)
    {
        if (!string.IsNullOrWhiteSpace(request.SessionId))
        {
            if (!_identifiers.TryValidateSessionId(request.SessionId, out var error))
                throw new ArgumentException(error, nameof(request));

            return request.SessionId;
        }

        var suffixSeed = request.SessionIdSuffix;
        if (string.IsNullOrWhiteSpace(suffixSeed))
            suffixSeed = request.Model;
        if (string.IsNullOrWhiteSpace(suffixSeed))
            suffixSeed = request.Title;
        if (string.IsNullOrWhiteSpace(suffixSeed))
            suffixSeed = "session";

        return _identifiers.CreateSessionId(suffixSeed);
    }

    private string ResolveRequestId(SessionLogTurnCreateRequest request, SessionLogWorkflowContext context)
    {
        if (!string.IsNullOrWhiteSpace(request.RequestId))
        {
            ValidateRequestId(request.RequestId, nameof(request));
            return request.RequestId;
        }

        var suffixSeed = request.RequestIdSuffix;
        if (string.IsNullOrWhiteSpace(suffixSeed))
            suffixSeed = request.QueryTitle;
        if (string.IsNullOrWhiteSpace(suffixSeed))
            suffixSeed = request.QueryText;
        if (string.IsNullOrWhiteSpace(suffixSeed))
            suffixSeed = "turn";

        var requestId = _identifiers.CreateRequestId(suffixSeed);
        if (context.FindTurn(requestId) is null)
            return requestId;

        return _identifiers.CreateRequestId($"{suffixSeed}-{context.TurnCount + 1:D3}");
    }

    private void Touch(SessionLogWorkflowContext context) =>
        context.LastUpdated = GetUtcTimestamp();

    private Task SubmitContextAsync(SessionLogWorkflowContext context, CancellationToken cancellationToken) =>
        _sessionLogClient.SubmitAsync(context.ToSubmitDto(), cancellationToken);

    private void ValidateRequestId(string requestId, string paramName)
    {
        if (!_identifiers.TryValidateRequestId(requestId, out var error))
            throw new ArgumentException(error, paramName);
    }

    private SessionLogWorkflowContext EnsureBootstrapped() =>
        _context ?? throw new InvalidOperationException(
            "The session-log workflow has not been bootstrapped. Call BootstrapAsync before performing session or turn operations.");
}
