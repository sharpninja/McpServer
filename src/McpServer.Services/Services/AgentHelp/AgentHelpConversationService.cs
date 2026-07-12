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
            request?.CallerAgent,
            request?.CallerSessionId,
            request?.CallerRequestId,
            request?.IssueSummary,
            now);
        _sessions[sessionId] = state;

        AgentHelpCorpusSummary? corpusSummary = null;
        if (opts.CorpusBootstrapEnabled)
        {
            var bootstrap = await _corpusService.BootstrapAsync(
                    workspacePath,
                    request?.Topic,
                    request?.IssueSummary,
                    request?.TodoId,
                    cancellationToken)
                .ConfigureAwait(false);
            corpusSummary = bootstrap.ToSummary();
            state.CorpusSummary = corpusSummary;
            state.PromptContext = new AgentHelpPromptContext
            {
                WorkspacePath = workspacePath,
                Topic = request?.Topic,
                TodoId = request?.TodoId,
                CallerAgent = request?.CallerAgent,
                CallerSessionId = request?.CallerSessionId,
                CallerRequestId = request?.CallerRequestId,
                IssueSummary = request?.IssueSummary,
                CustomSeed = request?.AgentSeed,
                ContextPackText = bootstrap.ContextPackText,
                SourceKeys = bootstrap.SourceKeys,
            };

            await AppendTranscriptAsync(
                state,
                new AgentHelpTranscriptEntry
                {
                    TimestampUtc = now.ToString("O"),
                    SessionId = sessionId,
                    Role = "system",
                    Category = "corpus",
                    Text = $"{corpusSummary.Summary} sources=[{string.Join(", ", corpusSummary.SourceKeys)}]",
                },
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            state.PromptContext = new AgentHelpPromptContext
            {
                WorkspacePath = workspacePath,
                Topic = request?.Topic,
                TodoId = request?.TodoId,
                CallerAgent = request?.CallerAgent,
                CallerSessionId = request?.CallerSessionId,
                CallerRequestId = request?.CallerRequestId,
                IssueSummary = request?.IssueSummary,
                CustomSeed = request?.AgentSeed,
            };
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
        var turnId = state.LastTurnId ?? "turn-0000";
        var sw = Stopwatch.StartNew();
        var startedTurn = false;
        AgentHelpTurnResponse? result = null;
        try
        {
            if (state.Terminated)
            {
                result = new AgentHelpTurnResponse
                {
                    SessionId = sessionId,
                    TurnId = turnId,
                    Status = "terminated_guardrail",
                    Error = "Session was terminated due to a guardrail violation.",
                    LatencyMs = 0,
                };
            }
            else if (state.IsTurnActive)
            {
                result = new AgentHelpTurnResponse
                {
                    SessionId = sessionId,
                    TurnId = turnId,
                    Status = "busy",
                    AssistantDisplayText = "Another help turn is already running for this session.",
                    Error = "Session already has an active turn.",
                    LatencyMs = 0,
                };
            }
            else
            {
                state.IsTurnActive = true;
                startedTurn = true;
                state.Status = "thinking";
                state.LastError = null;
                state.LastUpdatedUtc = DateTimeOffset.UtcNow;
                state.TurnCounter++;
                turnId = $"turn-{state.TurnCounter.ToString("0000", CultureInfo.InvariantCulture)}";
                state.LastTurnId = turnId;

                result = await ExecuteTurnCoreAsync(state, turnId, request.UserMessage, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            const string error = "Agent Help turn was cancelled or timed out.";
            result = new AgentHelpTurnResponse
            {
                SessionId = sessionId,
                TurnId = turnId,
                Status = "error",
                AssistantDisplayText = error,
                Error = error,
                LatencyMs = 0,
            };

            if (startedTurn)
                await AppendAssistantErrorTranscriptAsync(state, turnId, error, CancellationToken.None).ConfigureAwait(false);
        }
        finally
        {
            sw.Stop();
            if (result is not null)
            {
                result = result with
                {
                    LatencyMs = (int)Math.Clamp(sw.ElapsedMilliseconds, 0, int.MaxValue),
                };
            }

            if (startedTurn)
            {
                state.IsTurnActive = false;
                state.Status = result?.Status is "completed" ? "idle" : result?.Status ?? "error";
                state.LastError = result?.Error;
                state.LastUpdatedUtc = DateTimeOffset.UtcNow;
            }

            state.Gate.Release();
        }

        return result;
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
                Type = result.Status == "completed" ? "done" : "error",
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

        var helperResult = await ExecuteHelperAsync(state, trimmed, cancellationToken).ConfigureAwait(false);

        if (!string.IsNullOrWhiteSpace(helperResult.ProgressText))
        {
            await AppendTranscriptAsync(
                state,
                new AgentHelpTranscriptEntry
                {
                    TimestampUtc = DateTimeOffset.UtcNow.ToString("O"),
                    SessionId = state.SessionId,
                    TurnId = turnId,
                    Role = "assistant",
                    Category = "progress",
                    Text = helperResult.ProgressText,
                },
                cancellationToken).ConfigureAwait(false);
        }

        if (!string.Equals(helperResult.Status, "completed", StringComparison.Ordinal))
        {
            if (!string.IsNullOrWhiteSpace(helperResult.Error))
                await AppendAssistantErrorTranscriptAsync(state, turnId, helperResult.Error, cancellationToken).ConfigureAwait(false);

            return new AgentHelpTurnResponse
            {
                SessionId = state.SessionId,
                TurnId = turnId,
                Status = helperResult.Status,
                Error = helperResult.Error,
                GuardResult = guardResult,
                LatencyMs = 0,
            };
        }

        var assistantText = helperResult.AssistantText ?? string.Empty;
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

    private async Task AppendAssistantErrorTranscriptAsync(
        AgentHelpSessionState state,
        string turnId,
        string error,
        CancellationToken cancellationToken)
    {
        await AppendTranscriptAsync(
            state,
            new AgentHelpTranscriptEntry
            {
                TimestampUtc = DateTimeOffset.UtcNow.ToString("O"),
                SessionId = state.SessionId,
                TurnId = turnId,
                Role = "assistant",
                Category = "error",
                Text = error,
            },
            cancellationToken).ConfigureAwait(false);
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

    private async Task<AgentHelpHelperResult> ExecuteHelperAsync(
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
                {
                    var finalAnswer = TryExtractFinalAnswer(result.Body);
                    if (finalAnswer is not null)
                        return AgentHelpHelperResult.Completed(finalAnswer.Text, finalAnswer.ProgressText);

                    var trimmedBody = result.Body.Trim();
                    if (ContainsFinalAnswerMarker(result.Body))
                    {
                        return AgentHelpHelperResult.Incomplete(
                            $"Agent Help helper produced '{AgentHelpPromptBuilder.FinalAnswerMarker}' without a final answer body.",
                            trimmedBody);
                    }

                    if (LooksLikeProgressOnlyOutput(trimmedBody))
                    {
                        return AgentHelpHelperResult.Incomplete(
                            $"Agent Help helper did not produce a final answer. Expected direct answer text or output containing '{AgentHelpPromptBuilder.FinalAnswerMarker}'.",
                            trimmedBody);
                    }

                    return AgentHelpHelperResult.Completed(trimmedBody);
                }

                if (result.State != AgentCliResultState.Success && !_options.CurrentValue.UseEchoHelperFallback)
                {
                    var error = string.IsNullOrWhiteSpace(result.Stderr)
                        ? "Agent Help helper failed without stderr."
                        : result.Stderr.Trim();
                    return AgentHelpHelperResult.Failed(error);
                }
            }
            catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
            {
                _logger.LogDebug(ex, "Agent Help execution strategy unavailable; falling back to echo helper.");
            }
        }

        if (_options.CurrentValue.UseEchoHelperFallback)
            return AgentHelpHelperResult.Completed(BuildEchoHelperResponse(state, userMessage));

        return AgentHelpHelperResult.Failed("No Agent Help execution strategy is available and echo fallback is disabled.");
    }

    private static AgentHelpFinalAnswer? TryExtractFinalAnswer(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
            return null;

        var markerIndex = rawOutput.IndexOf(
            AgentHelpPromptBuilder.FinalAnswerMarker,
            StringComparison.OrdinalIgnoreCase);
        if (markerIndex < 0)
            return null;

        var progressText = rawOutput[..markerIndex].Trim();
        var finalAnswer = rawOutput[(markerIndex + AgentHelpPromptBuilder.FinalAnswerMarker.Length)..].Trim();
        return string.IsNullOrWhiteSpace(finalAnswer)
            ? null
            : new AgentHelpFinalAnswer(
                finalAnswer,
                string.IsNullOrWhiteSpace(progressText) ? null : progressText);
    }

    private static bool ContainsFinalAnswerMarker(string rawOutput) =>
        !string.IsNullOrWhiteSpace(rawOutput)
        && rawOutput.IndexOf(AgentHelpPromptBuilder.FinalAnswerMarker, StringComparison.OrdinalIgnoreCase) >= 0;

    private static bool LooksLikeProgressOnlyOutput(string rawOutput)
    {
        if (string.IsNullOrWhiteSpace(rawOutput))
            return true;

        var normalized = string.Join(
            ' ',
            rawOutput.Trim().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries));
        var lower = normalized.ToLowerInvariant();
        return lower.StartsWith("i'll ", StringComparison.Ordinal)
            || lower.StartsWith("i will ", StringComparison.Ordinal)
            || lower.StartsWith("i’m ", StringComparison.Ordinal)
            || lower.StartsWith("i'm ", StringComparison.Ordinal)
            || lower.StartsWith("first i'll ", StringComparison.Ordinal)
            || lower.StartsWith("first i will ", StringComparison.Ordinal)
            || lower.StartsWith("let me ", StringComparison.Ordinal)
            || lower.StartsWith("following workspace bootstrap", StringComparison.Ordinal)
            || lower.StartsWith("bootstrapping ", StringComparison.Ordinal)
            || lower.StartsWith("bootstrap ", StringComparison.Ordinal)
            || lower.StartsWith("plan:", StringComparison.Ordinal)
            || lower.StartsWith("plan -", StringComparison.Ordinal)
            || lower.StartsWith("here is the plan", StringComparison.Ordinal)
            || lower.StartsWith("here's the plan", StringComparison.Ordinal)
            || lower.StartsWith("i need to ", StringComparison.Ordinal)
            || lower.StartsWith("i can help by ", StringComparison.Ordinal)
            || lower.Contains(" then answering from the evidence", StringComparison.Ordinal)
            || lower.Contains("bootstrap mcp health", StringComparison.Ordinal);
    }


    private static string BuildHelperPrompt(AgentHelpSessionState state, string userMessage)
    {
        var context = state.PromptContext ?? new AgentHelpPromptContext
        {
            WorkspacePath = state.WorkspacePath,
            Topic = state.Topic,
            TodoId = state.TodoId,
            CustomSeed = state.AgentSeed,
        };

        return AgentHelpPromptBuilder.BuildTurnPrompt(context, userMessage);
    }

    private static string BuildEchoHelperResponse(AgentHelpSessionState state, string userMessage)
    {
        var context = state.PromptContext ?? new AgentHelpPromptContext
        {
            WorkspacePath = state.WorkspacePath,
            Topic = state.Topic,
            TodoId = state.TodoId,
            CustomSeed = state.AgentSeed,
        };

        return AgentHelpPromptBuilder.SynthesizeEchoResponse(context, userMessage);
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
            Timeout = opts.HelperTimeout,
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

    private sealed record AgentHelpHelperResult(
        string Status,
        string? AssistantText,
        string? Error,
        string? ProgressText)
    {
        public static AgentHelpHelperResult Completed(string assistantText, string? progressText = null)
            => new("completed", assistantText, null, progressText);

        public static AgentHelpHelperResult Incomplete(string error, string? progressText = null)
            => new("incomplete", null, error, progressText);

        public static AgentHelpHelperResult Failed(string error)
            => new("error", null, error, null);
    }

    private sealed record AgentHelpFinalAnswer(string Text, string? ProgressText);

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
            string? callerAgent,
            string? callerSessionId,
            string? callerRequestId,
            string? issueSummary,
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
            CallerAgent = callerAgent;
            CallerSessionId = callerSessionId;
            CallerRequestId = callerRequestId;
            IssueSummary = issueSummary;
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
        public string? CallerAgent { get; }
        public string? CallerSessionId { get; }
        public string? CallerRequestId { get; }
        public string? IssueSummary { get; }
        public AgentHelpCorpusSummary? CorpusSummary { get; set; }
        public AgentHelpPromptContext? PromptContext { get; set; }
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
