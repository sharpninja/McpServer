using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using NSubstitute;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TR-PLANNED-013, TR-GH-013-001: Unit tests for GitHubCliService with mocked IProcessRunner.</summary>
public sealed class GitHubCliServiceTests
{
    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
    private readonly GitHubCliService _sut;

    public GitHubCliServiceTests()
    {
        _sut = new GitHubCliService(_processRunner, NullLogger<GitHubCliService>.Instance);
    }

    [Fact]
    public async Task ListIssuesAsync_WhenGhSucceeds_ReturnsIssues()
    {
        var json = """[{"number":1,"title":"Bug","url":"https://github.com/test/1","state":"open"}]""";
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, json, null));

        var result = await _sut.ListIssuesAsync("open", 10).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Single(result.Issues);
        Assert.Equal(1, result.Issues[0].Number);
        Assert.Equal("Bug", result.Issues[0].Title);
    }

    [Fact]
    public async Task ListIssuesAsync_WhenGhFails_ReturnsError()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not authenticated"));

        var result = await _sut.ListIssuesAsync(null, 10).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("not authenticated", result.Error);
        Assert.Empty(result.Issues);
    }

    [Fact]
    public async Task ListPullsAsync_WhenGhSucceeds_ReturnsPulls()
    {
        var json = """[{"number":42,"title":"Feature","url":"https://github.com/test/pr/42","state":"open"}]""";
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, json, null));

        var result = await _sut.ListPullsAsync("open", 10).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Single(result.Pulls);
        Assert.Equal(42, result.Pulls[0].Number);
    }

    [Fact]
    public async Task CreateIssueAsync_WhenGhSucceeds_ReturnsUrl()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "https://github.com/test/issues/5\n", null));

        var result = await _sut.CreateIssueAsync("New issue", "Body text").ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal(5, result.Number);
        Assert.Contains("issues/5", result.Url, StringComparison.Ordinal);
    }

    [Fact]
    public async Task CreateIssueAsync_WhenGhFails_ReturnsError()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "auth required"));

        var result = await _sut.CreateIssueAsync("New issue", null).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("auth required", result.Error);
    }

    [Fact]
    public async Task CommentOnIssueAsync_WhenGhSucceeds_ReturnsSuccess()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CommentOnIssueAsync("1", "test comment").ConfigureAwait(true);

        Assert.True(result.Success);
    }

    [Fact]
    public async Task CommentOnPullAsync_VerifiesGhArgs()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CommentOnPullAsync("42", "PR comment").ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("pr comment 42", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task GetIssueAsync_WhenGhSucceeds_ReturnsIssueDetail()
    {
        var json = """
            {
                "number": 42,
                "title": "Bug report",
                "body": "Description here",
                "state": "OPEN",
                "url": "https://github.com/test/issues/42",
                "labels": [{"name": "bug", "color": "d73a4a", "description": "Something broken"}],
                "assignees": [{"login": "user1"}],
                "milestone": {"title": "v1.0"},
                "createdAt": "2026-02-15T00:00:00Z",
                "updatedAt": "2026-02-16T00:00:00Z",
                "closedAt": null,
                "author": {"login": "reporter"},
                "comments": [{"author": {"login": "dev"}, "body": "On it", "createdAt": "2026-02-15T12:00:00Z"}]
            }
            """;
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, json, null));

        var result = await _sut.GetIssueAsync(42).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(result.Issue);
        Assert.Equal(42, result.Issue.Number);
        Assert.Equal("Bug report", result.Issue.Title);
        Assert.Equal("Description here", result.Issue.Body);
        Assert.Equal("OPEN", result.Issue.State);
        Assert.Single(result.Issue.Labels);
        Assert.Equal("bug", result.Issue.Labels[0].Name);
        Assert.Single(result.Issue.Assignees);
        Assert.Equal("user1", result.Issue.Assignees[0]);
        Assert.Equal("v1.0", result.Issue.Milestone);
        Assert.Equal("reporter", result.Issue.Author);
        Assert.Single(result.Issue.Comments);
        Assert.Equal("On it", result.Issue.Comments[0].Body);
    }

    [Fact]
    public async Task GetIssueAsync_WhenGhFails_ReturnsError()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not found"));

        var result = await _sut.GetIssueAsync(999).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("not found", result.ErrorMessage);
    }

    [Fact]
    public async Task UpdateIssueAsync_AssemblesCorrectArgs()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "https://github.com/test/issues/42", null));

        var request = new GitHubIssueUpdateRequest
        {
            Title = "New title",
            AddLabels = new[] { "bug" },
            RemoveLabels = new[] { "wontfix" }
        };
        var result = await _sut.UpdateIssueAsync(42, request).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("issue edit 42", StringComparison.Ordinal)
                && a.Contains("--title", StringComparison.Ordinal)
                && a.Contains("--add-label", StringComparison.Ordinal)
                && a.Contains("--remove-label", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task CloseIssueAsync_WithReason_IncludesReasonFlag()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CloseIssueAsync(42, "not_planned").ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("issue close 42", StringComparison.Ordinal)
                && a.Contains("--reason not_planned", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task CloseIssueAsync_WithoutReason_NoReasonFlag()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CloseIssueAsync(42).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("issue close 42", StringComparison.Ordinal)
                && !a.Contains("--reason", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task ReopenIssueAsync_WhenGhSucceeds_ReturnsSuccess()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.ReopenIssueAsync(42).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("issue reopen 42", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task ListIssueLabelsAsync_WhenGhSucceeds_ReturnsLabels()
    {
        var json = """[{"name":"bug","color":"d73a4a","description":"Something broken"},{"name":"enhancement","color":"a2eeef","description":"New feature"}]""";
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, json, null));

        var result = await _sut.ListIssueLabelsAsync().ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(result.Labels);
        Assert.Equal(2, result.Labels.Count);
        Assert.Equal("bug", result.Labels[0].Name);
        Assert.Equal("enhancement", result.Labels[1].Name);
    }

    [Fact]
    public async Task ListIssueLabelsAsync_WhenGhFails_ReturnsError()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(1, null, "not authenticated"));

        var result = await _sut.ListIssueLabelsAsync().ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("not authenticated", result.ErrorMessage);
    }
}
