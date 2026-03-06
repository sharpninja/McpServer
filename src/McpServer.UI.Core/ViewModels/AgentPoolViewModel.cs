using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for the Agent Pool tab.
/// Owns the screen state for configured agents, runtime agents, queue entries, and queue actions.
/// </summary>
public sealed partial class AgentPoolViewModel : ObservableObject
{
    private readonly Dispatcher _dispatcher;
    private readonly WorkspaceContextViewModel _workspaceContext;
    private readonly ILogger<AgentPoolViewModel> _logger;

    /// <summary>Initializes a new instance of the <see cref="AgentPoolViewModel"/> class.</summary>
    public AgentPoolViewModel(
        Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<AgentPoolViewModel> logger)
    {
        _dispatcher = dispatcher;
        _workspaceContext = workspaceContext;
        _logger = logger;
        workspaceContext.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(WorkspaceContextViewModel.ActiveWorkspacePath))
                _ = Task.Run(() => LoadAsync());
        };
    }

    /// <summary>Logical area represented by this ViewModel.</summary>
    public McpArea Area => McpArea.Agents;

    /// <summary>Configured global agent definitions.</summary>
    public ObservableCollection<AgentDefinitionSummaryItem> ConfiguredAgents { get; } = [];

    /// <summary>Pooled runtime agents.</summary>
    public ObservableCollection<AgentPoolRuntimeAgentSnapshot> RuntimeAgents { get; } = [];

    /// <summary>Pooled queue entries.</summary>
    public ObservableCollection<AgentPoolQueueItemSnapshot> QueueItems { get; } = [];

    /// <summary>Selected configured-agent row index.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedConfiguredAgent))]
    private int _selectedConfiguredIndex = -1;

    /// <summary>Selected runtime-agent row index.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedRuntimeAgent))]
    private int _selectedRuntimeIndex = -1;

    /// <summary>Selected queue-item row index.</summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedQueueItem))]
    private int _selectedQueueIndex = -1;

    /// <summary>User-entered or selected agent name for actions.</summary>
    [ObservableProperty]
    private string? _agentNameInput;

    /// <summary>User-entered ad-hoc prompt text.</summary>
    [ObservableProperty]
    private string? _promptInput;

    /// <summary>Whether a load or mutation operation is running.</summary>
    [ObservableProperty]
    private bool _isLoading;

    /// <summary>Optional error message from the latest operation.</summary>
    [ObservableProperty]
    private string? _errorMessage;

    /// <summary>Optional status text from the latest operation.</summary>
    [ObservableProperty]
    private string _statusMessage = "Agent Pool";

    /// <summary>Selected configured agent summary.</summary>
    public AgentDefinitionSummaryItem? SelectedConfiguredAgent =>
        SelectedConfiguredIndex >= 0 && SelectedConfiguredIndex < ConfiguredAgents.Count
            ? ConfiguredAgents[SelectedConfiguredIndex]
            : null;

    /// <summary>Selected runtime agent snapshot.</summary>
    public AgentPoolRuntimeAgentSnapshot? SelectedRuntimeAgent =>
        SelectedRuntimeIndex >= 0 && SelectedRuntimeIndex < RuntimeAgents.Count
            ? RuntimeAgents[SelectedRuntimeIndex]
            : null;

    /// <summary>Selected queue item snapshot.</summary>
    public AgentPoolQueueItemSnapshot? SelectedQueueItem =>
        SelectedQueueIndex >= 0 && SelectedQueueIndex < QueueItems.Count
            ? QueueItems[SelectedQueueIndex]
            : null;

    /// <summary>
    /// Loads configured definitions, runtime agents, and queue entries.
    /// </summary>
    public async Task LoadAsync(CancellationToken ct = default)
    {
        if (IsLoading)
            return;

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = "Loading agent pool...";

        try
        {
            var definitionsResult = await _dispatcher.QueryAsync(new ListAgentDefinitionsQuery(), ct).ConfigureAwait(true);
            if (!definitionsResult.IsSuccess || definitionsResult.Value is null)
            {
                ErrorMessage = definitionsResult.Error ?? "Failed to load configured agents.";
                StatusMessage = "Agent pool load failed.";
                return;
            }

            var runtimeResult = await _dispatcher.QueryAsync(new ListAgentPoolAgentsQuery(), ct).ConfigureAwait(true);
            if (!runtimeResult.IsSuccess || runtimeResult.Value is null)
            {
                ErrorMessage = runtimeResult.Error ?? "Failed to load runtime agents.";
                StatusMessage = "Agent pool load failed.";
                return;
            }

            var queueResult = await _dispatcher.QueryAsync(new ListAgentPoolQueueQuery(), ct).ConfigureAwait(true);
            if (!queueResult.IsSuccess || queueResult.Value is null)
            {
                ErrorMessage = queueResult.Error ?? "Failed to load queue entries.";
                StatusMessage = "Agent pool load failed.";
                return;
            }

            ReplaceCollection(ConfiguredAgents, definitionsResult.Value.Items);
            ReplaceCollection(RuntimeAgents, runtimeResult.Value.Items);
            ReplaceCollection(QueueItems, queueResult.Value.Items);

            StatusMessage = $"Loaded agent pool ({ConfiguredAgents.Count} configured, {RuntimeAgents.Count} runtime, {QueueItems.Count} queued).";
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Agent pool load failed.";
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Loads full detail for a global agent definition.
    /// </summary>
    public async Task<AgentDefinitionDetail?> GetDefinitionAsync(string agentType, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentType))
            return null;

        var result = await _dispatcher.QueryAsync(new GetAgentDefinitionQuery(agentType), ct).ConfigureAwait(true);
        if (!result.IsSuccess)
        {
            ErrorMessage = result.Error ?? "Failed to load agent definition.";
            StatusMessage = "Agent definition load failed.";
            return null;
        }

        return result.Value;
    }

    /// <summary>
    /// Creates/updates a global definition, assigns it to the active workspace, and starts it in the pool.
    /// </summary>
    public async Task CreateOrUpdateAgentAndStartAsync(
        string agentName,
        string agentPath,
        string agentModel,
        string? agentSeed,
        CancellationToken ct = default)
    {
        agentName = agentName.Trim();
        agentPath = agentPath.Trim();
        agentModel = agentModel.Trim();
        if (string.IsNullOrWhiteSpace(agentName))
        {
            StatusMessage = "Enter an agent name first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(agentPath))
        {
            StatusMessage = "Enter an agent path first.";
            return;
        }

        if (string.IsNullOrWhiteSpace(agentModel))
        {
            StatusMessage = "Enter an agent model first.";
            return;
        }

        var workspacePath = _workspaceContext.ActiveWorkspacePath;
        if (string.IsNullOrWhiteSpace(workspacePath))
        {
            StatusMessage = "No active workspace selected.";
            return;
        }

        ErrorMessage = null;
        StatusMessage = $"Saving definition '{agentName}'...";
        var upsertDefinition = await _dispatcher.SendAsync(new UpsertAgentDefinitionCommand
        {
            Id = agentName,
            DisplayName = agentName,
            DefaultLaunchCommand = agentPath,
            DefaultModels = [agentModel],
            DefaultSeedPrompt = agentSeed ?? string.Empty
        }, ct).ConfigureAwait(true);

        if (!upsertDefinition.IsSuccess || upsertDefinition.Value is null)
        {
            ErrorMessage = upsertDefinition.Error ?? "Failed to save agent definition.";
            StatusMessage = "Create agent failed.";
            return;
        }

        if (!upsertDefinition.Value.Success)
        {
            ErrorMessage = upsertDefinition.Value.Error;
            StatusMessage = upsertDefinition.Value.Error ?? "Failed to save agent definition.";
            return;
        }

        StatusMessage = $"Assigning '{agentName}' to workspace...";
        var assignResult = await _dispatcher.SendAsync(new AssignWorkspaceAgentCommand
        {
            AgentId = agentName,
            WorkspacePath = workspacePath,
            Enabled = true,
            AgentIsolation = "worktree"
        }, ct).ConfigureAwait(true);

        if (!assignResult.IsSuccess || assignResult.Value is null)
        {
            ErrorMessage = assignResult.Error ?? "Failed to assign workspace agent.";
            StatusMessage = "Create agent failed.";
            return;
        }

        if (!assignResult.Value.Success)
        {
            ErrorMessage = assignResult.Value.Error;
            StatusMessage = assignResult.Value.Error ?? "Failed to assign workspace agent.";
            return;
        }

        StatusMessage = $"Starting '{agentName}'...";
        var startResult = await _dispatcher.SendAsync(new StartAgentPoolAgentCommand(agentName), ct).ConfigureAwait(true);
        if (!startResult.IsSuccess || startResult.Value is null)
        {
            ErrorMessage = startResult.Error ?? "Failed to start pooled agent.";
            StatusMessage = "Create agent failed.";
            return;
        }

        var startError = startResult.Value.Error ?? string.Empty;
        var started = startResult.Value.Success || startError.Contains("already", StringComparison.OrdinalIgnoreCase);
        if (!started)
        {
            ErrorMessage = startResult.Value.Error;
            StatusMessage = $"Agent '{agentName}' created and assigned, but start failed: {startResult.Value.Error ?? "unknown error"}";
            await LoadAsync(ct).ConfigureAwait(true);
            return;
        }

        AgentNameInput = agentName;
        await LoadAsync(ct).ConfigureAwait(true);
        StatusMessage = $"Agent '{agentName}' created, assigned, and started.";
    }

    /// <summary>Starts the selected/typed runtime agent.</summary>
    public Task StartSelectedAsync(CancellationToken ct = default)
        => ExecuteAgentMutationAsync(
            ResolveAgentNameOrNull(),
            name => new StartAgentPoolAgentCommand(name),
            name => $"Starting '{name}'...",
            name => $"Agent '{name}' started.",
            ct);

    /// <summary>Stops the selected/typed runtime agent.</summary>
    public Task StopSelectedAsync(CancellationToken ct = default)
        => ExecuteAgentMutationAsync(
            ResolveAgentNameOrNull(),
            name => new StopAgentPoolAgentCommand(name),
            name => $"Stopping '{name}'...",
            name => $"Agent '{name}' stopped.",
            ct);

    /// <summary>Recycles the selected/typed runtime agent.</summary>
    public Task RecycleSelectedAsync(CancellationToken ct = default)
        => ExecuteAgentMutationAsync(
            ResolveAgentNameOrNull(),
            name => new RecycleAgentPoolAgentCommand(name),
            name => $"Recycling '{name}'...",
            name => $"Agent '{name}' recycled.",
            ct);

    /// <summary>Connects to the selected/typed runtime agent.</summary>
    public async Task ConnectSelectedAsync(CancellationToken ct = default)
    {
        var agentName = ResolveAgentNameOrNull();
        if (string.IsNullOrWhiteSpace(agentName))
        {
            StatusMessage = "Select an agent row first.";
            return;
        }

        ErrorMessage = null;
        StatusMessage = $"Connecting to '{agentName}'...";
        var result = await _dispatcher.SendAsync(new ConnectAgentPoolAgentCommand(agentName), ct).ConfigureAwait(true);
        if (!result.IsSuccess || result.Value is null)
        {
            ErrorMessage = result.Error ?? "Connect failed.";
            StatusMessage = "Connect failed.";
            return;
        }

        if (!result.Value.Success)
        {
            ErrorMessage = result.Value.Error;
            StatusMessage = result.Value.Error ?? "Connect failed.";
            return;
        }

        await LoadAsync(ct).ConfigureAwait(true);
        StatusMessage = $"Connected '{agentName}' (session {result.Value.SessionId ?? "n/a"}).";
    }

    /// <summary>Cancels the selected queue item.</summary>
    public Task CancelSelectedJobAsync(CancellationToken ct = default)
        => ExecuteQueueMutationAsync(
            SelectedQueueItem?.JobId,
            jobId => new CancelAgentPoolQueueItemCommand(jobId),
            jobId => $"Canceling '{jobId}'...",
            jobId => $"Queue item '{jobId}' canceled.",
            ct);

    /// <summary>Removes the selected queue item.</summary>
    public Task RemoveSelectedJobAsync(CancellationToken ct = default)
        => ExecuteQueueMutationAsync(
            SelectedQueueItem?.JobId,
            jobId => new RemoveAgentPoolQueueItemCommand(jobId),
            jobId => $"Removing '{jobId}'...",
            jobId => $"Queue item '{jobId}' removed.",
            ct);

    /// <summary>Moves the selected queue item up.</summary>
    public Task MoveSelectedJobUpAsync(CancellationToken ct = default)
        => ExecuteQueueMutationAsync(
            SelectedQueueItem?.JobId,
            jobId => new MoveAgentPoolQueueItemUpCommand(jobId),
            jobId => $"Moving '{jobId}' up...",
            jobId => $"Queue item '{jobId}' moved up.",
            ct);

    /// <summary>Moves the selected queue item down.</summary>
    public Task MoveSelectedJobDownAsync(CancellationToken ct = default)
        => ExecuteQueueMutationAsync(
            SelectedQueueItem?.JobId,
            jobId => new MoveAgentPoolQueueItemDownCommand(jobId),
            jobId => $"Moving '{jobId}' down...",
            jobId => $"Queue item '{jobId}' moved down.",
            ct);

    /// <summary>Resolves and enqueues an ad-hoc prompt.</summary>
    public async Task EnqueueAdHocAsync(CancellationToken ct = default)
    {
        var prompt = (PromptInput ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(prompt))
        {
            StatusMessage = "Enter an ad-hoc prompt first.";
            return;
        }

        ErrorMessage = null;
        var request = new AgentPoolEnqueueDraft
        {
            AgentName = ResolveAgentNameOrNull(),
            Context = AgentPoolPromptContext.AdHoc,
            PromptText = prompt,
            UseWorkspaceContext = true
        };

        StatusMessage = "Resolving ad-hoc prompt...";
        var resolveResult = await _dispatcher.SendAsync(new ResolveAgentPoolPromptCommand(request), ct).ConfigureAwait(true);
        if (!resolveResult.IsSuccess || resolveResult.Value is null)
        {
            ErrorMessage = resolveResult.Error ?? "Resolve failed.";
            StatusMessage = "Resolve failed.";
            return;
        }

        if (!resolveResult.Value.Success)
        {
            ErrorMessage = resolveResult.Value.Error;
            StatusMessage = resolveResult.Value.Error ?? "Resolve failed.";
            return;
        }

        StatusMessage = "Queueing ad-hoc prompt...";
        var enqueueResult = await _dispatcher.SendAsync(new EnqueueAgentPoolPromptCommand(request), ct).ConfigureAwait(true);
        if (!enqueueResult.IsSuccess || enqueueResult.Value is null)
        {
            ErrorMessage = enqueueResult.Error ?? "Enqueue failed.";
            StatusMessage = "Enqueue failed.";
            return;
        }

        if (!enqueueResult.Value.Success)
        {
            ErrorMessage = enqueueResult.Value.Error;
            StatusMessage = enqueueResult.Value.Error ?? "Enqueue failed.";
            return;
        }

        PromptInput = string.Empty;
        await LoadAsync(ct).ConfigureAwait(true);
        StatusMessage = $"Queued '{enqueueResult.Value.JobId ?? "unknown"}' for agent '{enqueueResult.Value.AgentName ?? "auto"}'.";
    }

    partial void OnSelectedRuntimeIndexChanged(int value)
    {
        var selected = SelectedRuntimeAgent;
        if (selected is not null)
            AgentNameInput = selected.AgentName;
    }

    private async Task ExecuteAgentMutationAsync<TCommand>(
        string? agentName,
        Func<string, TCommand> commandFactory,
        Func<string, string> pendingMessage,
        Func<string, string> successMessage,
        CancellationToken ct)
        where TCommand : ICommand<AgentPoolMutationOutcome>
    {
        if (string.IsNullOrWhiteSpace(agentName))
        {
            StatusMessage = "Select an agent row first.";
            return;
        }

        ErrorMessage = null;
        StatusMessage = pendingMessage(agentName);
        var result = await _dispatcher.SendAsync(commandFactory(agentName), ct).ConfigureAwait(true);
        if (!result.IsSuccess || result.Value is null)
        {
            ErrorMessage = result.Error ?? "Operation failed.";
            StatusMessage = "Operation failed.";
            return;
        }

        if (!result.Value.Success)
        {
            ErrorMessage = result.Value.Error;
            StatusMessage = result.Value.Error ?? "Operation failed.";
            return;
        }

        await LoadAsync(ct).ConfigureAwait(true);
        StatusMessage = successMessage(agentName);
    }

    private async Task ExecuteQueueMutationAsync<TCommand>(
        string? jobId,
        Func<string, TCommand> commandFactory,
        Func<string, string> pendingMessage,
        Func<string, string> successMessage,
        CancellationToken ct)
        where TCommand : ICommand<AgentPoolMutationOutcome>
    {
        if (string.IsNullOrWhiteSpace(jobId))
        {
            StatusMessage = "Select a queue item first.";
            return;
        }

        ErrorMessage = null;
        StatusMessage = pendingMessage(jobId);
        var result = await _dispatcher.SendAsync(commandFactory(jobId), ct).ConfigureAwait(true);
        if (!result.IsSuccess || result.Value is null)
        {
            ErrorMessage = result.Error ?? "Operation failed.";
            StatusMessage = "Operation failed.";
            return;
        }

        if (!result.Value.Success)
        {
            ErrorMessage = result.Value.Error;
            StatusMessage = result.Value.Error ?? "Operation failed.";
            return;
        }

        await LoadAsync(ct).ConfigureAwait(true);
        StatusMessage = successMessage(jobId);
    }

    private string? ResolveAgentNameOrNull()
    {
        var typed = (AgentNameInput ?? string.Empty).Trim();
        if (!string.IsNullOrWhiteSpace(typed))
            return typed;
        return SelectedRuntimeAgent?.AgentName;
    }

    private static void ReplaceCollection<T>(ObservableCollection<T> target, IReadOnlyList<T> items)
    {
        target.Clear();
        foreach (var item in items)
            target.Add(item);
    }
}
