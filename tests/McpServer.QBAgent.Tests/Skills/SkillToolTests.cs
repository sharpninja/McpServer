using McpServer.QBAgent.Skills;
using McpServer.QBAgent.Tools;

namespace McpServer.QBAgent.Tests.Skills;

/// <summary>
/// TEST-MCP-QBSKILLS-003: Verifies the skill tools (list_skills / load_skill) are exposed as non-mcp_ external
/// tools and surface the registry contents (FR-MCP-QBSKILLS-002).
/// </summary>
public sealed class SkillToolTests
{
    private sealed class FakeRegistry : ISkillRegistry
    {
        public IReadOnlyList<SkillSummary> Discover() => [new SkillSummary("byrd-tdd-process", "Follow the Byrd TDD process.")];

        public SkillManifest? Load(string name)
            => name == "byrd-tdd-process"
                ? new SkillManifest("byrd-tdd-process", "Follow the Byrd TDD process.", "MIT", [], "RED then GREEN then REFACTOR", "p")
                : null;
    }

    private static SkillTool CreateSut() => new(new FakeRegistry());

    /// <summary>list_skills and load_skill are exposed as non-mcp_ tools.</summary>
    [Fact]
    public void CreateTools_ExposesListAndLoad_NonMcpPrefixed()
    {
        var names = CreateSut().CreateTools().Select(t => t.Name).ToHashSet(StringComparer.Ordinal);

        Assert.Contains("list_skills", names);
        Assert.Contains("load_skill", names);
        Assert.DoesNotContain(names, n => n.StartsWith("mcp_", StringComparison.Ordinal));
    }

    /// <summary>list_skills returns the discovery summaries.</summary>
    [Fact]
    public void ListSkills_ReturnsDiscovery()
    {
        var summaries = CreateSut().ListSkills();

        Assert.Single(summaries);
        Assert.Equal("byrd-tdd-process", summaries[0].Name);
    }

    /// <summary>load_skill returns the body for a known skill.</summary>
    [Fact]
    public void LoadSkill_Known_ReturnsBody()
    {
        var body = CreateSut().LoadSkill("byrd-tdd-process");

        Assert.Contains("RED then GREEN", body, StringComparison.Ordinal);
    }

    /// <summary>load_skill returns a not-found message for an unknown skill.</summary>
    [Fact]
    public void LoadSkill_Unknown_ReturnsNotFound()
    {
        var body = CreateSut().LoadSkill("nope");

        Assert.Contains("not found", body, StringComparison.Ordinal);
    }
}
