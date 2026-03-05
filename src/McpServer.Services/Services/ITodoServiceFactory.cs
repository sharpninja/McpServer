namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Creates <see cref="ITodoService"/> instances for primary and workspace-scoped usage.
/// </summary>
public interface ITodoServiceFactory
{
    /// <summary>Creates the primary workspace TODO service instance.</summary>
    /// <returns>The primary <see cref="ITodoService"/>.</returns>
    ITodoService CreatePrimary();

    /// <summary>Creates a workspace-specific TODO service instance.</summary>
    /// <param name="workspacePath">Absolute workspace path used for data resolution.</param>
    /// <param name="workspaceContext">Resolved workspace context metadata.</param>
    /// <returns>A workspace-specific <see cref="ITodoService"/>.</returns>
    ITodoService CreateForWorkspace(string workspacePath, WorkspaceContext workspaceContext);
}
