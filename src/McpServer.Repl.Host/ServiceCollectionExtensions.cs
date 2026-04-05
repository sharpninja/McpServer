// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - Service registration extensions
// TR-MCP-REPL-002: DI-Integrated REPL Host - Service collection configuration
// TR-MCP-REPL-005: Namespace Organization and Handler Parity - Workflow DI registration

// FR-MCP-REPL-001: YAML Protocol STDIO REPL Host - DI service registration
// TR-MCP-REPL-002: DI-Integrated REPL Host - Service composition root
// TR-MCP-REPL-004: Command Registry and Dispatcher - Workflow handler registration
// TEST-MCP-REPL-016: All dependencies resolved from DI container

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
        // Register TODO workflow (implementation lives in McpServer.Repl.Core)
        services.AddSingleton<ITodoWorkflow>(sp =>
        {
            var clientFactory = sp.GetRequiredService<McpServer.Client.McpServerClient>();
            return new McpServer.Repl.Core.TodoWorkflow(clientFactory.Todo);
        });

        return services;
    }
}
