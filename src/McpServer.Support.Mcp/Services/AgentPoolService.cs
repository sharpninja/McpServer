using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-052..058: In-memory pooled runtime for agent lifecycle, one-shot queue dispatch, and SSE fan-out streams.
/// </summary>
public sealed class AgentPoolService : IAgentPoolService, IDisposable
{
    private static readonly BoundedChannelOptions s_channelOptions = new(512)
    {
        FullMode = BoundedChannelFullMode.DropOldest,
        SingleReader = true,
        SingleWriter = false,
    };

    private readonly object _sync = new();
    private readonly Dictionary<string, AgentRuntimeState> _agents = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, QueueJobState> _jobs = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _queuedJobIds = [];
    private readonly SemaphoreSlim _dispatchGate = new(1, 1);
    private readonly ConcurrentDictionary<Guid, Channel<AgentPoolNotificationEventDto>> _notificationSubscribers = new();
    private readonly ConcurrentDictionary<string, ConcurrentDictionary<Guid, Channel<AgentPoolJobStreamEventDto>>> _jobSubscribers = new(StringComparer.OrdinalIgnoreCase);

    private readonly IVoiceConversationService _voiceService;
    private readonly IPromptTemplateService _templateService;
    private readonly PromptTemplateRenderer _templateRenderer;
    private readonly ITodoPromptProvider _todoPromptProvider;
    private readonly WorkspaceServiceAccessor _workspaceAccessor;
    private readonly IOptionsMonitor<AgentPoolOptions> _poolOptions;
    private readonly IOptionsMonitor<TodoPromptOptions> _todoPromptOptions;
    private readonly ILogger<AgentPoolService> _logger;
    private readonly IDisposable? _optionsChangeRegistration;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentPoolService"/> class.
    /// </summary>
    public AgentPoolService(
        IVoiceConversationService voiceService,
        IPromptTemplateService templateService,
        PromptTemplateRenderer templateRenderer,
        ITodoPromptProvider todoPromptProvider,
        WorkspaceServiceAccessor workspaceAccessor,
        IOptionsMonitor<AgentPoolOptions> poolOptions,
        IOptionsMonitor<TodoPromptOptions> todoPromptOptions,
        ILogger<AgentPoolService> logger)
    {
        _voiceService = voiceService ?? throw new ArgumentNullException(nameof(voiceService));
        _templateService = templateService ?? throw new ArgumentNullException(nameof(templateService));
        _templateRenderer = templateRenderer ?? throw new ArgumentNullException(nameof(templateRenderer));
        _todoPromptProvider = todoPromptProvider ?? throw new ArgumentNullException(nameof(todoPromptProvider));
        _workspaceAccessor = workspaceAccessor ?? throw new ArgumentNullException(nameof(workspaceAccessor));
        _poolOptions = poolOptions ?? throw new ArgumentNullException(nameof(poolOptions));
        _todoPromptOptions = todoPromptOptions ?? throw new ArgumentNullException(nameof(todoPromptOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        ReloadAgentDefinitions(_poolOptions.CurrentValue);
        _optionsChangeRegistration = _poolOptions.OnChange(ReloadAgentDefinitions);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentPoolAgentStatusDto>> GetAgentsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var items = _agents.Values
                .OrderBy(x => x.Definition.AgentName, StringComparer.OrdinalIgnoreCase)
                .Select(MapAgent)
                .ToList();
            return Task.FromResult<IReadOnlyList<AgentPoolAgentStatusDto>>(items);
        }
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<AgentPoolQueueItemDto>> GetQueueItemsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        lock (_sync)
        {
            var queuedSet = _queuedJobIds.ToHashSet(StringComparer.OrdinalIgnoreCase);
            var queued = _queuedJobIds
                .Where(id => _jobs.ContainsKey(id))
                .Select(id => _jobs[id])
                .Select(MapQueueItem);
            var nonQueued = _jobs.Values
                .Where(x => !queuedSet.Contains(x.JobId))
                .OrderByDescending(x => x.CreatedUtc)
                .Select(MapQueueItem);
            return Task.FromResult<IReadOnlyList<AgentPoolQueueItemDto>>(queued.Concat(nonQueued).ToList());
        }
    }

    /// <inheritdoc />
    public async Task<AgentPoolConnectResult> ConnectInteractiveAsync(string? agentName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var resolvedAgentName = ResolveAgentName(agentName, AgentPoolOneShotContext.AdHoc, interactiveFallback: true);
        if (resolvedAgentName is null)
            return new AgentPoolConnectResult { Success = false, Error = "No pooled agent available for interactive connection." };

        var start = await StartAgentAsync(resolvedAgentName, cancellationToken).ConfigureAwait(false);
        if (!start.Success)
            return new AgentPoolConnectResult { Success = false, Error = start.Error };

        lock (_sync)
        {
            if (!_agents.TryGetValue(resolvedAgentName, out var state))
                return new AgentPoolConnectResult { Success = false, Error = $"Agent '{resolvedAgentName}' no longer exists." };

            state.ActiveVoiceLinks++;
            return new AgentPoolConnectResult
            {
                Success = true,
                AgentName = resolvedAgentName,
                SessionId = state.SessionId,
            };
        }
    }

    /// <inheritdoc />
    public async Task<AgentPoolMutationResult> StartAgentAsync(string agentName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(agentName))
            return new AgentPoolMutationResult { Success = false, Error = "agentName is required." };

        AgentRuntimeState? state;
        lock (_sync)
        {
            _agents.TryGetValue(agentName, out state);
            if (state is not null)
                state.Lifecycle = "starting";
        }

        if (state is null)
            return new AgentPoolMutationResult { Success = false, Error = $"Unknown pooled agent '{agentName}'." };

        try
        {
            var existing = !string.IsNullOrWhiteSpace(state.SessionId)
                ? await _voiceService.GetStatusAsync(state.SessionId!, cancellationToken).ConfigureAwait(false)
                : null;

            if (existing is null)
            {
                var create = await _voiceService.CreateSessionAsync(
                    new VoiceSessionCreateRequest
                    {
                        AgentName = state.Definition.AgentName,
                        DeviceId = $"agent-pool-{state.Definition.AgentName}",
                        ClientName = "agent-pool",
                        WorkspacePath = _workspaceAccessor.GetWorkspacePath(),
                        AgentPath = state.Definition.AgentPath,
                        AgentModel = state.Definition.AgentModel,
                        AgentSeed = state.Definition.AgentSeed,
                        AgentParameters = state.Definition.AgentParameters,
                        OneShotSession = false,
                    },
                    cancellationToken).ConfigureAwait(false);

                lock (_sync)
                {
                    if (_agents.TryGetValue(agentName, out var current))
                    {
                        current.SessionId = create.SessionId;
                        current.Lifecycle = "idle";
                    }
                }
            }
            else
            {
                lock (_sync)
                {
                    if (_agents.TryGetValue(agentName, out var current))
                    {
                        current.SessionId = existing.SessionId;
                        current.Lifecycle = current.IsBusy ? "busy" : "idle";
                    }
                }
            }

            return new AgentPoolMutationResult { Success = true };
        }
        catch (Exception ex) when (ex is InvalidOperationException or ArgumentException)
        {
            lock (_sync)
            {
                if (_agents.TryGetValue(agentName, out var current))
                    current.Lifecycle = "error";
            }

            _logger.LogWarning(ex, "Failed to start pooled agent {AgentName}", agentName);
            return new AgentPoolMutationResult { Success = false, Error = ex.Message };
        }
    }

