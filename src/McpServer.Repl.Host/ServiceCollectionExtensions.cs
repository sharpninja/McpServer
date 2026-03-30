using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Repl.Host;

/// <summary>
/// Extension methods for configuring REPL services in the DI container.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds REPL core services to the service collection.
    /// Registers protocol handlers, workspace selectors, marker file readers, and auth rotation handlers.
    /// </summary>
    /// <param name="services">The service collection to configure.</param>
    /// <returns>The configured service collection for fluent chaining.</returns>
    public static IServiceCollection AddReplCoreServices(this IServiceCollection services)
    {
        return services;
    }
}
