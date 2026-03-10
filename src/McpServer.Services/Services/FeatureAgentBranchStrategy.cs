using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Creates a dedicated feature branch for agent execution and restores the original branch on finalize.
/// </summary>
public sealed class FeatureAgentBranchStrategy : IAgentBranchStrategy
{
    /// <summary>
    /// Canonical mode name for feature branch strategy.
    /// </summary>
    public const string ModeName = "feature-branch";

    private readonly IProcessRunner _processRunner;
    private readonly ILogger<FeatureAgentBranchStrategy> _logger;
    private readonly Dictionary<string, string> _originalBranches = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Initializes a new instance of the <see cref="FeatureAgentBranchStrategy"/> class.
    /// </summary>
    public FeatureAgentBranchStrategy(IProcessRunner processRunner, ILogger<FeatureAgentBranchStrategy> logger)
    {
        _processRunner = processRunner;
        _logger = logger;
    }

    /// <inheritdoc/>
    public string StrategyName => ModeName;

    /// <inheritdoc/>
    public async Task<string?> PrepareBranchAsync(string workDirectory, string agentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var originalBranchResult = await _processRunner.RunAsync(
            new ProcessRunRequest("git", "rev-parse --abbrev-ref HEAD", WorkingDirectory: workDirectory),
            ct).ConfigureAwait(false);
        if (originalBranchResult.ExitCode != 0)
            throw new InvalidOperationException($"Failed to resolve current branch for agent '{agentId}': {originalBranchResult.Stderr}");

        var originalBranch = originalBranchResult.Stdout?.Trim();
        var featureBranch = $"agent/{agentId}/{DateTime.UtcNow:yyyyMMdd-HHmmss}";
        var checkoutResult = await _processRunner.RunAsync(
            new ProcessRunRequest("git", $"checkout -b \"{featureBranch}\"", WorkingDirectory: workDirectory),
            ct).ConfigureAwait(false);
        if (checkoutResult.ExitCode != 0)
            throw new InvalidOperationException($"Failed to create feature branch for agent '{agentId}': {checkoutResult.Stderr}");

        if (!string.IsNullOrWhiteSpace(originalBranch))
            _originalBranches[NormalizePath(workDirectory)] = originalBranch;

        _logger.LogInformation("Created feature branch {BranchName} for agent {AgentId}", featureBranch, agentId);
        return featureBranch;
    }

    /// <inheritdoc/>
    public async Task FinalizeBranchAsync(string workDirectory, string agentId, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();

        var normalizedWorkDirectory = NormalizePath(workDirectory);
        if (!_originalBranches.TryGetValue(normalizedWorkDirectory, out var originalBranch) || string.IsNullOrWhiteSpace(originalBranch))
            return;

        var checkoutResult = await _processRunner.RunAsync(
            new ProcessRunRequest("git", $"checkout \"{originalBranch}\"", WorkingDirectory: workDirectory),
            ct).ConfigureAwait(false);
        if (checkoutResult.ExitCode == 0)
        {
            _originalBranches.Remove(normalizedWorkDirectory);
            _logger.LogInformation("Restored original branch {BranchName} for agent {AgentId}", originalBranch, agentId);
            return;
        }

        _logger.LogWarning(
            "Failed to restore original branch {BranchName} for agent {AgentId}: {Error}",
            originalBranch,
            agentId,
            checkoutResult.Stderr);
    }

    private static string NormalizePath(string path)
        => Path.GetFullPath(path.Trim().TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
}
