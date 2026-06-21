using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace McpServer.QBAgent.Skills;

/// <summary>
/// TR-MCP-QBSKILLS-002: DI registration for the QBAgent skill subsystem. Registers the manifest parser and a
/// <see cref="SkillRegistry"/> bound to the supplied skill root directories (for example the workspace
/// <c>skills/</c> folder plus any vendored <c>skills/vendor/dotnet-skills</c> root).
/// </summary>
public static class QBAgentSkillsServiceCollectionExtensions
{
    /// <summary>Registers the skill parser and registry over the given roots.</summary>
    /// <param name="services">The service collection.</param>
    /// <param name="roots">Skill root directories, in priority order.</param>
    /// <returns>The same service collection for chaining.</returns>
    public static IServiceCollection AddQBAgentSkills(this IServiceCollection services, params string[] roots)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<ISkillManifestParser, SkillManifestParser>();
        services.TryAddSingleton<ISkillRegistry>(sp =>
            new SkillRegistry(roots, sp.GetRequiredService<ISkillManifestParser>()));
        return services;
    }
}
