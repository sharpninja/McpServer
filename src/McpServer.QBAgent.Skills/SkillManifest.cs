namespace McpServer.QBAgent.Skills;

/// <summary>
/// FR-MCP-QBSKILLS-001 / TR-MCP-QBSKILLS-001: A parsed agentskills.io skill: its frontmatter metadata plus the
/// instruction body. <see cref="Name"/> and <see cref="Description"/> are required; the rest is optional.
/// </summary>
/// <param name="Name">The kebab-case skill name (matches the folder name).</param>
/// <param name="Description">A one-line description used during discovery to decide relevance.</param>
/// <param name="License">Optional SPDX license identifier.</param>
/// <param name="AllowedTools">Optional list of tools the skill is allowed to use.</param>
/// <param name="Body">The markdown instruction body (everything after the frontmatter).</param>
/// <param name="Path">The absolute path of the SKILL.md file.</param>
public sealed record SkillManifest(
    string Name,
    string Description,
    string? License,
    IReadOnlyList<string> AllowedTools,
    string Body,
    string Path);

/// <summary>FR-MCP-QBSKILLS-001: The discovery view of a skill: just enough to decide relevance.</summary>
/// <param name="Name">The skill name.</param>
/// <param name="Description">The skill description.</param>
public sealed record SkillSummary(string Name, string Description);
