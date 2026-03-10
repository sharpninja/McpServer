using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// Resolves the effective agent isolation strategy from DI by configured mode.
/// </summary>
public sealed class AgentIsolationStrategyResolver
{
    private readonly IReadOnlyDictionary<string, IAgentIsolationStrategy> _strategies;

    /// <summary>
    /// Initializes a new instance of the <see cref="AgentIsolationStrategyResolver"/> class.
    /// </summary>
    /// <param name="strategies">The registered isolation strategies.</param>
    public AgentIsolationStrategyResolver(IEnumerable<IAgentIsolationStrategy> strategies)
    {
        ArgumentNullException.ThrowIfNull(strategies);
        _strategies = strategies.ToDictionary(x => x.StrategyName, StringComparer.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Resolves an isolation strategy by mode.
    /// </summary>
    /// <param name="isolationMode">The configured isolation mode.</param>
    /// <returns>The matching strategy instance.</returns>
    public IAgentIsolationStrategy Resolve(string? isolationMode)
    {
        var mode = string.IsNullOrWhiteSpace(isolationMode)
            ? NoneAgentIsolationStrategy.ModeName
            : isolationMode.Trim();

        if (_strategies.TryGetValue(mode, out var strategy))
            return strategy;

        throw new ArgumentException($"Unsupported agent isolation mode '{mode}'.", nameof(isolationMode));
    }
}
