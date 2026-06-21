using McpServer.QBAgent.Skills;

namespace McpServer.QBAgent.Tests.Skills;

/// <summary>
/// TEST-MCP-QBSKILLS-001: Verifies the SKILL.md frontmatter parser requires name+description, rejects missing
/// fields and missing frontmatter, reads optional license/allowed-tools, and separates the body (FR-MCP-QBSKILLS-001).
/// </summary>
public sealed class SkillManifestParserTests
{
    private readonly SkillManifestParser _parser = new();

    private const string Valid =
        "---\n" +
        "name: my-skill\n" +
        "description: Use this skill to do a thing.\n" +
        "license: MIT\n" +
        "allowed-tools: read_file, git\n" +
        "---\n\n" +
        "# My Skill\nInstructions here.\n";

    /// <summary>A valid SKILL.md parses into name, description, license, allowed-tools, and body.</summary>
    [Fact]
    public void Parse_Valid_ExtractsAllFields()
    {
        var ok = _parser.TryParse(Valid, "path/SKILL.md", out var manifest, out var error);

        Assert.True(ok);
        Assert.Null(error);
        Assert.NotNull(manifest);
        Assert.Equal("my-skill", manifest!.Name);
        Assert.Equal("Use this skill to do a thing.", manifest.Description);
        Assert.Equal("MIT", manifest.License);
        Assert.Contains("read_file", manifest.AllowedTools);
        Assert.Contains("git", manifest.AllowedTools);
        Assert.Contains("# My Skill", manifest.Body, StringComparison.Ordinal);
        Assert.DoesNotContain("name:", manifest.Body, StringComparison.Ordinal);
    }

    /// <summary>Missing name is rejected with a descriptive error.</summary>
    [Fact]
    public void Parse_MissingName_Fails()
    {
        var content = "---\ndescription: a thing\n---\nbody";

        var ok = _parser.TryParse(content, "p", out var manifest, out var error);

        Assert.False(ok);
        Assert.Null(manifest);
        Assert.Contains("name", error!, StringComparison.Ordinal);
    }

    /// <summary>Missing description is rejected.</summary>
    [Fact]
    public void Parse_MissingDescription_Fails()
    {
        var content = "---\nname: x\n---\nbody";

        var ok = _parser.TryParse(content, "p", out _, out var error);

        Assert.False(ok);
        Assert.Contains("description", error!, StringComparison.Ordinal);
    }

    /// <summary>A document with no frontmatter is rejected.</summary>
    [Fact]
    public void Parse_NoFrontmatter_Fails()
    {
        var ok = _parser.TryParse("# Just markdown\nno frontmatter", "p", out _, out var error);

        Assert.False(ok);
        Assert.Contains("frontmatter", error!, StringComparison.Ordinal);
    }

    /// <summary>CRLF line endings are handled.</summary>
    [Fact]
    public void Parse_CrlfLineEndings_Succeeds()
    {
        var content = "---\r\nname: crlf\r\ndescription: handles crlf\r\n---\r\nbody";

        var ok = _parser.TryParse(content, "p", out var manifest, out _);

        Assert.True(ok);
        Assert.Equal("crlf", manifest!.Name);
    }
}
