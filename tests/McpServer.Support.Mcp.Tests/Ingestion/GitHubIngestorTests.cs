using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Ingestion;

/// <summary>TR-PLANNED-CORE-013: Unit tests for GitHubIngestor.</summary>
public sealed class GitHubIngestorTests
{
    private readonly IGitHubCliService _github = Substitute.For<IGitHubCliService>();
    private readonly GitHubIngestor _sut;

    public GitHubIngestorTests()
    {
        _sut = new GitHubIngestor(new Chunker(), _github, NullLogger<GitHubIngestor>.Instance);
    }

    [Fact]
    public async Task WhenIssuesExist_ThenCreatesDocumentsAndChunks()
    {
        var issues = new[]
        {
            new GitHubIssueItem(1, "Bug fix", "https://github.com/test/issues/1", "open"),
            new GitHubIssueItem(2, "Feature request", "https://github.com/test/issues/2", "closed")
        };
        _github.ListIssuesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueListResult(true, null, issues));
        _github.ListPullsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubPullListResult(true, null, Array.Empty<GitHubPullItem>()));

        var results = await _sut.IngestAsync().ConfigureAwait(true);

        Assert.Equal(2, results.Count);
        Assert.All(results, r =>
        {
            Assert.Equal("github-issue", r.Doc.SourceType);
            Assert.StartsWith("issue/", r.Doc.SourceKey, StringComparison.Ordinal);
            Assert.True(r.Chunks.Count > 0);
        });
    }

    [Fact]
    public async Task WhenPullsExist_ThenCreatesDocumentsWithPrSourceType()
    {
        _github.ListIssuesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueListResult(true, null, Array.Empty<GitHubIssueItem>()));
        var pulls = new[]
        {
            new GitHubPullItem(10, "Add feature", "https://github.com/test/pull/10", "open")
        };
        _github.ListPullsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubPullListResult(true, null, pulls));

        var results = await _sut.IngestAsync().ConfigureAwait(true);

        Assert.Single(results);
        Assert.Equal("github-pr", results[0].Doc.SourceType);
        Assert.Equal("pr/10", results[0].Doc.SourceKey);
    }

    [Fact]
    public async Task WhenGhFails_ThenReturnsEmptyResults()
    {
        _github.ListIssuesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueListResult(false, "not authenticated", Array.Empty<GitHubIssueItem>()));
        _github.ListPullsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubPullListResult(false, "not authenticated", Array.Empty<GitHubPullItem>()));

        var results = await _sut.IngestAsync().ConfigureAwait(true);

        Assert.Empty(results);
    }

    [Fact]
    public async Task ContentFormatting_IssueIncludesNumberAndTitle()
    {
        var issues = new[] { new GitHubIssueItem(99, "Critical bug", "https://github.com/test/issues/99", "open") };
        _github.ListIssuesAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueListResult(true, null, issues));
        _github.ListPullsAsync(Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubPullListResult(true, null, Array.Empty<GitHubPullItem>()));

        var results = await _sut.IngestAsync().ConfigureAwait(true);

        Assert.Single(results);
        var content = results[0].Chunks[0].Content;
        Assert.Contains("Issue #99", content, StringComparison.Ordinal);
        Assert.Contains("Critical bug", content, StringComparison.Ordinal);
        Assert.Contains("open", content, StringComparison.Ordinal);
    }
}
