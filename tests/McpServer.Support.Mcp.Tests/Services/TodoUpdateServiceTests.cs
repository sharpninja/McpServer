using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-094: Verifies the shared ISSUE-* TODO update orchestration using mocked TODO and GitHub sync
/// services. The tests use representative ISSUE-* and non-ISSUE ids so endpoint callers receive the same
/// immutable-description, sync, and comment behavior across HTTP, STDIO, and voice update paths.
/// </summary>
public sealed class TodoUpdateServiceTests
{
    private readonly ITodoService _todoService = Substitute.For<ITodoService>();
    private readonly IIssueTodoSyncService _issueSyncService = Substitute.For<IIssueTodoSyncService>();
    private readonly TodoUpdateService _sut;

    /// <summary>
    /// TEST-MCP-094: Initializes the update orchestrator with workspace-aware TODO access and mocked GitHub
    /// sync collaborators so each test can verify post-update sync behavior without touching the filesystem.
    /// </summary>
    public TodoUpdateServiceTests()
    {
        _sut = new TodoUpdateService(
            TestWorkspaceAccessorHelper.Create(_todoService),
            _issueSyncService,
            NullLogger<TodoUpdateService>.Instance);
    }

    /// <summary>
    /// TEST-MCP-094: Given an ISSUE-* TODO with changed local fields, when the shared update service runs,
    /// then it persists the local update, syncs the GitHub issue, and posts a follow-up comment describing
    /// the change set.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_IssueTodo_SyncsAndCommentsAfterLocalUpdate()
    {
        var existing = new TodoFlatItem
        {
            Id = "ISSUE-28",
            Title = "Old title",
            Section = "issues",
            Priority = "low",
            Done = false,
            Note = "Old note"
        };
        var updated = existing with
        {
            Title = "New title",
            Priority = "high",
            Note = "New note"
        };

        _todoService.GetByIdAsync("ISSUE-28", Arg.Any<CancellationToken>()).Returns(existing);
        _todoService.UpdateAsync("ISSUE-28", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true, null, updated));
        _issueSyncService.SyncTodoToIssueAsync("ISSUE-28", Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "https://github.com/test/issues/28", null));
        _issueSyncService.CommentOnTodoUpdateAsync(existing, updated, Arg.Any<CancellationToken>())
            .Returns(new GitHubCommentResult(true, null));

        var result = await _sut.UpdateAsync(
            "ISSUE-28",
            new TodoUpdateRequest
            {
                Title = "New title",
                Priority = "high",
                Note = "New note"
            }).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal("New title", result.Item?.Title);
        await _todoService.Received(1).UpdateAsync(
            "ISSUE-28",
            Arg.Is<TodoUpdateRequest>(request => MatchesUpdatedIssueRequest(request)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await _issueSyncService.Received(1).SyncTodoToIssueAsync("ISSUE-28", Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await _issueSyncService.Received(1).CommentOnTodoUpdateAsync(existing, updated, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-094: Given an ISSUE-* TODO update that only attempts to replace the description, when the
    /// shared update service normalizes the request, then the description change is ignored and neither the
    /// TODO store nor GitHub sync path is invoked.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_IssueTodo_DescriptionOnly_IsNoOp()
    {
        var existing = new TodoFlatItem
        {
            Id = "ISSUE-28",
            Title = "Immutable body",
            Section = "issues",
            Priority = "medium",
            Done = false,
            Description = ["Original description"]
        };

        _todoService.GetByIdAsync("ISSUE-28", Arg.Any<CancellationToken>()).Returns(existing);

        var result = await _sut.UpdateAsync(
            "ISSUE-28",
            new TodoUpdateRequest
            {
                Description = ["Attempted replacement"]
            }).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal(existing, result.Item);
        await _todoService.DidNotReceive().UpdateAsync(Arg.Any<string>(), Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await _issueSyncService.DidNotReceive().SyncTodoToIssueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await _issueSyncService.DidNotReceive().CommentOnTodoUpdateAsync(Arg.Any<TodoFlatItem>(), Arg.Any<TodoFlatItem>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-094: Given a normal non-ISSUE TODO update, when the shared update service runs, then it
    /// delegates to the underlying TODO store and does not invoke GitHub issue sync or change comments.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_NonIssueTodo_DoesNotCallGitHubSync()
    {
        var existing = new TodoFlatItem
        {
            Id = "MVP-APP-001",
            Title = "Existing task",
            Section = "mvp-app",
            Priority = "low",
            Done = false
        };
        var updated = existing with { Done = true };

        _todoService.GetByIdAsync("MVP-APP-001", Arg.Any<CancellationToken>()).Returns(existing);
        _todoService.UpdateAsync("MVP-APP-001", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true, null, updated));

        var result = await _sut.UpdateAsync(
            "MVP-APP-001",
            new TodoUpdateRequest
            {
                Done = true
            }).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.True(result.Item?.Done);
        await _issueSyncService.DidNotReceive().SyncTodoToIssueAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
        await _issueSyncService.DidNotReceive().CommentOnTodoUpdateAsync(Arg.Any<TodoFlatItem>(), Arg.Any<TodoFlatItem>(), Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static bool MatchesUpdatedIssueRequest(TodoUpdateRequest? request)
        => request != null
           && request.Title == "New title"
           && request.Priority == "high"
           && request.Note == "New note";
}
