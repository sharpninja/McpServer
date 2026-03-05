using McpServer.Common.Copilot;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace McpServer.Common.Copilot.Extensions;

/// <summary>Extension methods for registering Copilot client services.</summary>
public static class CopilotServiceCollectionExtensions
{
    /// <summary>Registers ICopilotClient with default options.</summary>
    public static IServiceCollection AddCopilotClient(this IServiceCollection services)
    {
        services.AddOptions<CopilotClientOptions>();
        services.AddSingleton<IProcessEnvironmentService, ProcessEnvironmentService>();
        services.TryAddSingleton<IProcessSpawner, DefaultProcessSpawner>();
        services.AddSingleton<ICopilotClient, CopilotClient>();
        return services;
    }

    /// <summary>Registers ICopilotClient with custom options.</summary>
    public static IServiceCollection AddCopilotClient(this IServiceCollection services, Action<CopilotClientOptions> configure)
    {
        services.AddOptions<CopilotClientOptions>()
            .Configure(configure);
        services.AddSingleton<IProcessEnvironmentService, ProcessEnvironmentService>();
        services.TryAddSingleton<IProcessSpawner, DefaultProcessSpawner>();
        services.AddSingleton<ICopilotClient, CopilotClient>();
        return services;
    }
}
