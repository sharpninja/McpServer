namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Prepares and cleans up agent-specific working directories for workspace execution isolation.
/// </summary>
public interface IAgentIsolationStrategy
{
    /// <summary>
    /// Gets the normalized strategy name handled by this implementation.
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Prepares an effective working directory for the specified workspace and agent.
    /// </summary>
    /// <param name="workspacePath">The owning workspace path.</param>
    /// <param name="agentId">The logical agent identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The effective working directory path.</returns>
    Task<string> PrepareWorkDirectoryAsync(string workspacePath, string agentId, CancellationToken ct = default);

    /// <summary>
    /// Cleans up any resources created for the specified workspace and agent.
    /// </summary>
    /// <param name="workspacePath">The owning workspace path.</param>
    /// <param name="agentId">The logical agent identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    Task CleanupAsync(string workspacePath, string agentId, CancellationToken ct = default);
}
