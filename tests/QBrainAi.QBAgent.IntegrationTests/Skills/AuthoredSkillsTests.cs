using QBrainAi.QBAgent.Skills;
using QBrainAi.Tests.Shared;

namespace QBrainAi.QBAgent.IntegrationTests.Skills;

/// <summary>
/// TEST-MCP-QBSKILLS-002: Verifies the authored workspace skills under the repo <c>skills/</c> root all parse
/// with valid frontmatter and are discoverable (FR-MCP-QBSKILLS-003 ac-2). The skills are read from the repository
/// checkout through the shared <see cref="TestRepositoryRoot"/>, so these are integration tests on both hosts
/// (TR-MCP-USECASE-018 AC-006, hostile validation round 2 finding R2-F1).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-QBSKILLS-002; FR-MCP-QBSKILLS-003 ac-2; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole and unchanged from QBrainAi.QBAgent.Tests (Skills/AuthoredSkillsTests.cs) into the integration-owned QBrainAi.QBAgent.IntegrationTests, so the unit-owned project holds only unit cases (slice S3, hostile validation finding R14-F4; operator 2026-11-06: "Per-owner projects (Recommended)"); only the namespace changed.</remarks>
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
