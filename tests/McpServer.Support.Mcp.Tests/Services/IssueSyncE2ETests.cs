using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TR-GH-013-003: End-to-end bidirectional sync integration test.</summary>
public sealed class IssueSyncE2ETests
{
    private readonly IGitHubCliService _github = Substitute.For<IGitHubCliService>();
    private readonly ITodoService _todoService = Substitute.For<ITodoService>();
    private readonly IssueTodoSyncService _sut;

    public IssueSyncE2ETests()
    {
        var accessor = TestWorkspaceAccessorHelper.Create(_todoService);
        _sut = new IssueTodoSyncService(_github, accessor, NullLogger<IssueTodoSyncService>.Instance);
    }

    /// <summary>Full cycle: Sync issue from GitHub -> modify TODO -> sync back to GitHub.</summary>
    [Fact]
    public async Task FullBidirectionalSync_CreateThenSyncBack()
    {
        // Step 1: Sync issue from GitHub to TODO
        var issue = new GitHubIssueDetail(
            42, "Feature request", "Please add X", "OPEN",
            "https://github.com/test/issues/42",
            new[] { new GitHubLabel("priority:high", null, null), new GitHubLabel("area:app", null, null) },
            new[] { "dev1" },
            null, "2026-02-15T00:00:00Z", "2026-02-16T00:00:00Z", null, "reporter",
            Array.Empty<GitHubIssueComment>());

        _todoService.GetByIdAsync("ISSUE-42", Arg.Any<CancellationToken>()).Returns((TodoFlatItem?)null);
        _todoService.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));
        _todoService.UpdateAsync("ISSUE-42", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));

        var createResult = await _sut.SyncIssueToTodoAsync(issue).ConfigureAwait(true);
        Assert.True(createResult.Success);

        // Verify it was created with correct priority and section from labels
        await _todoService.Received(1).CreateAsync(
            Arg.Is<TodoCreateRequest>(r => r != null && r.Id == "ISSUE-42" && r.Priority == "high" && r.Section == "app"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);

        // Step 2: TODO is marked done -> sync back should close GitHub issue
        _todoService.GetByIdAsync("ISSUE-42", Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem
            {
                Id = "ISSUE-42",
                Title = "Feature request",
                Section = "mvp-app",
                Priority = "high",
                Done = true
            });

        _github.GetIssueAsync(42, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(true, issue, null));
        _github.CloseIssueAsync(42, "completed", Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "https://github.com/test/issues/42", null));

        var syncBackResult = await _sut.SyncTodoToIssueAsync("ISSUE-42").ConfigureAwait(true);
        Assert.True(syncBackResult.Success);

        await _github.Received(1).CloseIssueAsync(42, "completed", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>SyncAllIssuesToTodosAsync batch imports multiple issues.</summary>
    [Fact]
    public async Task SyncAllIssuesToTodos_BatchImports()
    {
        var issues = new[]
        {
            new GitHubIssueItem(1, "Bug 1", "https://github.com/test/issues/1", "open"),
            new GitHubIssueItem(2, "Bug 2", "https://github.com/test/issues/2", "closed")
        };
        _github.ListIssuesAsync("open", 30, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueListResult(true, null, issues));

        _github.GetIssueAsync(1, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(true, CreateIssue(1, "Bug 1", "OPEN"), null));
        _github.GetIssueAsync(2, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(true, CreateIssue(2, "Bug 2", "CLOSED"), null));

        _todoService.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns((TodoFlatItem?)null);
        _todoService.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));
        _todoService.UpdateAsync(Arg.Any<string>(), Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));

        var result = await _sut.SyncAllIssuesToTodosAsync("open", 30).ConfigureAwait(true);

        Assert.Equal(2, result.Synced);
        Assert.Equal(0, result.Failed);
    }

    /// <summary>SyncAllTodosToIssuesAsync batch exports ISSUE-* TODOs.</summary>
    [Fact]
    public async Task SyncAllTodosToIssues_BatchExports()
    {
        var items = new[]
        {
            new TodoFlatItem { Id = "ISSUE-1", Title = "Bug 1", Section = "mvp-support", Priority = "low", Done = true },
            new TodoFlatItem { Id = "MVP-APP-001", Title = "Non-issue", Section = "mvp-app", Priority = "high", Done = false }
        };
        _todoService.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult(items, 2));

        _todoService.GetByIdAsync("ISSUE-1", Arg.Any<CancellationToken>())
            .Returns(items[0]);

        _github.GetIssueAsync(1, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(true, CreateIssue(1, "Bug 1", "OPEN"), null));
        _github.CloseIssueAsync(1, "completed", Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "url", null));

        var result = await _sut.SyncAllTodosToIssuesAsync().ConfigureAwait(true);

        // Only ISSUE-1 should be synced (MVP-APP-001 is not an ISSUE-* id)
        Assert.Equal(1, result.Synced);
    }

    private static GitHubIssueDetail CreateIssue(int number, string title, string state) =>
        new(number, title, "body", state,
            $"https://github.com/test/issues/{number}",
            Array.Empty<GitHubLabel>(), Array.Empty<string>(),
            null, "2026-02-15", "2026-02-16", null, "user",
            Array.Empty<GitHubIssueComment>());
}
