using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-SURFACE-001: Registers the shared handoff ingestion pipeline.</summary>
public static class HandoffServiceCollectionExtensions
{
    /// <summary>Adds handoff ingestion services to the MCP Server container.</summary>
    public static IServiceCollection AddHandoffServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.AddOptions<HandoffLeaseOptions>();
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<IHandoffSourceResolver, HandoffSourceResolver>();
        services.AddScoped<IHandoffOneShotExtractor>(sp =>
        {
            var pool = sp.GetService<IAgentPoolService>();
            return pool is null
                ? new UnavailableHandoffOneShotExtractor()
                : new HandoffOneShotExtractor(pool);
        });
        services.AddSingleton<IHandoffTodoDraftParser, HandoffTodoDraftParser>();
        services.AddSingleton<IHandoffTodoDraftValidator, HandoffTodoDraftValidator>();
        services.AddSingleton<IHandoffModePolicy, HandoffModePolicy>();
        services.AddScoped<IHandoffIngestionService, HandoffIngestionService>();
        services.AddScoped<McpServer.Cqrs.Mvvm.IHandoffDirectorExecutor, HandoffDirectorExecutor>();
        return services;
    }
}
