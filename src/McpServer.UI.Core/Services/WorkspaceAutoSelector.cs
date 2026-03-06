// Copyright (c) 2025 McpServer Contributors. All rights reserved.

using McpServer.Cqrs;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.ViewModels;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Shared workspace auto-selection logic used by both Director and Web hosts.
/// Queries workspaces via CQRS and selects the preferred one using the priority:
/// CWD match → Primary → First enabled.
/// </summary>
public sealed class WorkspaceAutoSelector
{
    private readonly Dispatcher _dispatcher;
    private readonly WorkspaceContextViewModel _workspaceContext;
    private readonly ILogger<WorkspaceAutoSelector> _logger;
    private Task? _autoSelectTask;
    private readonly object _gate = new();

    /// <summary>Initializes a new instance of the <see cref="WorkspaceAutoSelector"/> class.</summary>
    /// <param name="dispatcher">CQRS dispatcher for querying workspaces.</param>
    /// <param name="workspaceContext">Singleton workspace context ViewModel.</param>
    /// <param name="logger">Logger instance.</param>
    public WorkspaceAutoSelector(
        Dispatcher dispatcher,
        WorkspaceContextViewModel workspaceContext,
        ILogger<WorkspaceAutoSelector> logger)
    {
        _dispatcher = dispatcher;
        _workspaceContext = workspaceContext;
        _logger = logger;
    }

    /// <summary>
    /// Selects the preferred workspace from a loaded list.
    /// Priority: CWD match → primary → first enabled.
    /// </summary>
    /// <param name="workspaces">Available workspaces.</param>
    /// <returns>The preferred workspace path, or <see langword="null"/> if none available.</returns>
    public static string? SelectPreferred(IReadOnlyList<WorkspaceSummary> workspaces)
    {
        if (workspaces.Count == 0)
            return null;

        var cwd = Directory.GetCurrentDirectory();

        return workspaces.FirstOrDefault(w =>
                   w.IsEnabled &&
                   string.Equals(w.WorkspacePath, cwd, StringComparison.OrdinalIgnoreCase))?.WorkspacePath
               ?? workspaces.FirstOrDefault(w => w.IsPrimary && w.IsEnabled)?.WorkspacePath
               ?? workspaces.FirstOrDefault(w => w.IsEnabled)?.WorkspacePath;
    }

    /// <summary>
    /// Auto-selects a workspace if none is currently active.
    /// Thread-safe; only executes once. Subsequent calls return the cached task.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The selected workspace path, or <see langword="null"/> if selection failed.</returns>
    public Task<string?> EnsureWorkspaceSelectedAsync(CancellationToken cancellationToken = default)
    {
        if (!string.IsNullOrWhiteSpace(_workspaceContext.ActiveWorkspacePath))
            return Task.FromResult<string?>(_workspaceContext.ActiveWorkspacePath);

        Task task;
        lock (_gate)
        {
            _autoSelectTask ??= AutoSelectCoreAsync(cancellationToken);
            task = _autoSelectTask;
        }

        return (Task<string?>)task;
    }

    private async Task<string?> AutoSelectCoreAsync(CancellationToken cancellationToken)
    {
        try
        {
            var result = await _dispatcher.QueryAsync(new ListWorkspacesQuery(), cancellationToken).ConfigureAwait(true);
            if (!result.IsSuccess || result.Value is null || result.Value.Items.Count == 0)
            {
                _logger.LogWarning("Workspace auto-selection: no workspaces available.");
                return null;
            }

            var preferred = SelectPreferred(result.Value.Items);
            if (string.IsNullOrWhiteSpace(preferred))
            {
                _logger.LogWarning("Workspace auto-selection: no enabled workspace found.");
                return null;
            }

            _workspaceContext.ActiveWorkspacePath = preferred;
            _logger.LogInformation("Workspace auto-selected: {WorkspacePath}", preferred);
            return preferred;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Workspace auto-selection failed.");
            return null;
        }
    }
}
