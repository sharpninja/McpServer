using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace FWH.Common.Copilot.Extensions;

/// <summary>TR-CLI-001: DI extension methods for FWH.Common.Copilot.</summary>
public static class CopilotServiceCollectionExtensions
{
    /// <summary>
    /// Registers the Copilot CLI client with default options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCopilotClient(this IServiceCollection services)
    {
        services.TryAddSingleton<ICopilotClient, CopilotClient>();
        services.AddOptions<CopilotClientOptions>();
        return services;
    }

    /// <summary>
    /// Registers the Copilot CLI client with custom options.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configure">Action to configure <see cref="CopilotClientOptions"/>.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddCopilotClient(
        this IServiceCollection services,
        Action<CopilotClientOptions> configure)
    {
        services.TryAddSingleton<ICopilotClient, CopilotClient>();
        services.Configure(configure);
        return services;
    }
}
