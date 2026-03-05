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
    /// Creates a new workspace registration.
    /// </summary>
    Task<WorkspaceMutationOutcome> CreateWorkspaceAsync(CreateWorkspaceCommand command, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing workspace registration.
    /// </summary>
    Task<WorkspaceMutationOutcome> UpdateWorkspaceAsync(UpdateWorkspaceCommand command, CancellationToken ct = default);

    /// <summary>
    /// Deletes a workspace registration.
    /// </summary>
    Task<WorkspaceMutationOutcome> DeleteWorkspaceAsync(DeleteWorkspaceCommand command, CancellationToken ct = default);

    /// <summary>
    /// Gets the workspace host process state.
    /// </summary>
    Task<WorkspaceProcessState> GetWorkspaceStatusAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Starts the workspace host process.
    /// </summary>
    Task<WorkspaceProcessState> StartWorkspaceAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Stops the workspace host process.
    /// </summary>
    Task<WorkspaceProcessState> StopWorkspaceAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Probes the health endpoint for the specified workspace host.
    /// </summary>
    Task<WorkspaceHealthState> CheckWorkspaceHealthAsync(string workspacePath, CancellationToken ct = default);

    /// <summary>
    /// Reads the shared global marker prompt template.
    /// </summary>
    Task<WorkspaceGlobalPromptState> GetWorkspaceGlobalPromptAsync(CancellationToken ct = default);

    /// <summary>
    /// Updates the shared global marker prompt template.
    /// </summary>
    Task<WorkspaceGlobalPromptState> UpdateWorkspaceGlobalPromptAsync(UpdateWorkspaceGlobalPromptCommand command, CancellationToken ct = default);

    /// <summary>
    /// Runs the Director workspace-initialization workflow for the specified workspace.
    /// </summary>
    /// <param name="workspacePath">Absolute workspace path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>Initialization summary.</returns>
    Task<WorkspaceInitInfo> InitWorkspaceAsync(string workspacePath, CancellationToken ct = default);
}
