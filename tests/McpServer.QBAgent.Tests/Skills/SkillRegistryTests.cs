using McpServer.QBAgent.Skills;

namespace McpServer.QBAgent.Tests.Skills;

/// <summary>
/// TEST-MCP-QBSKILLS-002: Verifies the skill registry discovers skills recursively (flat and nested layouts),
/// returns only name+description for discovery, and loads the full body on demand (FR-MCP-QBSKILLS-001/003).
/// </summary>
public sealed class SkillRegistryTests : IDisposable
{
    private readonly string _root;

    public SkillRegistryTests()
    {
        _root = Path.Combine(Path.GetTempPath(), $"skills_test_{Guid.NewGuid():N}");
        // Flat workspace layout: skills/alpha/SKILL.md
        WriteSkill(Path.Combine(_root, "skills", "alpha"), "alpha", "Alpha does A.", "ALPHA BODY");
        // Nested vendored layout: vendor/plugins/p/skills/beta/SKILL.md
        WriteSkill(Path.Combine(_root, "vendor", "plugins", "p", "skills", "beta"), "beta", "Beta does B.", "BETA BODY");
        // An invalid skill (no name) must be skipped silently.
        Directory.CreateDirectory(Path.Combine(_root, "skills", "broken"));
        File.WriteAllText(Path.Combine(_root, "skills", "broken", "SKILL.md"), "---\ndescription: no name\n---\nx");
    }

    public void Dispose()
    {
        if (Directory.Exists(_root))
            Directory.Delete(_root, true);
    }

    private static void WriteSkill(string dir, string name, string description, string body)
    {
        Directory.CreateDirectory(dir);
        File.WriteAllText(Path.Combine(dir, "SKILL.md"), $"---\nname: {name}\ndescription: {description}\n---\n\n{body}\n");
    }

    private SkillRegistry CreateSut() => new([_root], new SkillManifestParser());

    /// <summary>Discovery finds both the flat and nested valid skills and returns name+description.</summary>
    [Fact]
    public void Discover_FindsValidSkillsAcrossLayouts()
    {
        var summaries = CreateSut().Discover();

        Assert.Contains(summaries, s => s.Name == "alpha" && s.Description == "Alpha does A.");
        Assert.Contains(summaries, s => s.Name == "beta" && s.Description == "Beta does B.");
        Assert.DoesNotContain(summaries, s => s.Name == string.Empty);
    }

    /// <summary>Load returns the full body for a known skill and null for an unknown one.</summary>
    [Fact]
    public void Load_ReturnsBody_OrNull()
    {
        var sut = CreateSut();

        Assert.Contains("ALPHA BODY", sut.Load("alpha")!.Body, StringComparison.Ordinal);
        Assert.Null(sut.Load("does-not-exist"));
    }

    /// <summary>A nonexistent root contributes nothing rather than throwing.</summary>
    [Fact]
    public void Discover_NonexistentRoot_NoThrow()
    {
        var sut = new SkillRegistry([Path.Combine(_root, "missing")], new SkillManifestParser());

        Assert.Empty(sut.Discover());
    }
}
