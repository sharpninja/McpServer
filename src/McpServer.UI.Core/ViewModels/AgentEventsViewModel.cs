using CommunityToolkit.Mvvm.ComponentModel;
using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for agent event-log browsing and logging operations.
/// </summary>
[ViewModelCommand("agent-events", Description = "List and append agent events")]
public sealed partial class AgentEventsViewModel : AreaListViewModelBase<AgentEventItem>
{
    private readonly Dispatcher _dispatcher;
    private readonly WorkspaceContextViewModel _workspaceContext;
    private readonly ILogger<AgentEventsViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentEventsViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context.</param>
    /// <param name="logger">Logger instance.</param>
    public AgentEventsViewModel(
        Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<AgentEventsViewModel> logger)
        : base(McpArea.Agents)
    {
        _dispatcher = dispatcher;
        _workspaceContext = workspaceContext;
        _logger = logger;
    }

    /// <summary>Agent identifier currently used for event queries.</summary>
    [ObservableProperty]
    private string? _agentId;

    /// <summary>Maximum number of events to fetch per query.</summary>
    [ObservableProperty]
    private int _limit = 50;

    /// <summary>
    /// Loads event history for an agent.
    /// </summary>
    /// <param name="agentId">Optional agent ID override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Query result on success, otherwise null.</returns>
    public async Task<AgentEventsResult?> LoadAsync(string? agentId = null, CancellationToken ct = default)
    {
        var effectiveAgent = string.IsNullOrWhiteSpace(agentId) ? AgentId : agentId;
        if (string.IsNullOrWhiteSpace(effectiveAgent))
        {
            ErrorMessage = "Agent ID is required.";
            return null;
        }

        AgentId = effectiveAgent;
        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = $"Loading events for '{effectiveAgent}'...";
        try
        {
            var result = await _dispatcher.QueryAsync(
                new GetAgentEventsQuery(
                    effectiveAgent,
                    _workspaceContext.ActiveWorkspacePath,
                    Limit <= 0 ? 50 : Limit),
                ct).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to load agent events.";
                StatusMessage = "Agent event load failed.";
                return null;
            }

            SetItems(result.Value.Items, result.Value.TotalCount);
            StatusMessage = $"Loaded {Items.Count} events for '{effectiveAgent}'.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Agent event load failed.";
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }

    /// <summary>
    /// Appends a new event for an agent.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="eventType">Numeric event type.</param>
    /// <param name="details">Optional details payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Mutation outcome on success, otherwise null.</returns>
    public async Task<AgentMutationOutcome?> LogAsync(
        string agentId,
        int eventType,
        string? details = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(agentId))
        {
            ErrorMessage = "Agent ID is required.";
            return null;
        }

        IsLoading = true;
        ErrorMessage = null;
        StatusMessage = $"Logging event for '{agentId}'...";
        try
        {
            var result = await _dispatcher.SendAsync(new LogAgentEventCommand
            {
                AgentId = agentId,
                EventType = eventType,
                Details = details,
                WorkspacePath = _workspaceContext.ActiveWorkspacePath
            }, ct).ConfigureAwait(false);

            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Failed to log event.";
                StatusMessage = "Agent event log failed.";
                return null;
            }

            if (!result.Value.Success)
            {
                ErrorMessage = result.Value.Error ?? "Failed to log event.";
                StatusMessage = "Agent event log failed.";
                return result.Value;
            }

            StatusMessage = $"Event logged for '{agentId}'.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Agent event log failed.";
            return null;
        }
        finally
        {
            IsLoading = false;
        }
    }
}
