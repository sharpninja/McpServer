using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Manages runtime agent processes keyed by workspace and agent identifier.
/// </summary>
public interface IAgentProcessManager
{
    /// <summary>
    /// Launches an agent process for the specified workspace and agent identifier.
    /// </summary>
    /// <param name="workspacePath">Owning workspace path.</param>
    /// <param name="agentId">Logical agent identifier.</param>
    /// <param name="resolvedCommand">Fully resolved launch command line.</param>
    /// <param name="workDirectory">Effective working directory for the process.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The resulting runtime process information.</returns>
    Task<AgentProcessInfo> LaunchAsync(string workspacePath, string agentId, string resolvedCommand, string workDirectory, CancellationToken ct = default);

    /// <summary>
    /// Stops a running agent process if one exists.
    /// </summary>
    /// <param name="workspacePath">Owning workspace path.</param>
    /// <param name="agentId">Logical agent identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns><see langword="true"/> when a running process was stopped; otherwise <see langword="false"/>.</returns>
    Task<bool> StopAsync(string workspacePath, string agentId, CancellationToken ct = default);

    /// <summary>
    /// Gets current runtime status for a workspace/agent pair.
    /// </summary>
    /// <param name="workspacePath">Owning workspace path.</param>
    /// <param name="agentId">Logical agent identifier.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The current process info when tracked; otherwise <see langword="null"/>.</returns>
    Task<AgentProcessInfo?> GetStatusAsync(string workspacePath, string agentId, CancellationToken ct = default);

    /// <summary>
    /// Lists tracked agent processes, optionally filtered to a single workspace.
    /// </summary>
    /// <param name="workspacePath">Optional workspace path filter.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A read-only list of tracked process information.</returns>
    Task<IReadOnlyList<AgentProcessInfo>> ListRunningAsync(string? workspacePath = null, CancellationToken ct = default);
}
