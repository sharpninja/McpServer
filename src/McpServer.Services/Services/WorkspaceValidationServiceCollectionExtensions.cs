using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace McpServer.Support.Mcp.Services;

/// <summary>TR-MCP-HYGIENE-005: Registers read-only workspace validation.</summary>
public static class WorkspaceValidationServiceCollectionExtensions
{
    /// <summary>Adds workspace validation services.</summary>
    public static IServiceCollection AddWorkspaceValidationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);
        services.TryAddSingleton(TimeProvider.System);
        services.AddScoped<WorkspaceValidationService>();
        services.AddScoped<IWorkspaceValidationService>(sp => sp.GetRequiredService<WorkspaceValidationService>());
        services.AddScoped<McpServer.Cqrs.Mvvm.IWorkspaceValidationDirectorExecutor, WorkspaceValidationDirectorExecutor>();
        return services;
    }
}
