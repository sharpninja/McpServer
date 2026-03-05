using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>TR-GH-013-004: Unit tests for IssueIngestor.</summary>
public sealed class IssueIngestorTests
{
    private readonly IGitHubCliService _github = Substitute.For<IGitHubCliService>();
    private readonly IssueIngestor _sut;

    public IssueIngestorTests()
    {
        _sut = new IssueIngestor(new Chunker(), _github, NullLogger<IssueIngestor>.Instance);
    }

    [Fact]
    public async Task IngestAsync_IssuesExist_CreatesDocumentsWithIssueSourceType()
    {
        var issues = new[] { new GitHubIssueItem(1, "Bug", "https://github.com/test/issues/1", "open") };
        _github.ListIssuesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueListResult(true, null, issues));
        _github.GetIssueAsync(1, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(true, CreateDetailedIssue(1, "Bug"), null));

        var results = await _sut.IngestAsync().ConfigureAwait(true);

        Assert.Single(results);
        Assert.Equal("issue", results[0].Doc.SourceType);
        Assert.Equal("issue:1", results[0].Doc.SourceKey);
        Assert.True(results[0].Chunks.Count > 0);
    }

    [Fact]
    public async Task IngestAsync_ContentIncludesTitleBodyComments()
    {
        var issue = new GitHubIssueDetail(
            42, "Critical bug", "This is the body", "OPEN",
            "https://github.com/test/issues/42",
            Array.Empty<GitHubLabel>(), Array.Empty<string>(),
            null, null, null, null, "author1",
            new[] { new GitHubIssueComment("commenter", "A comment", "2026-02-15") });

        var issues = new[] { new GitHubIssueItem(42, "Critical bug", null, "open") };
        _github.ListIssuesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueListResult(true, null, issues));
        _github.GetIssueAsync(42, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(true, issue, null));

        var results = await _sut.IngestAsync().ConfigureAwait(true);

        Assert.Single(results);
        var content = results[0].Chunks[0].Content;
        Assert.Contains("Issue #42", content, StringComparison.Ordinal);
        Assert.Contains("Critical bug", content, StringComparison.Ordinal);
        Assert.Contains("This is the body", content, StringComparison.Ordinal);
        Assert.Contains("A comment", content, StringComparison.Ordinal);
    }

    [Fact]
    public async Task IngestAsync_GhFails_ReturnsEmpty()
    {
        _github.ListIssuesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueListResult(false, "not authenticated", Array.Empty<GitHubIssueItem>()));

        var results = await _sut.IngestAsync().ConfigureAwait(true);

        Assert.Empty(results);
    }

    [Fact]
    public async Task IngestAsync_GetIssueFails_SkipsIssue()
    {
        var issues = new[]
        {
            new GitHubIssueItem(1, "Bug1", null, "open"),
            new GitHubIssueItem(2, "Bug2", null, "open")
        };
        _github.ListIssuesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueListResult(true, null, issues));
        _github.GetIssueAsync(1, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(false, null, "not found"));
        _github.GetIssueAsync(2, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(true, CreateDetailedIssue(2, "Bug2"), null));

        var results = await _sut.IngestAsync().ConfigureAwait(true);

        Assert.Single(results);
        Assert.Equal("issue:2", results[0].Doc.SourceKey);
    }

    [Fact]
    public void FormatIssueContent_IncludesAllSections()
    {
        var issue = new GitHubIssueDetail(
            1, "Test", "Body text", "OPEN", null,
            Array.Empty<GitHubLabel>(), Array.Empty<string>(),
            null, null, null, null, null,
            new[] { new GitHubIssueComment("user1", "Comment 1", "2026-01-01") });

        var content = IssueIngestor.FormatIssueContent(issue);

        Assert.Contains("# Issue #1: Test", content, StringComparison.Ordinal);
        Assert.Contains("Body text", content, StringComparison.Ordinal);
        Assert.Contains("## Comments", content, StringComparison.Ordinal);
        Assert.Contains("Comment 1", content, StringComparison.Ordinal);
    }

    private static GitHubIssueDetail CreateDetailedIssue(int number, string title) =>
        new(number, title, "Test body", "OPEN",
            $"https://github.com/test/issues/{number}",
            Array.Empty<GitHubLabel>(), Array.Empty<string>(),
            null, null, null, null, "author",
            Array.Empty<GitHubIssueComment>());
}
