namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Resolves the effective agent branch strategy from DI by configured mode.
/// </summary>
public sealed class AgentBranchStrategyResolver
{
    private readonly IReadOnlyDictionary<string, IAgentBranchStrategy> _strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentBranchStrategyResolver"/> class.
    /// </summary>
    /// <param name="strategies">The registered branch strategies.</param>
    public AgentBranchStrategyResolver(IEnumerable<IAgentBranchStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        _strategies = strategies.ToDictionary(x => x.StrategyName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves a branch strategy by mode.
    /// </summary>
    /// <param name="branchStrategy">The configured branch strategy.</param>
    /// <returns>The matching strategy instance.</returns>
    public IAgentBranchStrategy Resolve(string? branchStrategy)
    {
        var mode = string.IsNullOrWhiteSpace(branchStrategy)
            ? DirectAgentBranchStrategy.ModeName
            : branchStrategy.Trim();

        if (_strategies.TryGetValue(mode, out var strategy))
            return strategy;

        throw new ArgumentException($"Unsupported agent branch strategy '{mode}'.", nameof(branchStrategy));
    }
}
