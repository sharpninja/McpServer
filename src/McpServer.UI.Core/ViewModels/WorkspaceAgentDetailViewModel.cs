using McpServer.Cqrs;
using McpServer.Cqrs.Mvvm;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels.Base;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.ViewModels;

/// <summary>
/// ViewModel for workspace agent assignment detail and mutation workflows.
/// </summary>
[ViewModelCommand("workspace-agent-detail", Description = "Get/update workspace agent assignment")]
public sealed class WorkspaceAgentDetailViewModel : AreaDetailViewModelBase<WorkspaceAgentDetail>
{
    private readonly Dispatcher _dispatcher;
    private readonly WorkspaceContextViewModel _workspaceContext;
    private readonly ILogger<WorkspaceAgentDetailViewModel> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorkspaceAgentDetailViewModel"/> class.
    /// </summary>
    /// <param name="dispatcher">CQRS dispatcher.</param>
    /// <param name="workspaceContext">Shared workspace context.</param>
    /// <param name="logger">Logger instance.</param>
    public WorkspaceAgentDetailViewModel(
        Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<WorkspaceAgentDetailViewModel> logger)
        : base(McpArea.Agents)
    {
        _dispatcher = dispatcher;
        _workspaceContext = workspaceContext;
        _logger = logger;
    }

    /// <summary>Current active workspace path from the shared context.</summary>
    public string? ActiveWorkspacePath => _workspaceContext.ActiveWorkspacePath;

