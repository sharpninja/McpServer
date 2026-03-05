using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Ingestion;

namespace McpServer.Services;

/// <summary>
/// Extension methods for registering McpServer services with the DI container.
/// </summary>
public static class ServicesServiceCollectionExtensions
{
    /// <summary>
    /// Registers MCP service implementations with the dependency injection container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configuration">The application configuration.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    public static IServiceCollection AddMcpServices(this IServiceCollection services, IConfiguration configuration)
    {
        // Placeholder — concrete registrations will be wired in ARCH-SUPPORT-001
        // For now, the class library compiles and the extension exists
        return services;
    }
}
