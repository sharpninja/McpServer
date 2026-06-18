using McpServer.QBAgent.Skills;
using Microsoft.Extensions.AI;

namespace McpServer.QBAgent.Tools;

/// <summary>
/// FR-MCP-QBSKILLS-002: Exposes the skill subsystem to the QBAgent loop as external tools. Under progressive
/// disclosure the discovery list (name + description) is injected into the system prompt, and the model calls
/// <c>load_skill</c> to pull a skill's full instructions on demand.
/// </summary>
public sealed class SkillTool
{
    private readonly ISkillRegistry _registry;

    /// <summary>Initializes a new instance of the <see cref="SkillTool"/> class.</summary>
    /// <param name="registry">The skill registry the tools read from.</param>
    public SkillTool(ISkillRegistry registry)
        => _registry = registry ?? throw new ArgumentNullException(nameof(registry));

    /// <summary>Lists available skills (name + description) for discovery.</summary>
    /// <returns>The discovery list.</returns>
    public IReadOnlyList<SkillSummary> ListSkills() => _registry.Discover();

    /// <summary>Loads a skill's full instruction body by name.</summary>
    /// <param name="name">The skill name.</param>
    /// <returns>The skill body, or a not-found message.</returns>
    public string LoadSkill(string name)
    {
        var manifest = _registry.Load(name);
        return manifest is null ? $"Skill '{name}' was not found. Call list_skills to see available skills." : manifest.Body;
    }

    /// <summary>Builds the <c>list_skills</c> and <c>load_skill</c> external tools.</summary>
    /// <returns>The skill tools.</returns>
    public IReadOnlyList<AITool> CreateTools() =>
    [
        AIFunctionFactory.Create(
            (Func<IReadOnlyList<SkillSummary>>)ListSkills,
            new AIFunctionFactoryOptions
            {
                Name = "list_skills",
                Description = "List available skills (name and description) to discover relevant procedures before acting.",
            }),
        AIFunctionFactory.Create(
            (Func<string, string>)LoadSkill,
            new AIFunctionFactoryOptions
            {
                Name = "load_skill",
                Description = "Load the full instructions for a skill by its name, then follow them.",
            }),
    ];
}
