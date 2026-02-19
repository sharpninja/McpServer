using McpServer.Support.Mcp.Models;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Models;

/// <summary>TR-GH-013-005: Unit tests for IssueNoteFrontmatter parse/serialize.</summary>
public sealed class IssueNoteFrontmatterTests
{
    [Fact]
    public void Parse_ValidFrontmatter_ReturnsAllFields()
    {
        var note = """
            status: OPEN
            github-url: https://github.com/user/repo/issues/42
            labels: bug, enhancement
            assignees: user1, user2
            created: 2026-02-15
            updated: 2026-02-16
            """;

        var result = IssueNoteFrontmatter.Parse(note);

        Assert.NotNull(result);
        Assert.Equal("OPEN", result.Status);
        Assert.Equal("https://github.com/user/repo/issues/42", result.GitHubUrl);
        Assert.Equal(new[] { "bug", "enhancement" }, result.Labels);
        Assert.Equal(new[] { "user1", "user2" }, result.Assignees);
        Assert.Equal("2026-02-15", result.Created);
        Assert.Equal("2026-02-16", result.Updated);
    }

    [Fact]
    public void Parse_NullOrEmpty_ReturnsNull()
    {
        Assert.Null(IssueNoteFrontmatter.Parse(null));
        Assert.Null(IssueNoteFrontmatter.Parse(""));
        Assert.Null(IssueNoteFrontmatter.Parse("   "));
    }

    [Fact]
    public void Parse_PartialFields_ReturnsPopulatedFieldsOnly()
    {
        var note = "status: closed\ngithub-url: https://example.com/1";

        var result = IssueNoteFrontmatter.Parse(note);

        Assert.NotNull(result);
        Assert.Equal("closed", result.Status);
        Assert.Equal("https://example.com/1", result.GitHubUrl);
        Assert.Null(result.Labels);
        Assert.Null(result.Assignees);
    }

    [Fact]
    public void Serialize_AllFields_ProducesCorrectFormat()
    {
        var fm = new IssueNoteFrontmatter
        {
            Status = "OPEN",
            GitHubUrl = "https://github.com/user/repo/issues/42",
            Labels = new[] { "bug", "enhancement" },
            Assignees = new[] { "user1" },
            Created = "2026-02-15",
            Updated = "2026-02-16"
        };

        var serialized = fm.Serialize();

        Assert.Contains("status: OPEN", serialized, StringComparison.Ordinal);
        Assert.Contains("github-url: https://github.com/user/repo/issues/42", serialized, StringComparison.Ordinal);
        Assert.Contains("labels: bug, enhancement", serialized, StringComparison.Ordinal);
        Assert.Contains("assignees: user1", serialized, StringComparison.Ordinal);
        Assert.Contains("created: 2026-02-15", serialized, StringComparison.Ordinal);
        Assert.Contains("updated: 2026-02-16", serialized, StringComparison.Ordinal);
    }

    [Fact]
    public void RoundTrip_SerializeThenParse_PreservesAllFields()
    {
        var original = new IssueNoteFrontmatter
        {
            Status = "OPEN",
            GitHubUrl = "https://github.com/user/repo/issues/99",
            Labels = new[] { "bug" },
            Assignees = new[] { "dev1", "dev2" },
            Created = "2026-01-01",
            Updated = "2026-02-01"
        };

        var serialized = original.Serialize();
        var parsed = IssueNoteFrontmatter.Parse(serialized);

        Assert.NotNull(parsed);
        Assert.Equal(original.Status, parsed.Status);
        Assert.Equal(original.GitHubUrl, parsed.GitHubUrl);
        Assert.Equal(original.Labels, parsed.Labels);
        Assert.Equal(original.Assignees, parsed.Assignees);
        Assert.Equal(original.Created, parsed.Created);
        Assert.Equal(original.Updated, parsed.Updated);
    }

    [Fact]
    public void Parse_NoRecognizedKeys_ReturnsNull()
    {
        var note = "unknown-key: some value\nanother: thing";
        Assert.Null(IssueNoteFrontmatter.Parse(note));
    }
}
