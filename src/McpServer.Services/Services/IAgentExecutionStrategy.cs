using McpServer.Common.Copilot;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Services;

internal static class AgentExecutionStrategyNames
{
    public const string CopilotCli = "copilot-cli";
    public const string HostedMcpAgent = "hosted-mcp-agent";
    public const string HostedAgentFrameworkLegacy = "hosted-agentframework";

    public static IReadOnlyList<string> SupportedNames { get; } =
    [
        CopilotCli,
        HostedMcpAgent,
    ];

    public static bool IsSupported(string? strategyName)
    {
        var normalized = NormalizeOrDefault(strategyName);
        return SupportedNames.Contains(normalized, StringComparer.OrdinalIgnoreCase);
    }

    public static string NormalizeOrDefault(string? strategyName) =>
        string.IsNullOrWhiteSpace(strategyName)
            ? CopilotCli
            : NormalizeAlias(strategyName.Trim());

    private static string NormalizeAlias(string strategyName)
    {
        return string.Equals(strategyName, HostedAgentFrameworkLegacy, StringComparison.OrdinalIgnoreCase)
            ? HostedMcpAgent
            : strategyName;
    }
}

internal sealed record AgentExecutionSessionRequest(
    string InitialPrompt,
    string WorkspacePath,
    string? AgentName,
    string ExecutionStrategy,
    CopilotClientOptions Options);

internal interface IAgentExecutionSession : IAsyncDisposable
{
    bool IsAlive { get; }

    int? ProcessId { get; }

    Task<CopilotResult> ReadInitialResponseAsync(CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> ReadInitialResponseStreamingAsync(CancellationToken cancellationToken = default);

    Task<CopilotResult> SendAsync(string prompt, CancellationToken cancellationToken = default);

    IAsyncEnumerable<string> SendStreamingAsync(string prompt, CancellationToken cancellationToken = default);

    Task SendEscapeAsync(CancellationToken cancellationToken = default);

    Task EndAsync(TimeSpan timeout);
}

internal interface IAgentExecutionStrategy
{
    string Name { get; }

    ValueTask<IAgentExecutionSession> CreateSessionAsync(
        AgentExecutionSessionRequest request,
        CancellationToken cancellationToken = default);
}

internal interface IAgentExecutionStrategyResolver
{
    IAgentExecutionStrategy Resolve(string? strategyName);
}

internal sealed class AgentExecutionStrategyResolver(IEnumerable<IAgentExecutionStrategy> strategies)
    : IAgentExecutionStrategyResolver
{
    private readonly Dictionary<string, IAgentExecutionStrategy> _strategies = strategies
        .ToDictionary(static strategy => strategy.Name, StringComparer.OrdinalIgnoreCase);

    public IAgentExecutionStrategy Resolve(string? strategyName)
    {
        var normalized = AgentExecutionStrategyNames.NormalizeOrDefault(strategyName);
        if (_strategies.TryGetValue(normalized, out var strategy))
            return strategy;

        throw new ArgumentException(
            $"Unsupported agent execution strategy '{strategyName}'. Supported values: {string.Join(", ", AgentExecutionStrategyNames.SupportedNames)}.",
            nameof(strategyName));
    }
}

/// <summary>
/// FR-MCP-052..058: Registers the pluggable agent execution strategies used by the voice
/// conversation and agent-pool services.
/// </summary>
public static class AgentExecutionServiceCollectionExtensions
{
    /// <summary>
    /// FR-MCP-052..058: Adds the default agent execution strategy set, including the legacy
    /// Copilot CLI backend and the hosted MCP Agent backend.
    /// </summary>
    /// <param name="services">The service collection receiving the strategy registrations.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddAgentExecutionStrategies(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<IAgentExecutionStrategy, CopilotCliAgentExecutionStrategy>();
        services.AddSingleton<IAgentExecutionStrategy, HostedMcpAgentExecutionStrategy>();
        services.AddSingleton<IAgentExecutionStrategyResolver, AgentExecutionStrategyResolver>();
        return services;
    }
}

