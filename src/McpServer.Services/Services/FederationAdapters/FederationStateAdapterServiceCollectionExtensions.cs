using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Services.FederationAdapters;

/// <summary>Dependency injection helpers for federation state adapters.</summary>
public static class FederationStateAdapterServiceCollectionExtensions
{
    /// <summary>
    /// Registers federation state adapters for mutable MCP state domains and
    /// explicit local-only exemptions.
    /// </summary>
    /// <param name="services">Service collection to update.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddFederationStateAdapters(this IServiceCollection services)
    {
        services.AddSingleton<IFederationStateAdapter, WorkspaceFederationStateAdapter>();
        services.AddSingleton<IFederationStateAdapter, MemoryFederationStateAdapter>();
        services.AddSingleton<IFederationStateAdapter, TodoFederationStateAdapter>();
        services.AddSingleton<IFederationStateAdapter, SessionLogFederationStateAdapter>();
        services.AddSingleton<IFederationStateAdapter, RequirementsFederationStateAdapter>();
        services.AddSingleton<IFederationStateAdapter, ToolsBucketsFederationStateAdapter>();
        services.AddSingleton<IFederationStateAdapter, AgentsFederationStateAdapter>();
        services.AddSingleton<IFederationStateAdapter>(_ => new LocalOnlyFederationStateAdapter(
            "context_metadata",
            "Context search metadata is derived from local indexes, embeddings, and chunk stores."));
        services.AddSingleton<IFederationStateAdapter>(_ => new LocalOnlyFederationStateAdapter(
            "github_metadata",
            "GitHub remains the external source of truth and local credentials must stay proxy-owned."));
        services.AddSingleton<IFederationStateAdapter>(_ => new LocalOnlyFederationStateAdapter(
            "repo_file_changes",
            "Arbitrary repository file writes must flow through explicit git or worktree controls."));
        services.AddSingleton<IFederationStateAdapter>(_ => new LocalOnlyFederationStateAdapter(
            "marker_state",
            "Marker files contain host-specific ports, process ids, API keys, signatures, and trust material."));
        services.AddSingleton<IFederationStateAdapter>(_ => new LocalOnlyFederationStateAdapter(
            "mcp_transport",
            "MCP transport streams can carry arbitrary tool calls and are forwarded live rather than replayed from the offline queue."));

        return services;
    }
}