    /// <inheritdoc />
    public async Task<AgentPoolMutationResult> StopAgentAsync(string agentName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(agentName))
            return new AgentPoolMutationResult { Success = false, Error = "agentName is required." };

        string? sessionId;
        lock (_sync)
        {
            if (!_agents.TryGetValue(agentName, out var state))
                return new AgentPoolMutationResult { Success = false, Error = $"Unknown pooled agent '{agentName}'." };

            state.Lifecycle = "stopping";
            sessionId = state.SessionId;
            state.IsBusy = false;
            state.ActiveJobId = null;
        }

        if (!string.IsNullOrWhiteSpace(sessionId))
            await _voiceService.DeleteSessionAsync(sessionId, cancellationToken).ConfigureAwait(false);

        lock (_sync)
        {
            if (_agents.TryGetValue(agentName, out var state))
            {
                state.SessionId = null;
                state.Lifecycle = "offline";
            }
        }

        return new AgentPoolMutationResult { Success = true };
    }

    /// <inheritdoc />
    public async Task<AgentPoolMutationResult> RecycleAgentAsync(string agentName, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var stop = await StopAgentAsync(agentName, cancellationToken).ConfigureAwait(false);
        if (!stop.Success)
            return stop;

        return await StartAgentAsync(agentName, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<AgentPoolEnqueueResult> EnqueueOneShotAsync(AgentPoolOneShotRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var resolution = await ResolvePromptAsync(request, cancellationToken).ConfigureAwait(false);
        if (!resolution.Success)
            return new AgentPoolEnqueueResult { Success = false, Error = resolution.Error };

        var effectiveContext = request.Context ?? InferContextFromPrompt(resolution.PromptText);
        var resolvedAgentName = ResolveAgentName(request.AgentName, effectiveContext, interactiveFallback: false);
        if (resolvedAgentName is null)
            return new AgentPoolEnqueueResult { Success = false, Error = "No eligible pooled agent configured." };

        var maxQueueSize = Math.Max(1, _poolOptions.CurrentValue.MaxQueueSize);
        var jobId = $"job-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}".ToLowerInvariant();
        QueueJobState snapshot;
        lock (_sync)
        {
            var activeCount = _jobs.Values.Count(x => x.Status is "queued" or "processing");
            if (activeCount >= maxQueueSize)
                return new AgentPoolEnqueueResult { Success = false, Error = $"Queue is full (max {maxQueueSize})." };

            var state = new QueueJobState
            {
                JobId = jobId,
                AgentName = resolvedAgentName,
                Status = "queued",
                Context = effectiveContext,
                PromptTemplateId = resolution.TemplateId,
                RenderedPrompt = resolution.PromptText,
                CreatedUtc = DateTimeOffset.UtcNow,
            };
            _jobs[jobId] = state;
            _queuedJobIds.Add(jobId);
            snapshot = state.Clone();
        }

        PublishNotification(new AgentPoolNotificationEventDto
        {
            EventType = "queued",
            AgentName = snapshot.AgentName,
            JobId = snapshot.JobId,
            LastRequestPrompt = snapshot.RenderedPrompt,
            Message = "One-shot request queued.",
        });
        PublishJobStream(snapshot.JobId, new AgentPoolJobStreamEventDto
        {
            JobId = snapshot.JobId,
            EventType = "queued",
            Status = "queued",
        });

        _ = TryDispatchAsync();

        return new AgentPoolEnqueueResult
        {
            Success = true,
            JobId = snapshot.JobId,
            AgentName = snapshot.AgentName,
            RenderedPrompt = snapshot.RenderedPrompt,
        };
    }

    /// <inheritdoc />
    public Task<AgentPoolMutationResult> CancelQueueItemAsync(string jobId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(jobId))
            return Task.FromResult(new AgentPoolMutationResult { Success = false, Error = "jobId is required." });

        string? sessionIdToInterrupt = null;
        QueueJobState? updated = null;

        lock (_sync)
        {
            if (!_jobs.TryGetValue(jobId, out var state))
                return Task.FromResult(new AgentPoolMutationResult { Success = false, Error = $"Queue item '{jobId}' not found." });

            if (state.Status == "queued")
            {
                state.Status = "canceled";
                state.CompletedUtc = DateTimeOffset.UtcNow;
                _queuedJobIds.Remove(jobId);
                updated = state.Clone();
            }
            else if (state.Status == "processing")
            {
                state.CancelRequested = true;
                state.Status = "canceling";
                updated = state.Clone();
                sessionIdToInterrupt = state.SessionId;
            }
            else
            {
                return Task.FromResult(new AgentPoolMutationResult
                {
                    Success = false,
                    Error = $"Queue item '{jobId}' cannot be canceled from status '{state.Status}'.",
                });
            }
        }

        if (!string.IsNullOrWhiteSpace(sessionIdToInterrupt))
            _ = _voiceService.InterruptAsync(sessionIdToInterrupt, CancellationToken.None);

        if (updated is not null)
        {
            PublishNotification(new AgentPoolNotificationEventDto
            {
                EventType = updated.Status == "canceling" ? "canceling" : "canceled",
                AgentName = updated.AgentName,
                JobId = updated.JobId,
                SessionId = updated.SessionId,
                LastRequestPrompt = updated.RenderedPrompt,
                Message = updated.Status == "canceling" ? "Cancellation requested." : "Queue item canceled.",
            });
            PublishJobStream(updated.JobId, new AgentPoolJobStreamEventDto
            {
                JobId = updated.JobId,
                EventType = updated.Status == "canceling" ? "canceling" : "canceled",
                Status = updated.Status,
            });
        }

        return Task.FromResult(new AgentPoolMutationResult { Success = true });
    }

    /// <inheritdoc />
    public Task<AgentPoolMutationResult> RemoveQueueItemAsync(string jobId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        if (string.IsNullOrWhiteSpace(jobId))
            return Task.FromResult(new AgentPoolMutationResult { Success = false, Error = "jobId is required." });

        lock (_sync)
        {
            if (!_jobs.TryGetValue(jobId, out var state))
                return Task.FromResult(new AgentPoolMutationResult { Success = false, Error = $"Queue item '{jobId}' not found." });

            if (state.Status == "processing")
                return Task.FromResult(new AgentPoolMutationResult { Success = false, Error = "Cannot remove processing queue item." });

            _queuedJobIds.Remove(jobId);
            _jobs.Remove(jobId);
        }

        PublishNotification(new AgentPoolNotificationEventDto
        {
            EventType = "removed",
            JobId = jobId,
            Message = "Queue item removed.",
        });
        PublishJobStream(jobId, new AgentPoolJobStreamEventDto
        {
            JobId = jobId,
            EventType = "removed",
            Status = "removed",
        });

        return Task.FromResult(new AgentPoolMutationResult { Success = true });
    }

    /// <inheritdoc />
    public Task<AgentPoolMutationResult> MoveQueueItemUpAsync(string jobId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(MoveQueueItem(jobId, moveUp: true));
    }

    /// <inheritdoc />
    public Task<AgentPoolMutationResult> MoveQueueItemDownAsync(string jobId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(MoveQueueItem(jobId, moveUp: false));
    }

    /// <inheritdoc />
    public async Task<AgentPoolPromptResolutionResult> ResolvePromptAsync(AgentPoolOneShotRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        cancellationToken.ThrowIfCancellationRequested();

        var hasTemplateId = !string.IsNullOrWhiteSpace(request.PromptTemplateId);
        var hasPromptText = !string.IsNullOrWhiteSpace(request.PromptText);

        if (hasTemplateId && hasPromptText)
            return new AgentPoolPromptResolutionResult { Success = false, Error = "Specify either promptTemplateId or promptText, not both." };

        Dictionary<string, object?> variables = [];
        if (request.UseWorkspaceContext)
            MergeWorkspaceContextVariables(variables, request);
        if (request.Values is not null)
        {
            foreach (var pair in request.Values)
                variables[pair.Key] = pair.Value;
        }

        if (hasTemplateId)
        {
            if (string.IsNullOrWhiteSpace(request.Id))
                return new AgentPoolPromptResolutionResult { Success = false, Error = "id is required for template-resolved requests." };

            var template = await _templateService.GetByIdAsync(request.PromptTemplateId!, cancellationToken).ConfigureAwait(false);
            if (template is null)
                return new AgentPoolPromptResolutionResult { Success = false, Error = $"Template '{request.PromptTemplateId}' not found." };

            var missing = PromptTemplateRenderer.ValidateRequiredVariables(template.Variables, variables);
            if (missing.Count > 0)
                return new AgentPoolPromptResolutionResult
                {
                    Success = false,
                    Error = $"Missing required variables: {string.Join(", ", missing)}",
                };

            var rendered = _templateRenderer.Render(template.Content, variables);
            return new AgentPoolPromptResolutionResult
            {
                Success = true,
                PromptText = ApplyBraceTokenReplacement(rendered, variables),
                TemplateId = template.Id,
                TemplateResolved = true,
            };
        }

        if (hasPromptText)
        {
            return new AgentPoolPromptResolutionResult
            {
                Success = true,
                PromptText = request.PromptText!.Trim(),
                TemplateResolved = false,
            };
        }

        if (request.Context is null)
            return new AgentPoolPromptResolutionResult { Success = false, Error = "No prompt source provided." };

        if (request.Context == AgentPoolOneShotContext.AdHoc)
            return new AgentPoolPromptResolutionResult { Success = false, Error = "AdHoc context requires explicit promptText when promptTemplateId is not provided." };

        if (string.IsNullOrWhiteSpace(request.Id))
            return new AgentPoolPromptResolutionResult { Success = false, Error = "id is required for context template resolution." };

        var templateText = await GetContextTemplateAsync(request.Context.Value, cancellationToken).ConfigureAwait(false);
        var populated = ApplyBraceTokenReplacement(templateText, variables);
        return new AgentPoolPromptResolutionResult
        {
            Success = true,
            PromptText = populated,
            TemplateId = request.Context.Value.ToString(),
            TemplateResolved = true,
        };
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AgentPoolNotificationEventDto> SubscribeNotificationsAsync(
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var subscriptionId = Guid.NewGuid();
        var channel = Channel.CreateBounded<AgentPoolNotificationEventDto>(s_channelOptions);
        _notificationSubscribers[subscriptionId] = channel;

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return item;
        }
        finally
        {
            _notificationSubscribers.TryRemove(subscriptionId, out _);
        }
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<AgentPoolJobStreamEventDto> SubscribeJobStreamAsync(
        string jobId,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            yield break;

        var channel = Channel.CreateBounded<AgentPoolJobStreamEventDto>(s_channelOptions);
        var subscriptionId = Guid.NewGuid();
        var subscribers = _jobSubscribers.GetOrAdd(jobId, _ => new ConcurrentDictionary<Guid, Channel<AgentPoolJobStreamEventDto>>());
        subscribers[subscriptionId] = channel;

        string? agentNameForCounter = null;
        lock (_sync)
        {
            if (_jobs.TryGetValue(jobId, out var state))
            {
                agentNameForCounter = state.AgentName;
                channel.Writer.TryWrite(new AgentPoolJobStreamEventDto
                {
                    JobId = state.JobId,
                    EventType = "snapshot",
                    Status = state.Status,
                    Text = state.ResponseText,
                    Error = state.Error,
                });
            }

            if (!string.IsNullOrWhiteSpace(agentNameForCounter) && _agents.TryGetValue(agentNameForCounter, out var agent))
                agent.ReadOnlySubscribers++;
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(cancellationToken).ConfigureAwait(false))
                yield return item;
        }
        finally
        {
            if (_jobSubscribers.TryGetValue(jobId, out var existing))
            {
                existing.TryRemove(subscriptionId, out _);
                if (existing.IsEmpty)
                    _jobSubscribers.TryRemove(jobId, out _);
            }

            lock (_sync)
            {
                if (!string.IsNullOrWhiteSpace(agentNameForCounter) && _agents.TryGetValue(agentNameForCounter, out var agent))
                    agent.ReadOnlySubscribers = Math.Max(0, agent.ReadOnlySubscribers - 1);
            }
        }
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _optionsChangeRegistration?.Dispose();
        _dispatchGate.Dispose();

        foreach (var channel in _notificationSubscribers.Values)
            channel.Writer.TryComplete();
        _notificationSubscribers.Clear();

        foreach (var group in _jobSubscribers.Values)
        {
            foreach (var channel in group.Values)
                channel.Writer.TryComplete();
        }

        _jobSubscribers.Clear();
    }

    private void ReloadAgentDefinitions(AgentPoolOptions options)
    {
        lock (_sync)
        {
            var names = options.Agents
                .Where(x => !string.IsNullOrWhiteSpace(x.AgentName))
                .Select(x => x.AgentName.Trim())
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            var removed = _agents.Keys.Where(x => !names.Contains(x)).ToList();
            foreach (var key in removed)
                _agents.Remove(key);

            foreach (var definition in options.Agents)
            {
                if (string.IsNullOrWhiteSpace(definition.AgentName))
                    continue;

                if (_agents.TryGetValue(definition.AgentName, out var existing))
                {
                    existing.Definition = definition;
                }
                else
                {
                    _agents[definition.AgentName] = new AgentRuntimeState
                    {
                        Definition = definition,
                        Lifecycle = "offline",
                    };
                }
            }
        }
    }

    private AgentPoolMutationResult MoveQueueItem(string jobId, bool moveUp)
    {
        if (string.IsNullOrWhiteSpace(jobId))
            return new AgentPoolMutationResult { Success = false, Error = "jobId is required." };

        lock (_sync)
        {
            if (!_jobs.TryGetValue(jobId, out var state))
                return new AgentPoolMutationResult { Success = false, Error = $"Queue item '{jobId}' not found." };

            if (state.Status != "queued")
                return new AgentPoolMutationResult { Success = false, Error = $"Queue item '{jobId}' is not in queued state." };

            var index = _queuedJobIds.FindIndex(x => string.Equals(x, jobId, StringComparison.OrdinalIgnoreCase));
            if (index < 0)
                return new AgentPoolMutationResult { Success = false, Error = $"Queue item '{jobId}' is not currently queued." };

            var targetIndex = moveUp ? index - 1 : index + 1;
            if (targetIndex < 0 || targetIndex >= _queuedJobIds.Count)
                return new AgentPoolMutationResult { Success = true };

            (_queuedJobIds[index], _queuedJobIds[targetIndex]) = (_queuedJobIds[targetIndex], _queuedJobIds[index]);
        }

        PublishNotification(new AgentPoolNotificationEventDto
        {
            EventType = moveUp ? "moved_up" : "moved_down",
            JobId = jobId,
            Message = moveUp ? "Queue item moved up." : "Queue item moved down.",
        });

        return new AgentPoolMutationResult { Success = true };
    }

    private async Task TryDispatchAsync()
    {
        if (!await _dispatchGate.WaitAsync(0).ConfigureAwait(false))
            return;

        try
        {
            while (true)
            {
                QueueJobState? job;
                AgentRuntimeState? agent;
                lock (_sync)
                {
                    job = null;
                    agent = null;

                    foreach (var queuedId in _queuedJobIds.ToList())
                    {
                        if (!_jobs.TryGetValue(queuedId, out var candidate) || candidate.Status != "queued")
                        {
                            _queuedJobIds.Remove(queuedId);
                            continue;
                        }

                        if (string.IsNullOrWhiteSpace(candidate.AgentName) || !_agents.TryGetValue(candidate.AgentName, out var candidateAgent))
                        {
                            candidate.Status = "failed";
                            candidate.Error = "No eligible pooled agent found.";
                            candidate.CompletedUtc = DateTimeOffset.UtcNow;
                            _queuedJobIds.Remove(queuedId);
                            PublishTerminalFailure(candidate);
                            continue;
                        }

                        if (candidateAgent.IsBusy)
                            continue;

                        job = candidate;
                        agent = candidateAgent;
                        _queuedJobIds.Remove(queuedId);
                        break;
                    }

                    if (job is null || agent is null)
                        break;

                    job.Status = "processing";
                    job.StartedUtc = DateTimeOffset.UtcNow;
                    agent.IsBusy = true;
                    agent.ActiveJobId = job.JobId;
                    agent.Lifecycle = "busy";
                    agent.LastRequestPrompt = job.RenderedPrompt;
                }

                PublishNotification(new AgentPoolNotificationEventDto
                {
                    EventType = "processing",
                    AgentName = job.AgentName,
                    JobId = job.JobId,
                    LastRequestPrompt = job.RenderedPrompt,
                    Message = "One-shot request is processing.",
                });
                PublishJobStream(job.JobId, new AgentPoolJobStreamEventDto
                {
                    JobId = job.JobId,
                    EventType = "processing",
                    Status = "processing",
                });

                var start = await StartAgentAsync(job.AgentName!, CancellationToken.None).ConfigureAwait(false);
                if (!start.Success)
                {
                    lock (_sync)
                    {
                        if (_jobs.TryGetValue(job.JobId, out var failedJob))
                        {
                            failedJob.Status = "failed";
                            failedJob.Error = start.Error ?? "Failed to start pooled agent.";
                            failedJob.CompletedUtc = DateTimeOffset.UtcNow;
                        }

                        if (_agents.TryGetValue(job.AgentName!, out var failedAgent))
                        {
                            failedAgent.IsBusy = false;
                            failedAgent.ActiveJobId = null;
                            failedAgent.Lifecycle = "error";
                        }
                    }

                    PublishTerminalFailure(job);
                    continue;
                }

                string? sessionId;
                lock (_sync)
                {
                    _agents.TryGetValue(job.AgentName!, out var currentAgent);
                    sessionId = currentAgent?.SessionId;
                    if (_jobs.TryGetValue(job.JobId, out var currentJob))
                        currentJob.SessionId = sessionId;
                }

                VoiceTurnResponse? response = null;
                Exception? executionError = null;
                if (!job.CancelRequested && !string.IsNullOrWhiteSpace(sessionId))
                {
                    try
                    {
                        response = await _voiceService.SubmitTurnAsync(
                            sessionId,
                            new VoiceTurnRequest { UserTranscriptText = job.RenderedPrompt ?? string.Empty },
                            CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        executionError = ex;
                    }
                }

                QueueJobState? completedSnapshot;
                lock (_sync)
                {
                    _jobs.TryGetValue(job.JobId, out var finalJob);
                    _agents.TryGetValue(job.AgentName!, out var finalAgent);
                    if (finalJob is null || finalAgent is null)
                        continue;

                    finalAgent.IsBusy = false;
                    finalAgent.ActiveJobId = null;
                    finalAgent.Lifecycle = "idle";

                    finalJob.CompletedUtc = DateTimeOffset.UtcNow;

                    if (finalJob.CancelRequested)
                    {
                        finalJob.Status = "canceled";
                    }
                    else if (executionError is not null)
                    {
                        finalJob.Status = "failed";
                        finalJob.Error = executionError.Message;
                    }
                    else if (response is null)
                    {
                        finalJob.Status = "failed";
                        finalJob.Error = "No response returned from voice runtime.";
                    }
                    else if (!string.Equals(response.Status, "completed", StringComparison.OrdinalIgnoreCase))
                    {
                        finalJob.Status = response.Status is "interrupted" ? "canceled" : "failed";
                        finalJob.Error = response.Error;
                        finalJob.ResponseText = response.AssistantDisplayText;
                    }
                    else
                    {
                        finalJob.Status = "completed";
                        finalJob.ResponseText = response.AssistantDisplayText;
                    }

                    completedSnapshot = finalJob.Clone();
                }

                if (completedSnapshot is not null)
                {
                    var terminalEventType = completedSnapshot.Status switch
                    {
                        "completed" => "completed",
                        "canceled" => "canceled",
                        _ => "failed",
                    };

                    PublishNotification(new AgentPoolNotificationEventDto
                    {
                        EventType = terminalEventType,
                        AgentName = completedSnapshot.AgentName,
                        JobId = completedSnapshot.JobId,
                        SessionId = completedSnapshot.SessionId,
                        LastRequestPrompt = completedSnapshot.RenderedPrompt,
                        Message = completedSnapshot.Error ?? completedSnapshot.ResponseText,
                    });
                    PublishJobStream(completedSnapshot.JobId, new AgentPoolJobStreamEventDto
                    {
                        JobId = completedSnapshot.JobId,
                        EventType = terminalEventType,
                        Status = completedSnapshot.Status,
                        Text = completedSnapshot.ResponseText,
                        Error = completedSnapshot.Error,
                    });
                }
            }
        }
        finally
        {
            _dispatchGate.Release();
        }
    }

    private string? ResolveAgentName(string? explicitAgentName, AgentPoolOneShotContext? context, bool interactiveFallback)
    {
        lock (_sync)
        {
            if (!string.IsNullOrWhiteSpace(explicitAgentName))
                return _agents.ContainsKey(explicitAgentName) ? explicitAgentName : null;

            AgentRuntimeState? selected = context switch
            {
                AgentPoolOneShotContext.Plan => _agents.Values.FirstOrDefault(x => x.Definition.IsTodoPlanDefault),
                AgentPoolOneShotContext.Status => _agents.Values.FirstOrDefault(x => x.Definition.IsTodoStatusDefault),
                AgentPoolOneShotContext.Implement => _agents.Values.FirstOrDefault(x => x.Definition.IsTodoImplementDefault),
                AgentPoolOneShotContext.AdHoc => _agents.Values.FirstOrDefault(x => x.Definition.IsInteractiveDefault),
                null when interactiveFallback => _agents.Values.FirstOrDefault(x => x.Definition.IsInteractiveDefault),
                _ => null,
            };

            selected ??= _agents.Values.OrderBy(x => x.Definition.AgentName, StringComparer.OrdinalIgnoreCase).FirstOrDefault();
            return selected?.Definition.AgentName;
        }
    }

    private static AgentPoolOneShotContext InferContextFromPrompt(string? promptText)
    {
        if (string.IsNullOrWhiteSpace(promptText))
            return AgentPoolOneShotContext.AdHoc;

        var normalized = promptText.Trim().ToLowerInvariant();
        if (normalized.Contains("todo status", StringComparison.Ordinal) ||
            normalized.Contains("status", StringComparison.Ordinal))
            return AgentPoolOneShotContext.Status;

        if (normalized.Contains("todo plan", StringComparison.Ordinal) ||
            normalized.Contains("implementation plan", StringComparison.Ordinal) ||
            normalized.Contains("plan", StringComparison.Ordinal))
            return AgentPoolOneShotContext.Plan;

        if (normalized.Contains("todo implement", StringComparison.Ordinal) ||
            normalized.Contains("implement", StringComparison.Ordinal) ||
            normalized.Contains("code change", StringComparison.Ordinal))
            return AgentPoolOneShotContext.Implement;

        return AgentPoolOneShotContext.AdHoc;
    }

    private async Task<string> GetContextTemplateAsync(AgentPoolOneShotContext context, CancellationToken cancellationToken)
    {
        return context switch
        {
            AgentPoolOneShotContext.Plan => await _todoPromptProvider.GetPlanPromptAsync(cancellationToken).ConfigureAwait(false),
            AgentPoolOneShotContext.Status => await _todoPromptProvider.GetStatusPromptAsync(cancellationToken).ConfigureAwait(false),
            AgentPoolOneShotContext.Implement => await _todoPromptProvider.GetImplementPromptAsync(cancellationToken).ConfigureAwait(false),
            _ => throw new InvalidOperationException("AdHoc context does not use context-template resolution."),
        };
    }

    private void MergeWorkspaceContextVariables(Dictionary<string, object?> variables, AgentPoolOneShotRequest request)
    {
        variables["workspacePath"] = _workspaceAccessor.GetWorkspacePath();
        variables["baseUrl"] = _todoPromptOptions.CurrentValue.BaseUrl;

        if (!string.IsNullOrWhiteSpace(request.Id))
            variables["id"] = request.Id;

        if (request.Context is not null)
            variables["context"] = request.Context.ToString();
    }

    private static string ApplyBraceTokenReplacement(string templateText, IReadOnlyDictionary<string, object?> values)
    {
        var rendered = templateText;
        foreach (var pair in values)
        {
            if (pair.Value is null)
                continue;

            rendered = rendered.Replace($"{{{pair.Key}}}", pair.Value.ToString(), StringComparison.Ordinal);
        }

        return rendered;
    }

    private void PublishNotification(AgentPoolNotificationEventDto notification)
    {
        foreach (var subscriber in _notificationSubscribers.Values)
            subscriber.Writer.TryWrite(notification);
    }

    private void PublishJobStream(string jobId, AgentPoolJobStreamEventDto evt)
    {
        if (_jobSubscribers.TryGetValue(jobId, out var subscribers))
        {
            foreach (var channel in subscribers.Values)
                channel.Writer.TryWrite(evt);
        }
    }

    private void PublishTerminalFailure(QueueJobState job)
    {
        PublishNotification(new AgentPoolNotificationEventDto
        {
            EventType = "failed",
            AgentName = job.AgentName,
            JobId = job.JobId,
            SessionId = job.SessionId,
            LastRequestPrompt = job.RenderedPrompt,
            Message = job.Error,
        });
        PublishJobStream(job.JobId, new AgentPoolJobStreamEventDto
        {
            JobId = job.JobId,
            EventType = "failed",
            Status = "failed",
            Error = job.Error,
        });
    }

    private static AgentPoolAgentStatusDto MapAgent(AgentRuntimeState state)
        => new()
        {
            AgentName = state.Definition.AgentName,
            Lifecycle = state.Lifecycle,
            SessionId = state.SessionId,
            ActiveJobId = state.ActiveJobId,
            LastRequestPrompt = state.LastRequestPrompt,
            ActiveVoiceLinks = state.ActiveVoiceLinks,
            ReadOnlySubscribers = state.ReadOnlySubscribers,
            IsInteractiveDefault = state.Definition.IsInteractiveDefault,
            IsTodoPlanDefault = state.Definition.IsTodoPlanDefault,
            IsTodoStatusDefault = state.Definition.IsTodoStatusDefault,
            IsTodoImplementDefault = state.Definition.IsTodoImplementDefault,
        };

    private static AgentPoolQueueItemDto MapQueueItem(QueueJobState state)
        => new()
        {
            JobId = state.JobId,
            AgentName = state.AgentName,
            Status = state.Status,
            Context = state.Context,
            PromptTemplateId = state.PromptTemplateId,
            RenderedPrompt = state.RenderedPrompt,
            ResponseText = state.ResponseText,
            Error = state.Error,
            CreatedUtc = state.CreatedUtc,
            StartedUtc = state.StartedUtc,
            CompletedUtc = state.CompletedUtc,
            SessionId = state.SessionId,
        };

    private sealed class AgentRuntimeState
    {
        public required AgentPoolDefinitionOptions Definition { get; set; }

        public string Lifecycle { get; set; } = "offline";

        public string? SessionId { get; set; }

        public bool IsBusy { get; set; }

        public string? ActiveJobId { get; set; }

        public string? LastRequestPrompt { get; set; }

        public int ActiveVoiceLinks { get; set; }

        public int ReadOnlySubscribers { get; set; }
    }

    private sealed class QueueJobState
    {
        public required string JobId { get; init; }

        public string? AgentName { get; set; }

        public required string Status { get; set; }

        public AgentPoolOneShotContext? Context { get; init; }

        public string? PromptTemplateId { get; init; }

        public string? RenderedPrompt { get; init; }

        public string? ResponseText { get; set; }

        public string? Error { get; set; }

        public required DateTimeOffset CreatedUtc { get; init; }

        public DateTimeOffset? StartedUtc { get; set; }

        public DateTimeOffset? CompletedUtc { get; set; }

        public string? SessionId { get; set; }

        public bool CancelRequested { get; set; }

        public QueueJobState Clone()
            => (QueueJobState)MemberwiseClone();
    }
}
