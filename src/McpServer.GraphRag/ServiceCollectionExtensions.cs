using System.Reflection;
using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.GraphRag;

/// <summary>
/// DI registration extensions for the McpServer.GraphRag library.
/// </summary>
public static class GraphRagServiceCollectionExtensions
{
    /// <summary>
    /// Registers GraphRAG services, backend adapters, and CQRS handlers.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddMcpGraphRag(this IServiceCollection services)
    {
        services.AddScoped<IGraphRagService, GraphRagService>();
        services.AddTransient<IGraphRagBackendAdapter, ExternalCommandGraphRagBackendAdapter>();
        services.AddTransient<IGraphRagBackendAdapter, InternalFallbackGraphRagBackendAdapter>();
        services.AddCqrsHandlers(Assembly.GetExecutingAssembly());
        return services;
    }
}
