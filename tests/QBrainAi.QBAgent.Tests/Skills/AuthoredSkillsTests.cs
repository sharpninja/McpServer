using QBrainAi.QBAgent.Skills;
using QBrainAi.Tests.Shared;

namespace QBrainAi.QBAgent.Tests.Skills;

/// <summary>
/// TEST-MCP-QBSKILLS-002: Verifies the authored workspace skills under the repo <c>skills/</c> root all parse
/// with valid frontmatter and are discoverable (FR-MCP-QBSKILLS-003 ac-2). The skills are read from the repository
/// checkout through the shared <see cref="TestRepositoryRoot"/>, so these are integration tests on both hosts
/// (TR-MCP-USECASE-018 AC-006, hostile validation round 2 finding R2-F1).
/// </summary>
[Trait("Category", "Integration")]
public sealed class AuthoredSkillsTests
{
    private static readonly string[] ExpectedSkills =
    [
        "byrd-tdd-process",
        "mcp-session-logging",
        "mcp-todo",
        "mcp-requirements-traceability",
        "git-usage",
        "bash-usage",
        "edit-file-usage",
    ];

    private static string FindSkillsRoot() => TestRepositoryRoot.Combine("skills");

    /// <summary>Every authored workspace skill parses and is discoverable by name.</summary>
    [Fact]
    public void AuthoredSkills_AllParseAndDiscover()
    {
        var registry = new SkillRegistry([FindSkillsRoot()], new SkillManifestParser());

        var names = registry.Discover().Select(s => s.Name).ToHashSet(StringComparer.Ordinal);

        foreach (var expected in ExpectedSkills)
            Assert.Contains(expected, names);
    }

    /// <summary>Each authored skill loads a non-empty instruction body.</summary>
    [Fact]
    public void AuthoredSkills_HaveBodies()
    {
        var registry = new SkillRegistry([FindSkillsRoot()], new SkillManifestParser());

        foreach (var expected in ExpectedSkills)
        {
            var manifest = registry.Load(expected);
            Assert.NotNull(manifest);
            Assert.False(string.IsNullOrWhiteSpace(manifest!.Body));
        }
    }
}
