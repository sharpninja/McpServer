using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace QBrainAi.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TRIAGE-003: Registers triage intake, research runner, and shared time provider services.
/// </summary>
public static class TriageServiceCollectionExtensions
{
    /// <summary>Adds triage services to the QBrain.AI dependency injection container.</summary>
    public static IServiceCollection AddTriageServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.TryAddScoped<ITriageTodoCreator, DirectTriageTodoCreator>();
        services.AddScoped<ITriageResearchRunner, ConfiguredTriageResearchRunner>();
        services.AddScoped<ITriageService, TriageService>();
        return services;
    }
}
