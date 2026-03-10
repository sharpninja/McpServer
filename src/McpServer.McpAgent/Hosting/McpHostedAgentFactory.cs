using Microsoft.Extensions.DependencyInjection;

namespace McpServer.McpAgent.Hosting;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-006: Default dependency-injection-backed implementation of
/// <see cref="IMcpHostedAgentFactory"/>.
/// </summary>
public sealed class McpHostedAgentFactory : IMcpHostedAgentFactory
{
    private readonly IServiceProvider _serviceProvider;

    /// <summary>
    /// Initializes a new <see cref="McpHostedAgentFactory"/> that resolves hosted-agent instances
    /// from the configured dependency-injection container.
    /// </summary>
    /// <param name="serviceProvider">The root service provider used to resolve hosted-agent instances.</param>
    public McpHostedAgentFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
    }

    /// <inheritdoc />
    public IMcpHostedAgent CreateHostedAgent() =>
        _serviceProvider.GetRequiredService<IMcpHostedAgent>();

    /// <inheritdoc />
    public McpHostedAgentRegistration CreateRegistration() =>
        CreateHostedAgent().Registration;
}
