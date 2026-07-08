using System.Collections.Concurrent;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using McpServer.Common.AgentCli;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services.AgentHelp;

/// <summary>
/// FR-MCP-HELP-001: In-memory Agent Help conversation orchestration service.
/// TR-MCP-HELP-007: Session registry with inbound guard evaluation and helper execution.
/// </summary>
public sealed class AgentHelpConversationService : IAgentHelpConversationService
{
    private readonly ConcurrentDictionary<string, AgentHelpSessionState> _sessions = new(StringComparer.OrdinalIgnoreCase);
    private readonly IAgentExecutionStrategyResolver? _strategyResolver;
    private readonly AgentHelpInboundGuard _inboundGuard;
    private readonly HelpTranscriptWriter _transcriptWriter;
    private readonly AgentHelpIncidentLogger _incidentLogger;
    private readonly AgentHelpCorpusService _corpusService;
    private readonly WorkspaceServiceAccessor _workspaceAccessor;
    private readonly IOptionsMonitor<AgentHelpOptions> _options;
    private readonly ILogger<AgentHelpConversationService> _logger;

    /// <summary>
    /// TR-MCP-HELP-007: Creates a new conversation service.
    /// </summary>
    public AgentHelpConversationService(
        AgentHelpInboundGuard inboundGuard,
        HelpTranscriptWriter transcriptWriter,
        AgentHelpIncidentLogger incidentLogger,
        AgentHelpCorpusService corpusService,
        WorkspaceServiceAccessor workspaceAccessor,
        IOptionsMonitor<AgentHelpOptions> options,
        ILogger<AgentHelpConversationService> logger,
        IServiceProvider? serviceProvider = null)
    {
        _inboundGuard = inboundGuard ?? throw new ArgumentNullException(nameof(inboundGuard));
        _transcriptWriter = transcriptWriter ?? throw new ArgumentNullException(nameof(transcriptWriter));
        _incidentLogger = incidentLogger ?? throw new ArgumentNullException(nameof(incidentLogger));
        _corpusService = corpusService ?? throw new ArgumentNullException(nameof(corpusService));
        _workspaceAccessor = workspaceAccessor ?? throw new ArgumentNullException(nameof(workspaceAccessor));
        _options = options ?? throw new ArgumentNullException(nameof(options));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _strategyResolver = serviceProvider?.GetService<IAgentExecutionStrategyResolver>();
    }

