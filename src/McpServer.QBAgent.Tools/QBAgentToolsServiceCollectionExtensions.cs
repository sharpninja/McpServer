using McpServer.Common.Copilot;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace McpServer.QBAgent.Tools;

/// <summary>
/// TR-MCP-QBTOOLS-008: DI registration for the QBAgent external tool surface dependencies. Registers the process
/// execution services (<see cref="IProcessRunner"/> + <see cref="IProcessEnvironmentService"/>) the git and bash
/// tools need, so a host building a bare <see cref="IServiceCollection"/> for QBAgent can resolve them.
/// </summary>
public static class QBAgentToolsServiceCollectionExtensions
{
    /// <summary>Registers the QBAgent tool dependencies (process runner + environment service + logging).</summary>
    /// <param name="services">The service collection.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQBAgentTools(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddLogging();
        services.AddOptions<ProcessRunnerOptions>();
        services.TryAddSingleton<IProcessEnvironmentService, ProcessEnvironmentService>();
        services.TryAddSingleton<IProcessRunner, ProcessRunner>();
        return services;
    }
}
