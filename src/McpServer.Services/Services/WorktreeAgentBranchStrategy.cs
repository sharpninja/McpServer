namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Uses the branch already created by worktree isolation.
/// </summary>
public sealed class WorktreeAgentBranchStrategy : IAgentBranchStrategy
{
    /// <summary>
    /// Canonical mode name for worktree branch strategy.
    /// </summary>
    public const string ModeName = "worktree";

    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="WorktreeAgentBranchStrategy"/> class.
    /// </summary>
    public WorktreeAgentBranchStrategy(IProcessRunner processRunner)
    {
        _processRunner = processRunner;
    }

    /// <inheritdoc/>
    public string StrategyName => ModeName;

    /// <inheritdoc/>
    public async Task<string?> PrepareBranchAsync(string workDirectory, string agentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        var result = await _processRunner.RunAsync(
            new ProcessRunRequest("git", "rev-parse --abbrev-ref HEAD", WorkingDirectory: workDirectory),
            ct).ConfigureAwait(false);
        return result.ExitCode == 0 ? result.Stdout?.Trim() : null;
    }

    /// <inheritdoc/>
    public Task FinalizeBranchAsync(string workDirectory, string agentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
