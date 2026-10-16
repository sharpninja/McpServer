using QBrainAi.Support.Mcp.Options;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Services.AgentHelp;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace QBrainAi.Support.Mcp.Extensions;

/// <summary>
/// FR-MCP-HELP-001: Dependency injection registration for Agent Help services.
/// TR-MCP-HELP-001: Registers options, validators, and conversation orchestration singletons.
/// </summary>
public static class AgentHelpServiceCollectionExtensions
{
    /// <summary>
    /// FR-MCP-HELP-001: Adds Agent Help options, validators, and orchestration services.
    /// TR-MCP-HELP-007: Registers the in-memory conversation service as <see cref="IAgentHelpConversationService"/>.
    /// </summary>
    /// <param name="services">Application service collection.</param>
    /// <param name="configuration">Application configuration root.</param>
    /// <returns>The same <paramref name="services"/> instance for chaining.</returns>
    public static IServiceCollection AddAgentHelpServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AgentHelpOptions>(configuration.GetSection(AgentHelpOptions.SectionName));
        services.TryAddSingleton<IValidateOptions<AgentHelpOptions>, AgentHelpOptionsValidator>();
        services.TryAddSingleton<HelpTranscriptWriter>();
        services.TryAddSingleton<AgentHelpInboundGuard>();
        services.TryAddSingleton<AgentHelpIncidentLogger>();
        services.TryAddSingleton<IGlobalGraphRagCorpusSource, GlobalGraphRagCorpusSource>();
        services.TryAddSingleton<AgentHelpCorpusService>();
        services.TryAddSingleton<AgentHelpPinnedPathResolver>();
        services.TryAddSingleton<AgentHelpOutcomeService>();
        services.TryAddSingleton<AgentHelpConversationService>();
        services.TryAddSingleton<IAgentHelpConversationService>(sp =>
            sp.GetRequiredService<AgentHelpConversationService>());

        return services;
    }
}