    /// <summary>
    /// Loads a workspace agent assignment.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="workspacePath">Optional workspace path override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The loaded detail, if found.</returns>
    public async Task<WorkspaceAgentDetail?> LoadAsync(string agentId, string? workspacePath = null, CancellationToken ct = default)
    {
        var targetWorkspace = ResolveWorkspacePath(workspacePath);
        if (string.IsNullOrWhiteSpace(agentId))
        {
            ErrorMessage = "Agent ID is required.";
            return null;
        }
        if (string.IsNullOrWhiteSpace(targetWorkspace))
        {
            ErrorMessage = "No active workspace selected.";
            return null;
        }

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = $"Loading workspace agent '{agentId}'...";
        try
        {
            var result = await _dispatcher.QueryAsync(new GetWorkspaceAgentQuery(agentId, targetWorkspace), ct).ConfigureAwait(false);
            if (!result.IsSuccess)
            {
                ErrorMessage = result.Error ?? "Failed to load workspace agent detail.";
                StatusMessage = "Workspace agent load failed.";
                return null;
            }

            Detail = result.Value;
            LastUpdatedAt = DateTimeOffset.UtcNow;
            StatusMessage = result.Value is null
                ? $"Workspace agent '{agentId}' was not found."
                : $"Loaded workspace agent '{agentId}'.";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Workspace agent load failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Assigns a global definition to the target workspace (enabled by default).
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="workspacePath">Optional workspace path override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mutation outcome, if dispatch succeeded.</returns>
    public async Task<AgentMutationOutcome?> AssignAsync(string agentId, string? workspacePath = null, CancellationToken ct = default)
    {
        var targetWorkspace = ResolveWorkspacePath(workspacePath);
        if (string.IsNullOrWhiteSpace(agentId))
        {
            ErrorMessage = "Agent ID is required.";
            return null;
        }
        if (string.IsNullOrWhiteSpace(targetWorkspace))
        {
            ErrorMessage = "No active workspace selected.";
            return null;
        }

        return await SendMutationAsync(
            new AssignWorkspaceAgentCommand
            {
                AgentId = agentId,
                WorkspacePath = targetWorkspace,
                Enabled = true,
                AgentIsolation = "worktree"
            },
            $"Assigning '{agentId}'...",
            $"Assigned '{agentId}'.",
            ct).ConfigureAwait(false);
    }

    /// <summary>
    /// Saves workspace assignment details.
    /// </summary>
    /// <param name="command">Upsert command payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mutation outcome, if dispatch succeeded.</returns>
    public async Task<AgentMutationOutcome?> UpsertAsync(UpsertWorkspaceAgentCommand command, CancellationToken ct = default)
    {
        var targetWorkspace = ResolveWorkspacePath(command.WorkspacePath);
        if (string.IsNullOrWhiteSpace(command.AgentId))
        {
            ErrorMessage = "Agent ID is required.";
            return null;
        }
        if (string.IsNullOrWhiteSpace(targetWorkspace))
        {
            ErrorMessage = "No active workspace selected.";
            return null;
        }

        command = command with { WorkspacePath = targetWorkspace };
        var outcome = await SendMutationAsync(
            command,
            $"Saving '{command.AgentId}'...",
            $"Saved '{command.AgentId}'.",
            ct).ConfigureAwait(false);

        if (outcome is { Success: true })
            await LoadAsync(command.AgentId, targetWorkspace, ct).ConfigureAwait(false);

        return outcome;
    }

    /// <summary>
    /// Deletes a workspace assignment.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="workspacePath">Optional workspace path override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mutation outcome, if dispatch succeeded.</returns>
    public Task<AgentMutationOutcome?> DeleteAsync(string agentId, string? workspacePath = null, CancellationToken ct = default)
    {
        var targetWorkspace = ResolveWorkspacePath(workspacePath);
        return SendMutationAsync(
            new DeleteWorkspaceAgentCommand(agentId, targetWorkspace),
            $"Deleting '{agentId}'...",
            $"Deleted '{agentId}'.",
            ct);
    }

    /// <summary>
    /// Bans a workspace assignment.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="reason">Optional ban reason.</param>
    /// <param name="workspacePath">Optional workspace path override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mutation outcome, if dispatch succeeded.</returns>
    public Task<AgentMutationOutcome?> BanAsync(
        string agentId,
        string? reason,
        string? workspacePath = null,
        CancellationToken ct = default)
    {
        var targetWorkspace = ResolveWorkspacePath(workspacePath);
        return SendMutationAsync(
            new BanAgentCommand
            {
                AgentId = agentId,
                Reason = reason,
                Global = false,
                WorkspacePath = targetWorkspace
            },
            $"Banning '{agentId}'...",
            $"Banned '{agentId}'.",
            ct);
    }

    /// <summary>
    /// Unbans a workspace assignment.
    /// </summary>
    /// <param name="agentId">Agent identifier.</param>
    /// <param name="workspacePath">Optional workspace path override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The mutation outcome, if dispatch succeeded.</returns>
    public Task<AgentMutationOutcome?> UnbanAsync(string agentId, string? workspacePath = null, CancellationToken ct = default)
    {
        var targetWorkspace = ResolveWorkspacePath(workspacePath);
        return SendMutationAsync(
            new UnbanAgentCommand(agentId, targetWorkspace, false),
            $"Unbanning '{agentId}'...",
            $"Unbanned '{agentId}'.",
            ct);
    }

    /// <summary>
    /// Validates the current workspace agents.yaml.
    /// </summary>
    /// <param name="workspacePath">Optional workspace path override.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Validation outcome on success, otherwise null.</returns>
    public async Task<AgentValidateOutcome?> ValidateAsync(string? workspacePath = null, CancellationToken ct = default)
    {
        var targetWorkspace = ResolveWorkspacePath(workspacePath);
        if (string.IsNullOrWhiteSpace(targetWorkspace))
        {
            ErrorMessage = "No active workspace selected.";
            return null;
        }

        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = "Validating agents.yaml...";
        try
        {
            var result = await _dispatcher.QueryAsync(new ValidateAgentQuery(targetWorkspace), ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Validation failed.";
                StatusMessage = "Validation failed.";
                return null;
            }

            StatusMessage = result.Value.Valid
                ? "agents.yaml is valid."
                : $"Validation failed: {result.Value.Error ?? "unknown"}";
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Validation failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private string? ResolveWorkspacePath(string? workspacePath)
        => string.IsNullOrWhiteSpace(workspacePath) ? ActiveWorkspacePath : workspacePath;

    private async Task<AgentMutationOutcome?> SendMutationAsync<TCommand>(
        TCommand command,
        string pendingStatus,
        string successStatus,
        CancellationToken ct)
        where TCommand : ICommand<AgentMutationOutcome>
    {
        IsBusy = true;
        ErrorMessage = null;
        StatusMessage = pendingStatus;
        try
        {
            var result = await _dispatcher.SendAsync(command, ct).ConfigureAwait(false);
            if (!result.IsSuccess || result.Value is null)
            {
                ErrorMessage = result.Error ?? "Mutation failed.";
                StatusMessage = "Mutation failed.";
                return null;
            }

            if (!result.Value.Success)
            {
                ErrorMessage = result.Value.Error ?? "Mutation failed.";
                StatusMessage = "Mutation failed.";
                return result.Value;
            }

            StatusMessage = successStatus;
            return result.Value;
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            ErrorMessage = ex.Message;
            StatusMessage = "Mutation failed.";
            return null;
        }
        finally
        {
            IsBusy = false;
        }
    }
}
