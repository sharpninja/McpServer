namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Uses the current branch in the working directory without creating a new branch.
/// </summary>
public sealed class DirectAgentBranchStrategy : IAgentBranchStrategy
{
    /// <summary>
    /// Canonical mode name for direct branch usage.
    /// </summary>
    public const string ModeName = "direct";

    private readonly IProcessRunner _processRunner;

    /// <summary>
    /// Initializes a new instance of the <see cref="DirectAgentBranchStrategy"/> class.
    /// </summary>
    public DirectAgentBranchStrategy(IProcessRunner processRunner)
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
