using QBrainAi.McpAgent.Hosting;
using QBrainAi.Client;
using QBrainAi.Repl.Core;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using AgentSessionLogWorkflow = QBrainAi.McpAgent.SessionLog.SessionLogWorkflow;
using AgentTodoWorkflow = QBrainAi.McpAgent.Todo.TodoWorkflow;
using IAgentSessionLogWorkflow = QBrainAi.McpAgent.SessionLog.ISessionLogWorkflow;
using IAgentTodoWorkflow = QBrainAi.McpAgent.Todo.ITodoWorkflow;
using IReplSessionLogWorkflow = QBrainAi.Repl.Core.ISessionLogWorkflow;
using ReplSessionLogWorkflow = QBrainAi.Repl.Core.SessionLogWorkflow;

namespace QBrainAi.McpAgent;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-006: Dependency injection extensions for the hosted MCP Agent
/// registration surface, including the built-in session-log, TODO, requirements, and
/// generic client passthrough workflow services.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the QBrain.AI hosted-agent surface using externally configured
    /// <see cref="McpAgentOptions"/> values and the built-in workflow services.
    /// </summary>
    /// <param name="services">The service collection receiving the scaffold registrations.</param>
    /// <returns>The <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddQBrainAiMcpAgent(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IValidateOptions<McpAgentOptions>, McpAgentOptionsValidator>());

        services.AddOptions<McpAgentOptions>()
            .ValidateOnStart();

        services.AddOptions<QBrainAiClientOptions>()
            .Configure<IOptions<McpAgentOptions>>(static (clientOptions, agentFrameworkOptions) =>
            {
                var options = agentFrameworkOptions.Value;
                clientOptions.ApiKey = options.ApiKey;
                clientOptions.BearerToken = options.BearerToken;
                clientOptions.BaseUrl = options.BaseUrl;
                clientOptions.DesktopLaunchToken = options.DesktopLaunchToken;
                clientOptions.Timeout = options.Timeout;
                clientOptions.WorkspacePath = options.WorkspacePath;
            });

        QBrainAi.Client.ServiceCollectionExtensions.AddQBrainAiClient(services, static _ =>
        {
        });

        services.TryAddSingleton(TimeProvider.System);
        services.TryAddSingleton<IMcpSessionIdentifierFactory, McpSessionIdentifierFactory>();

        services.TryAddTransient<ChatClientAgentOptions>(static serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<McpAgentOptions>>().Value.ToAgentOptions());

        services.TryAddTransient<QBrainAiClientOptions>(static serviceProvider =>
            serviceProvider.GetRequiredService<IOptions<QBrainAiClientOptions>>().Value);

        // McpAgent-internal workflows (operate on McpAgent.SessionLog/Todo types)
        services.TryAddTransient<IAgentSessionLogWorkflow, AgentSessionLogWorkflow>();
        services.TryAddTransient<IAgentTodoWorkflow, AgentTodoWorkflow>();

        // REPL Core workflows (requirements, session history, generic passthrough)
        services.TryAddTransient<IRequirementsWorkflow>(static sp =>
            new RequirementsWorkflow(sp.GetRequiredService<QBrainAiClient>().Requirements));

        services.TryAddTransient<IGenericClientPassthrough>(static sp =>
            new GenericClientPassthrough(
                sp.GetRequiredService<QBrainAiClient>(),
                sp.GetService<IClientMutationPolicy>()));

        services.TryAddTransient<ISessionLogClientAdapter>(static sp =>
            new SessionLogClientAdapter(sp.GetRequiredService<QBrainAiClient>().SessionLog));

        services.TryAddTransient<IReplSessionLogWorkflow>(static sp =>
            new ReplSessionLogWorkflow(
                sp.GetRequiredService<ISessionLogClientAdapter>(),
                sp.GetRequiredService<TimeProvider>()));

        services.TryAddTransient<IMcpHostedAgent, McpHostedAgent>();
        services.TryAddSingleton<IMcpHostedAgentFactory, McpHostedAgentFactory>();
        return services;
    }

    /// <summary>
    /// Registers the QBrain.AI hosted-agent surface using a delegate that configures
    /// <see cref="McpAgentOptions"/> and enables the built-in workflow services.
    /// </summary>
    /// <param name="services">The service collection receiving the scaffold registrations.</param>
    /// <param name="configure">Delegate used to configure <see cref="McpAgentOptions"/>.</param>
    /// <returns>The <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddQBrainAiMcpAgent(
        this IServiceCollection services,
        Action<McpAgentOptions> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddOptions<McpAgentOptions>()
            .Configure(configure);

        return services.AddQBrainAiMcpAgent();
    }
}
