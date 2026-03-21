using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

    /// <summary>
    /// TEST-MCP-GH-006: Verifies that issue comment targets are emitted after an explicit end-of-options
    /// marker so a flag-shaped identifier cannot be reinterpreted as an injected gh CLI option.
    /// </summary>
    [Fact]
    public async Task CommentOnIssueAsync_WithFlagLikeIdentifier_UsesEndOfOptionsMarker()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CommentOnIssueAsync("--repo", "test comment").ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("issue comment --body \"test comment\" -- --repo", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task CommentOnPullAsync_VerifiesGhArgs()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CommentOnPullAsync("42", "PR comment").ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("pr comment --body \"PR comment\" -- 42", StringComparison.Ordinal)),
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

    /// <summary>
    /// TEST-MCP-GH-006: Verifies that invalid close reasons are rejected before launching the GitHub CLI so
    /// attacker-controlled query strings cannot append extra flags through the close-issue reason parameter.
    /// </summary>
    [Fact]
    public async Task CloseIssueAsync_WithInvalidReason_DoesNotInvokeGh()
    {
        var result = await _sut.CloseIssueAsync(42, "completed --repo other/repo").ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("Invalid close reason. Allowed values: completed, not_planned.", result.ErrorMessage);
        await _processRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default!, default).ConfigureAwait(true);
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

    /// <summary>
    /// TEST-MCP-GH-006: Verifies that invalid list-state values are rejected before process launch so state
    /// query parameters cannot smuggle additional GitHub CLI flags into the issue-list command line.
    /// </summary>
    [Fact]
    public async Task ListIssuesAsync_WithInvalidState_DoesNotInvokeGh()
    {
        var result = await _sut.ListIssuesAsync("open --repo other/repo", 10).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal("Invalid state. Allowed values: open, closed, all.", result.Error);
        await _processRunner.DidNotReceiveWithAnyArgs().RunAsync(default!, default!, default).ConfigureAwait(true);
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

    [Fact]
    public async Task ListWorkflowRunsAsync_WhenGhSucceeds_ReturnsRuns()
    {
        var json = """[{"databaseId":101,"workflowName":"CI","displayTitle":"build","headBranch":"main","status":"completed","conclusion":"success","event":"push","url":"https://github.com/x/actions/runs/101","createdAt":"2026-03-01T00:00:00Z","updatedAt":"2026-03-01T00:05:00Z"}]""";
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, json, null));

        var result = await _sut.ListWorkflowRunsAsync(new GitHubWorkflowRunQuery { Limit = 10 }).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Single(result.Runs);
        Assert.Equal(101, result.Runs[0].RunId);
        Assert.Equal("CI", result.Runs[0].WorkflowName);
    }

    [Fact]
    public async Task GetWorkflowRunAsync_WhenGhSucceeds_ReturnsRun()
    {
        var json = """
            {
                "databaseId": 202,
                "workflowName": "Deploy",
                "displayTitle": "release",
                "headBranch": "main",
                "headSha": "abc123",
                "status": "completed",
                "conclusion": "success",
                "event": "workflow_dispatch",
                "url": "https://github.com/x/actions/runs/202",
                "attempt": 1,
                "createdAt": "2026-03-01T00:00:00Z",
                "updatedAt": "2026-03-01T00:10:00Z",
                "jobs": [
                    {
                        "name": "build",
                        "status": "completed",
                        "conclusion": "success",
                        "startedAt": "2026-03-01T00:01:00Z",
                        "completedAt": "2026-03-01T00:09:00Z",
                        "url": "https://github.com/x/actions/runs/202/job/1",
                        "steps": [
                            { "name": "checkout", "status": "completed", "conclusion": "success", "number": 1 }
                        ]
                    }
                ]
            }
            """;
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, json, null));

        var result = await _sut.GetWorkflowRunAsync(202).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.NotNull(result.Run);
        Assert.Equal(202, result.Run.RunId);
        Assert.Single(result.Run.Jobs);
        Assert.Single(result.Run.Jobs[0].Steps);
        Assert.Equal("checkout", result.Run.Jobs[0].Steps[0].Name);
    }

    [Fact]
    public async Task RerunWorkflowRunAsync_UsesRerunCommand()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.RerunWorkflowRunAsync(303).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("run rerun 303", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task CancelWorkflowRunAsync_UsesCancelCommand()
    {
        _processRunner.RunAsync("gh", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "", null));

        var result = await _sut.CancelWorkflowRunAsync(404).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync("gh",
            Arg.Is<string>(a => a != null && a.Contains("run cancel 404", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task ListIssuesAsync_WithStoredWorkspaceToken_UsesProcessRunRequestOverride()
    {
        var tokenStore = Substitute.For<IGitHubWorkspaceTokenStore>();
        tokenStore.GetAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubWorkspaceTokenRecord("C:\\workspace", "gho_stored", DateTimeOffset.UtcNow, null));

        var options = Substitute.For<IOptionsMonitor<GitHubIntegrationOptions>>();
        options.CurrentValue.Returns(new GitHubIntegrationOptions
        {
            PreferStoredToken = true,
            AllowCliFallback = true,
        });

        var services = new ServiceCollection();
        services.AddScoped(_ => new WorkspaceContext { WorkspacePath = "C:\\workspace" });
        using var provider = services.BuildServiceProvider();
        using var scope = provider.CreateScope();
        var accessor = Substitute.For<IHttpContextAccessor>();
        accessor.HttpContext.Returns(new DefaultHttpContext { RequestServices = scope.ServiceProvider });

        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "[]", null));

        var workspaceAccessor = TestWorkspaceAccessorHelper.Create(Substitute.For<ITodoService>(), repoRoot: "C:\\workspace");
        var sut = new GitHubCliService(_processRunner, NullLogger<GitHubCliService>.Instance, tokenStore, accessor, options, workspaceAccessor);
        var result = await sut.ListIssuesAsync("open", 10).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(r => r != null
                && r.FileName == "gh"
                && r.GitHubTokenOverride == "gho_stored"
                && r.WorkingDirectory == Path.GetFullPath("C:\\workspace")),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task ListIssuesAsync_WithWorkspaceAccessor_UsesWorkspaceWorkingDirectory()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(new ProcessRunResult(0, "[]", null));

        var workspaceAccessor = TestWorkspaceAccessorHelper.Create(Substitute.For<ITodoService>(), repoRoot: "C:\\repo\\workspace");
        var sut = new GitHubCliService(_processRunner, NullLogger<GitHubCliService>.Instance, workspaceAccessor: workspaceAccessor);

        var result = await sut.ListIssuesAsync("open", 5).ConfigureAwait(true);

        Assert.True(result.Success);
        await _processRunner.Received(1).RunAsync(
            Arg.Is<ProcessRunRequest>(request => request != null
                && request.FileName == "gh"
                && request.WorkingDirectory == Path.GetFullPath("C:\\repo\\workspace")),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }
}