    /// <inheritdoc />
    public async Task<AgentHelpSessionCreateResponse> CreateSessionAsync(
        AgentHelpSessionCreateRequest? request,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        EnsureEnabled();

        var opts = _options.CurrentValue;
        var now = DateTimeOffset.UtcNow;
        var requestedModel = string.IsNullOrWhiteSpace(request?.AgentModel) ? opts.HelperModel : request.AgentModel.Trim();
        var configuredExecutionStrategy = string.IsNullOrWhiteSpace(request?.ExecutionStrategy)
            ? opts.DefaultExecutionStrategy
            : request.ExecutionStrategy;
        var executionStrategy = ResolveExecutionStrategyName(configuredExecutionStrategy);
        var workspacePath = ResolveWorkspacePath(request?.WorkspacePath);

        if (!string.IsNullOrWhiteSpace(request?.DeviceId))
        {
            foreach (var kvp in _sessions)
            {
                if (string.Equals(kvp.Value.DeviceId, request.DeviceId, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation(
                        "Closing existing Agent Help session {OldSessionId} for device {DeviceId}",
                        kvp.Key,
                        request.DeviceId);
                    await DeleteSessionAsync(kvp.Key, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        var sessionId = $"help-{now:yyyyMMddHHmmss}-{Guid.NewGuid():N}".ToLowerInvariant();
        var state = new AgentHelpSessionState(
            sessionId,
            request?.DeviceId,
            request?.ClientName,
            workspacePath,
            request?.AgentName,
            request?.AgentPath,
            requestedModel,
            request?.AgentSeed,
            request?.AgentParameters,
            executionStrategy,
            request?.TodoId,
            request?.Topic,
            now);
        _sessions[sessionId] = state;

        AgentHelpCorpusSummary? corpusSummary = null;
        if (opts.CorpusBootstrapEnabled)
        {
            corpusSummary = await _corpusService.BootstrapAsync(workspacePath, request?.Topic, cancellationToken)
                .ConfigureAwait(false);
            state.CorpusSummary = corpusSummary;

            await AppendTranscriptAsync(
                state,
                new AgentHelpTranscriptEntry
                {
                    TimestampUtc = now.ToString("O"),
                    SessionId = sessionId,
                    Role = "system",
                    Category = "corpus",
                    Text = corpusSummary.Summary,
                },
                cancellationToken).ConfigureAwait(false);
        }

        _logger.LogInformation("Created Agent Help session {SessionId} for workspace {WorkspacePath}", sessionId, workspacePath);

        return new AgentHelpSessionCreateResponse
        {
            SessionId = sessionId,
            Status = "idle",
            ModelRequested = requestedModel,
            ModelResolved = requestedModel,
            ExecutionStrategy = executionStrategy,
            CorpusSummary = corpusSummary,
        };
    }

    /// <inheritdoc />
    public async Task<AgentHelpTurnResponse?> SubmitTurnAsync(
        string sessionId,
        AgentHelpTurnRequest request,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(request);

        if (!_sessions.TryGetValue(sessionId, out var state))
            return null;

        await state.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (state.Terminated)
            {
                return new AgentHelpTurnResponse
                {
                    SessionId = sessionId,
                    TurnId = state.LastTurnId ?? "turn-0000",
                    Status = "terminated_guardrail",
                    Error = "Session was terminated due to a guardrail violation.",
                    LatencyMs = 0,
                };
            }

            if (state.IsTurnActive)
            {
                return new AgentHelpTurnResponse
                {
                    SessionId = sessionId,
                    TurnId = state.LastTurnId ?? "turn-0000",
                    Status = "busy",
                    AssistantDisplayText = "Another help turn is already running for this session.",
                    Error = "Session already has an active turn.",
                    LatencyMs = 0,
                };
            }

            state.IsTurnActive = true;
            state.Status = "thinking";
            state.LastError = null;
            state.LastUpdatedUtc = DateTimeOffset.UtcNow;
            state.TurnCounter++;
            var turnId = $"turn-{state.TurnCounter.ToString("0000", CultureInfo.InvariantCulture)}";
            state.LastTurnId = turnId;

            var sw = Stopwatch.StartNew();
            var result = await ExecuteTurnCoreAsync(state, turnId, request.UserMessage, cancellationToken)
                .ConfigureAwait(false);
            sw.Stop();

            state.IsTurnActive = false;
            state.Status = result.Status is "completed" ? "idle" : result.Status;
            state.LastError = result.Error;
            state.LastUpdatedUtc = DateTimeOffset.UtcNow;

            return result with
            {
                LatencyMs = (int)Math.Clamp(sw.ElapsedMilliseconds, 0, int.MaxValue),
            };
        }
        finally
        {
            state.Gate.Release();
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AgentHelpStreamEvent> SubmitTurnStreamingAsync(
        string sessionId,
        AgentHelpTurnRequest request,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);
        ArgumentNullException.ThrowIfNull(request);

        if (!_sessions.TryGetValue(sessionId, out var state))
        {
            yield return new AgentHelpStreamEvent { Type = "error", Message = $"Agent Help session '{sessionId}' not found." };
            yield break;
        }

        await state.Gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        string turnId;
        try
        {
            if (state.Terminated)
            {
                yield return new AgentHelpStreamEvent
                {
                    Type = "error",
                    Message = "Session was terminated due to a guardrail violation.",
                    Status = "terminated_guardrail",
                };
                yield break;
            }

            if (state.IsTurnActive)
            {
                yield return new AgentHelpStreamEvent { Type = "error", Message = "A turn is already in progress." };
                yield break;
            }

            state.IsTurnActive = true;
            state.Status = "thinking";
            state.LastError = null;
            state.LastUpdatedUtc = DateTimeOffset.UtcNow;
            state.TurnCounter++;
            turnId = $"turn-{state.TurnCounter.ToString("0000", CultureInfo.InvariantCulture)}";
            state.LastTurnId = turnId;
        }
        finally
        {
            state.Gate.Release();
        }

        var sw = Stopwatch.StartNew();
        AgentHelpTurnResponse? result = null;
        try
        {
            result = await ExecuteTurnCoreAsync(state, turnId, request.UserMessage, cancellationToken)
                .ConfigureAwait(false);

            if (result.Status == "terminated_guardrail")
            {
                yield return new AgentHelpStreamEvent
                {
                    Type = "session_terminated",
                    TurnId = turnId,
                    IncidentId = result.IncidentId,
                    Message = result.Error,
                    Status = "terminated_guardrail",
                    GuardResult = result.GuardResult,
                    LatencyMs = (int)Math.Clamp(sw.ElapsedMilliseconds, 0, int.MaxValue),
                };
                yield break;
            }
            else if (!string.IsNullOrWhiteSpace(result.AssistantDisplayText))
            {
                foreach (var chunk in ChunkText(result.AssistantDisplayText))
                {
                    yield return new AgentHelpStreamEvent { Type = "chunk", TurnId = turnId, Text = chunk };
                }
            }

            yield return new AgentHelpStreamEvent
            {
                Type = result.Status == "error" ? "error" : "done",
                TurnId = turnId,
                Status = result.Status,
                Message = result.Error,
                LatencyMs = (int)Math.Clamp(sw.ElapsedMilliseconds, 0, int.MaxValue),
                GuardResult = result.GuardResult,
            };
        }
        finally
        {
            await state.Gate.WaitAsync(CancellationToken.None).ConfigureAwait(false);
            try
            {
                state.IsTurnActive = false;
                state.Status = result?.Status is "completed"
                    ? "idle"
                    : result?.Status ?? "error";
                state.LastError = result?.Error;
                state.LastUpdatedUtc = DateTimeOffset.UtcNow;
            }
            finally
            {
                state.Gate.Release();
            }
        }
    }

    /// <inheritdoc />
    public Task<AgentHelpSessionStatusDto?> GetStatusAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_sessions.TryGetValue(sessionId, out var state))
            return Task.FromResult<AgentHelpSessionStatusDto?>(null);

        lock (state.SyncRoot)
        {
            return Task.FromResult<AgentHelpSessionStatusDto?>(new AgentHelpSessionStatusDto
            {
                SessionId = state.SessionId,
                Status = state.Status,
                CreatedUtc = state.CreatedUtc.ToString("O"),
                LastUpdatedUtc = state.LastUpdatedUtc.ToString("O"),
                IsTurnActive = state.IsTurnActive,
                LastError = state.LastError,
                LastTurnId = state.LastTurnId,
                TurnCounter = state.TurnCounter,
                ExecutionStrategy = state.ExecutionStrategy,
                TodoId = state.TodoId,
                Topic = state.Topic,
                Terminated = state.Terminated,
            });
        }
    }

    /// <inheritdoc />
    public Task<AgentHelpTranscriptResponse?> GetTranscriptAsync(
        string sessionId,
        CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_sessions.TryGetValue(sessionId, out var state))
            return Task.FromResult<AgentHelpTranscriptResponse?>(null);

        lock (state.SyncRoot)
        {
            return Task.FromResult<AgentHelpTranscriptResponse?>(new AgentHelpTranscriptResponse
            {
                SessionId = sessionId,
                Items = state.Transcript.ToList(),
            });
        }
    }

    /// <inheritdoc />
    public Task<bool> DeleteSessionAsync(string sessionId, CancellationToken cancellationToken = default)
    {
        EnsureEnabled();
        cancellationToken.ThrowIfCancellationRequested();
        ArgumentException.ThrowIfNullOrWhiteSpace(sessionId);

        if (!_sessions.TryRemove(sessionId, out var state))
            return Task.FromResult(false);

        state.Gate.Dispose();
        return Task.FromResult(true);
    }

    private async Task<AgentHelpTurnResponse> ExecuteTurnCoreAsync(
        AgentHelpSessionState state,
        string turnId,
        string userMessage,
        CancellationToken cancellationToken)
    {
        var trimmed = (userMessage ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(trimmed))
        {
            return new AgentHelpTurnResponse
            {
                SessionId = state.SessionId,
                TurnId = turnId,
                Status = "error",
                Error = "UserMessage is required.",
                LatencyMs = 0,
            };
        }

        await AppendTranscriptAsync(
            state,
            new AgentHelpTranscriptEntry
            {
                TimestampUtc = DateTimeOffset.UtcNow.ToString("O"),
                SessionId = state.SessionId,
                TurnId = turnId,
                Role = "user",
                Category = "transcript",
                Text = trimmed,
            },
            cancellationToken).ConfigureAwait(false);

        AgentHelpGuardResult? guardResult = null;
        if (_options.CurrentValue.GuardEnabled)
        {
            guardResult = _inboundGuard.Evaluate(trimmed);
            if (!guardResult.Allowed)
            {
                var incidentId = await RecordBlockedTurnAsync(state, turnId, guardResult, cancellationToken)
                    .ConfigureAwait(false);
                return new AgentHelpTurnResponse
                {
                    SessionId = state.SessionId,
                    TurnId = turnId,
                    Status = "terminated_guardrail",
                    AssistantDisplayText = guardResult.Reason,
                    Error = guardResult.Reason,
                    GuardResult = guardResult,
                    IncidentId = incidentId,
                    LatencyMs = 0,
                };
            }
        }

        var assistantText = await ExecuteHelperAsync(state, trimmed, cancellationToken).ConfigureAwait(false);

        await AppendTranscriptAsync(
            state,
            new AgentHelpTranscriptEntry
            {
                TimestampUtc = DateTimeOffset.UtcNow.ToString("O"),
                SessionId = state.SessionId,
                TurnId = turnId,
                Role = "assistant",
                Category = "transcript",
                Text = assistantText,
            },
            cancellationToken).ConfigureAwait(false);

        return new AgentHelpTurnResponse
        {
            SessionId = state.SessionId,
            TurnId = turnId,
            Status = "completed",
            AssistantDisplayText = assistantText,
            GuardResult = guardResult,
            LatencyMs = 0,
        };
    }

    private async Task<string> RecordBlockedTurnAsync(
        AgentHelpSessionState state,
        string turnId,
        AgentHelpGuardResult guardResult,
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var incident = new AgentHelpIncidentRecord
        {
            IncidentId = Guid.NewGuid().ToString("N"),
            SessionId = state.SessionId,
            TurnId = turnId,
            RuleId = guardResult.RuleId ?? "unknown",
            Reason = guardResult.Reason ?? "Blocked by inbound guard.",
            MatchedSnippet = guardResult.MatchedSnippet,
            TimestampUtc = now.ToString("O"),
            WorkspacePath = state.WorkspacePath,
        };

        await _incidentLogger.WriteAsync(ResolveWorkspaceDataRoot(state.WorkspacePath), incident, cancellationToken)
            .ConfigureAwait(false);

        await AppendTranscriptAsync(
            state,
            new AgentHelpTranscriptEntry
            {
                TimestampUtc = now.ToString("O"),
                SessionId = state.SessionId,
                TurnId = turnId,
                Role = "guard",
                Category = "guardrail_violation",
                Text = guardResult.Reason ?? "Blocked by inbound guard.",
                GuardRuleId = guardResult.RuleId,
            },
            cancellationToken).ConfigureAwait(false);

        state.Status = "terminated_guardrail";
        state.Terminated = true;
        state.LastError = guardResult.Reason;
        state.LastUpdatedUtc = now;

        return incident.IncidentId;
    }

    private async Task<string> ExecuteHelperAsync(
        AgentHelpSessionState state,
        string userMessage,
        CancellationToken cancellationToken)
    {
        if (_strategyResolver is not null)
        {
            try
            {
                var strategy = _strategyResolver.Resolve(state.ExecutionStrategy);
                var options = BuildAgentCliOptions(state);
                var prompt = BuildHelperPrompt(state, userMessage);
                await using var session = await strategy.CreateSessionAsync(
                    new AgentExecutionSessionRequest(
                        prompt,
                        state.WorkspacePath,
                        state.AgentName,
                        state.ExecutionStrategy,
                        options),
                    cancellationToken).ConfigureAwait(false);

                var result = await session.ReadInitialResponseAsync(cancellationToken).ConfigureAwait(false);
                if (result.State == AgentCliResultState.Success && !string.IsNullOrWhiteSpace(result.Body))
                    return result.Body.Trim();
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                _logger.LogDebug(ex, "Agent Help execution strategy unavailable; falling back to echo helper.");
            }
        }

        if (_options.CurrentValue.UseEchoHelperFallback)
            return BuildEchoHelperResponse(state, userMessage);

        throw new InvalidOperationException("No Agent Help execution strategy is available and echo fallback is disabled.");
    }

    private static string BuildHelperPrompt(AgentHelpSessionState state, string userMessage)
    {
        if (string.IsNullOrWhiteSpace(state.AgentSeed))
            return userMessage;

        return $"{state.AgentSeed.Trim()}{Environment.NewLine}{Environment.NewLine}{userMessage}";
    }

    private static string BuildEchoHelperResponse(AgentHelpSessionState state, string userMessage)
    {
        var topic = string.IsNullOrWhiteSpace(state.Topic) ? "general assistance" : state.Topic.Trim();
        return $"Agent Help echo: I received your request about '{topic}'. You said: {userMessage}";
    }

    private AgentCliClientOptions BuildAgentCliOptions(AgentHelpSessionState state)
    {
        var opts = _options.CurrentValue;
        var options = new AgentCliClientOptions
        {
            AgentPath = string.IsNullOrWhiteSpace(state.AgentPath)
                ? new AgentCliClientOptions().AgentPath
                : state.AgentPath,
            Model = string.IsNullOrWhiteSpace(state.AgentModel) ? opts.HelperModel : state.AgentModel,
            Silent = true,
            Timeout = Timeout.InfiniteTimeSpan,
            WorkingDirectory = state.WorkspacePath,
        };

        if (!string.IsNullOrWhiteSpace(opts.ModelApiKey)
            && !string.IsNullOrWhiteSpace(opts.ModelApiKeyEnvironmentVariableName))
        {
            options.EnvironmentVariables[opts.ModelApiKeyEnvironmentVariableName.Trim()] = opts.ModelApiKey.Trim();
        }

        foreach (var pair in state.AgentParameters)
            options.EnvironmentVariables[pair.Key] = pair.Value;

        return options;
    }

    private async Task AppendTranscriptAsync(
        AgentHelpSessionState state,
        AgentHelpTranscriptEntry entry,
        CancellationToken cancellationToken)
    {
        lock (state.SyncRoot)
        {
            state.Transcript.Add(entry);
            state.LastUpdatedUtc = DateTimeOffset.UtcNow;
        }

        await _transcriptWriter.AppendAsync(ResolveWorkspaceDataRoot(state.WorkspacePath), entry, cancellationToken)
            .ConfigureAwait(false);
    }

    private string ResolveWorkspacePath(string? requestedWorkspacePath)
    {
        if (!string.IsNullOrWhiteSpace(requestedWorkspacePath))
            return requestedWorkspacePath.Trim();

        var accessorPath = _workspaceAccessor.GetWorkspacePath();
        if (!string.IsNullOrWhiteSpace(accessorPath))
            return accessorPath;

        var opts = _options.CurrentValue;
        if (!string.IsNullOrWhiteSpace(opts.WorkingDirectory))
            return opts.WorkingDirectory.Trim();

        return Directory.GetCurrentDirectory();
    }

    private static string ResolveWorkspaceDataRoot(string workspacePath)
        => Path.Combine(workspacePath, ".mcpServer");

    private string ResolveExecutionStrategyName(string? configuredExecutionStrategy)
    {
        if (_strategyResolver is null)
            return AgentExecutionStrategyNames.NormalizeOrDefault(configuredExecutionStrategy);

        return _strategyResolver.Resolve(configuredExecutionStrategy).Name;
    }

    private void EnsureEnabled()
    {
        if (!_options.CurrentValue.Enabled)
            throw new InvalidOperationException("Agent Help endpoints are disabled.");
    }

    private static IEnumerable<string> ChunkText(string text)
    {
        const int chunkSize = 80;
        for (var index = 0; index < text.Length; index += chunkSize)
        {
            var length = Math.Min(chunkSize, text.Length - index);
            yield return text.Substring(index, length);
        }
    }

    private sealed class AgentHelpSessionState
    {
        public AgentHelpSessionState(
            string sessionId,
            string? deviceId,
            string? clientName,
            string workspacePath,
            string? agentName,
            string? agentPath,
            string? agentModel,
            string? agentSeed,
            Dictionary<string, string>? agentParameters,
            string executionStrategy,
            string? todoId,
            string? topic,
            DateTimeOffset now)
        {
            SessionId = sessionId;
            DeviceId = deviceId;
            ClientName = clientName;
            WorkspacePath = workspacePath;
            AgentName = agentName;
            AgentPath = agentPath;
            AgentModel = agentModel;
            AgentSeed = agentSeed;
            AgentParameters = agentParameters is null
                ? []
                : new Dictionary<string, string>(agentParameters, StringComparer.OrdinalIgnoreCase);
            ExecutionStrategy = executionStrategy;
            TodoId = todoId;
            Topic = topic;
            CreatedUtc = now;
            LastUpdatedUtc = now;
        }

        public object SyncRoot { get; } = new();
        public SemaphoreSlim Gate { get; } = new(1, 1);
        public string SessionId { get; }
        public string? DeviceId { get; }
        public string? ClientName { get; }
        public string WorkspacePath { get; }
        public string? AgentName { get; }
        public string? AgentPath { get; }
        public string? AgentModel { get; }
        public string? AgentSeed { get; }
        public Dictionary<string, string> AgentParameters { get; }
        public string ExecutionStrategy { get; }
        public string? TodoId { get; }
        public string? Topic { get; }
        public AgentHelpCorpusSummary? CorpusSummary { get; set; }
        public DateTimeOffset CreatedUtc { get; set; }
        public DateTimeOffset LastUpdatedUtc { get; set; }
        public bool IsTurnActive { get; set; }
        public string Status { get; set; } = "idle";
        public bool Terminated { get; set; }
        public string? LastError { get; set; }
        public string? LastTurnId { get; set; }
        public int TurnCounter { get; set; }
        public List<AgentHelpTranscriptEntry> Transcript { get; } = [];
    }
}