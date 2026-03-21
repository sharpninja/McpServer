namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Prepares and finalizes the effective git branch used by an agent execution session.
/// </summary>
public interface IAgentBranchStrategy
{
    /// <summary>
    /// Gets the normalized strategy name handled by this implementation.
    /// </summary>
    string StrategyName { get; }

    /// <summary>
    /// Prepares the effective branch context for agent execution.
    /// </summary>
    /// <param name="workDirectory">The effective working directory for the agent.</param>
    /// <param name="agentId">The logical agent identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    /// <returns>The effective branch name for the launch session.</returns>
    Task<string?> PrepareBranchAsync(string workDirectory, string agentId, CancellationToken ct = default);

    /// <summary>
    /// Finalizes any branch-related state after agent execution completes.
    /// </summary>
    /// <param name="workDirectory">The effective working directory for the agent.</param>
    /// <param name="agentId">The logical agent identifier.</param>
    /// <param name="ct">A cancellation token.</param>
    Task FinalizeBranchAsync(string workDirectory, string agentId, CancellationToken ct = default);
}
