using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Abstraction for workspace REST operations used by UI.Core CQRS handlers.
/// Implementations are provided by the hosting shell (for example, Director).
/// </summary>
public interface IWorkspaceApiClient
{
    /// <summary>
    /// Lists all registered workspaces.
    /// </summary>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The workspace list result.</returns>
    Task<ListWorkspacesResult> ListWorkspacesAsync(CancellationToken ct = default);

    /// <summary>
    /// Gets a single workspace by its absolute workspace path.
    /// </summary>
    /// <param name="workspacePath">Absolute workspace path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The workspace detail, or <see langword="null"/> when not found.</returns>
    Task<WorkspaceDetail?> GetWorkspaceAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Updates a workspace's compliance policy (ban lists).
    /// </summary>
    /// <param name="command">Policy update command payload.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when the server reports success; otherwise <see langword="false"/>.</returns>
    Task<bool> UpdateWorkspacePolicyAsync(UpdateWorkspacePolicyCommand command, CancellationToken ct = default);

    /// <summary>
    /// Runs the Director workspace-initialization workflow for the specified workspace.
    /// </summary>
    /// <param name="workspacePath">Absolute workspace path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Initialization summary.</returns>
    Task<WorkspaceInitInfo> InitWorkspaceAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Creates/registers a new workspace.
    /// </summary>
    Task<WorkspaceMutationOutcome> CreateWorkspaceAsync(CreateWorkspaceCommand command, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing workspace.
    /// </summary>
    Task<WorkspaceMutationOutcome> UpdateWorkspaceAsync(UpdateWorkspaceCommand command, CancellationToken ct = default);

    /// <summary>
    /// Deletes an existing workspace.
    /// </summary>
    Task<WorkspaceMutationOutcome> DeleteWorkspaceAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Starts a workspace host.
    /// </summary>
    Task<WorkspaceRuntimeStatus> StartWorkspaceAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Stops a workspace host.
    /// </summary>
    Task<WorkspaceRuntimeStatus> StopWorkspaceAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Gets workspace host status.
    /// </summary>
    Task<WorkspaceRuntimeStatus> GetWorkspaceStatusAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Gets the global marker prompt template.
    /// </summary>
    Task<GlobalPromptInfo> GetGlobalPromptAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the global marker prompt template.
    /// </summary>
    Task<GlobalPromptInfo> UpdateGlobalPromptAsync(UpdateGlobalPromptCommand command, CancellationToken ct = default);
}
