using McpServer.AgentFramework.AgentFramework;
using McpServer.AgentFramework.SessionLog;
using McpServer.AgentFramework.Todo;
using McpServer.Client;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace McpServer.AgentFramework;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-006: Dependency injection extensions for the hosted Agent Framework
/// registration surface, including the built-in session-log and TODO workflow services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the MCP Server hosted-agent surface using externally configured
    /// <see cref="McpAgentFrameworkOptions"/> values and the built-in workflow services.
    /// </summary>
    /// <param name="services">The service collection receiving the scaffold registrations.</param>
    /// <returns>The <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddMcpServerAgentFramework(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<McpAgentFrameworkOptions>, McpAgentFrameworkOptionsValidator>());

        services.AddOptions<McpAgentFrameworkOptions>()
            .ValidateOnStart();

        services.AddOptions<McpServerClientOptions>()
            .Configure<IOptions<McpAgentFrameworkOptions>>(static (clientOptions, agentFrameworkOptions) =>
            {
                var options = agentFrameworkOptions.Value;
                clientOptions.ApiKey = options.ApiKey;
                clientOptions.BearerToken = options.BearerToken;
                clientOptions.BaseUrl = options.BaseUrl;
                clientOptions.Timeout = options.Timeout;
                clientOptions.WorkspacePath = options.WorkspacePath;
            });

        McpServer.Client.ServiceCollectionExtensions.AddMcpServerClient(services, static _ =>
        {
        });

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IMcpSessionIdentifierFactory, McpSessionIdentifierFactory>();

        services.TryAddTransient<ChatClientAgentOptions>(static serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<McpAgentFrameworkOptions>>().Value.ToAgentOptions());

        services.TryAddTransient<McpServerClientOptions>(static serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<McpServerClientOptions>>().Value);

        services.TryAddTransient<ISessionLogWorkflow, SessionLogWorkflow>();
        services.TryAddTransient<ITodoWorkflow, TodoWorkflow>();
        services.TryAddTransient<IMcpHostedAgent, McpHostedAgent>();
        services.TryAddSingleton<IMcpHostedAgentFactory, McpHostedAgentFactory>();
        return services;
    }

    /// <summary>
    /// Registers the MCP Server hosted-agent surface using a delegate that configures
    /// <see cref="McpAgentFrameworkOptions"/> and enables the built-in workflow services.
    /// </summary>
    /// <param name="services">The service collection receiving the scaffold registrations.</param>
    /// <param name="configure">Delegate used to configure <see cref="McpAgentFrameworkOptions"/>.</param>
    /// <returns>The <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddMcpServerAgentFramework(
        this IServiceCollection services,
        Action<McpAgentFrameworkOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<McpAgentFrameworkOptions>()
            .Configure(configure);

        return services.AddMcpServerAgentFramework();
    }
}
