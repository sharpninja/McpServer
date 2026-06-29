using McpServer.Common.AgentCli;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace McpServer.Common.AgentCli.Extensions;

/// <summary>Extension methods for registering CLI agent client services.</summary>
public static class AgentCliServiceCollectionExtensions
{
    /// <summary>Registers IAgentCliClient with default options.</summary>
    public static IServiceCollection AddAgentCliClient(this IServiceCollection services)
    {
        services.AddOptions<AgentCliClientOptions>();
        services.AddSingleton<IProcessEnvironmentService, ProcessEnvironmentService>();
        services.TryAddSingleton<IProcessSpawner, DefaultProcessSpawner>();
        services.TryAddSingleton<AgentCliClient>();
        services.AddSingleton<IAgentCliClient>(sp => sp.GetRequiredService<AgentCliClient>());
        return services;
    }

    /// <summary>Registers IAgentCliClient with custom options.</summary>
    public static IServiceCollection AddAgentCliClient(this IServiceCollection services, Action<AgentCliClientOptions> configure)
    {
        services.AddOptions<AgentCliClientOptions>()
            .Configure(configure);
        services.AddSingleton<IProcessEnvironmentService, ProcessEnvironmentService>();
        services.TryAddSingleton<IProcessSpawner, DefaultProcessSpawner>();
        services.TryAddSingleton<AgentCliClient>();
        services.AddSingleton<IAgentCliClient>(sp => sp.GetRequiredService<AgentCliClient>());
        return services;
    }
}
