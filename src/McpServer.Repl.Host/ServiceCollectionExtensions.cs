using Microsoft.Extensions.DependencyInjection;
using McpServer.Repl.Core;

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
        // Register TODO workflow
        services.AddSingleton<ITodoWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<McpServer.Client.McpServerClient>();
            return new TodoWorkflow(clientFactory.Todo);
        });

        return services;
    }
}
