using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>TR-GH-013-003: Unit tests for IssueTodoSyncService.</summary>
public sealed class IssueTodoSyncServiceTests
{
    private readonly IGitHubCliService _github = Substitute.For<IGitHubCliService>();
    private readonly ITodoService _todoService = Substitute.For<ITodoService>();
    private readonly IssueTodoSyncService _sut;

    public IssueTodoSyncServiceTests()
    {
        var accessor = TestWorkspaceAccessorHelper.Create(_todoService);
        _sut = new IssueTodoSyncService(_github, accessor, NullLogger<IssueTodoSyncService>.Instance);
    }

    [Fact]
    public async Task SyncIssueToTodo_NewIssue_CreatesTodoWithCorrectId()
    {
        var issue = CreateTestIssue(42, "Test Bug", "OPEN");
        _todoService.GetByIdAsync("ISSUE-42", Arg.Any<CancellationToken>()).Returns((TodoFlatItem?)null);
        _todoService.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));

        var result = await _sut.SyncIssueToTodoAsync(issue).ConfigureAwait(true);

        Assert.True(result.Success);
        await _todoService.Received(1).CreateAsync(
            Arg.Is<TodoCreateRequest>(r => r != null && r.Id == "ISSUE-42" && r.Title == "Test Bug"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task SyncIssueToTodo_ExistingIssue_UpdatesTodo()
    {
        var issue = CreateTestIssue(42, "Updated Bug", "OPEN");
        _todoService.GetByIdAsync("ISSUE-42", Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem { Id = "ISSUE-42", Title = "Old Title", Section = "mvp-support", Priority = "low", Done = false });
        _todoService.UpdateAsync("ISSUE-42", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));

        var result = await _sut.SyncIssueToTodoAsync(issue).ConfigureAwait(true);

        Assert.True(result.Success);
        await _todoService.Received(1).UpdateAsync("ISSUE-42",
            Arg.Is<TodoUpdateRequest>(r => r != null && r.Title == "Updated Bug"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task SyncIssueToTodo_ClosedIssue_SetsDoneTrue()
    {
        var issue = CreateTestIssue(42, "Fixed Bug", "CLOSED");
        _todoService.GetByIdAsync("ISSUE-42", Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem { Id = "ISSUE-42", Title = "Fixed Bug", Section = "mvp-support", Priority = "low", Done = false });
        _todoService.UpdateAsync("ISSUE-42", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));

        var result = await _sut.SyncIssueToTodoAsync(issue).ConfigureAwait(true);

        Assert.True(result.Success);
        await _todoService.Received(1).UpdateAsync("ISSUE-42",
            Arg.Is<TodoUpdateRequest>(r => r != null && r.Done == true),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task SyncTodoToIssue_DoneTodo_ClosesOpenIssue()
    {
        _todoService.GetByIdAsync("ISSUE-42", Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem { Id = "ISSUE-42", Title = "Bug", Section = "mvp-support", Priority = "low", Done = true });
        _github.GetIssueAsync(42, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(true, CreateTestIssue(42, "Bug", "OPEN"), null));
        _github.CloseIssueAsync(42, "completed", Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "https://github.com/test/issues/42", null));

        var result = await _sut.SyncTodoToIssueAsync("ISSUE-42").ConfigureAwait(true);

        Assert.True(result.Success);
        await _github.Received(1).CloseIssueAsync(42, "completed", Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task SyncTodoToIssue_UndoneTodo_ReopensClosedIssue()
    {
        _todoService.GetByIdAsync("ISSUE-42", Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem { Id = "ISSUE-42", Title = "Bug", Section = "mvp-support", Priority = "low", Done = false });
        _github.GetIssueAsync(42, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(true, CreateTestIssue(42, "Bug", "CLOSED"), null));
        _github.ReopenIssueAsync(42, Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "https://github.com/test/issues/42", null));

        var result = await _sut.SyncTodoToIssueAsync("ISSUE-42").ConfigureAwait(true);

        Assert.True(result.Success);
        await _github.Received(1).ReopenIssueAsync(42, Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task SyncTodoToIssue_TitleChange_UpdatesGitHub()
    {
        _todoService.GetByIdAsync("ISSUE-42", Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem { Id = "ISSUE-42", Title = "New Title", Section = "mvp-support", Priority = "low", Done = false });
        _github.GetIssueAsync(42, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(true, CreateTestIssue(42, "Old Title", "OPEN"), null));
        _github.UpdateIssueAsync(42, Arg.Any<GitHubIssueUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "https://github.com/test/issues/42", null));

        var result = await _sut.SyncTodoToIssueAsync("ISSUE-42").ConfigureAwait(true);

        Assert.True(result.Success);
        await _github.Received(1).UpdateIssueAsync(42,
            Arg.Is<GitHubIssueUpdateRequest>(r => r != null && r.Title == "New Title"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task SyncTodoToIssue_NonIssueId_ReturnsError()
    {
        var result = await _sut.SyncTodoToIssueAsync("MVP-APP-001").ConfigureAwait(true);
        Assert.False(result.Success);
        Assert.Contains("not an ISSUE-* id", result.ErrorMessage, StringComparison.Ordinal);
    }

    [Fact]
    public void MapPriority_PriorityHighLabel_ReturnsHigh()
    {
        var labels = new[] { new GitHubLabel("priority:high", null, null), new GitHubLabel("bug", null, null) };
        Assert.Equal("high", IssueTodoSyncService.MapPriority(labels));
    }

    [Fact]
    public void MapPriority_NoLabels_ReturnsLow()
    {
        Assert.Equal("low", IssueTodoSyncService.MapPriority(Array.Empty<GitHubLabel>()));
    }

    [Fact]
    public void MapSection_AreaAppLabel_ReturnsMvpApp()
    {
        var labels = new[] { new GitHubLabel("area:app", null, null) };
        Assert.Equal("app", IssueTodoSyncService.MapSection(labels));
    }

    [Fact]
    public void MapSection_NoLabels_ReturnsMvpSupport()
    {
        Assert.Equal("issues", IssueTodoSyncService.MapSection(Array.Empty<GitHubLabel>()));
    }

    private static GitHubIssueDetail CreateTestIssue(int number, string title, string state) =>
        new(number, title, "Test body", state,
            $"https://github.com/test/issues/{number}",
            Array.Empty<GitHubLabel>(),
            Array.Empty<string>(),
            null, "2026-02-15T00:00:00Z", "2026-02-16T00:00:00Z", null, "testuser",
            Array.Empty<GitHubIssueComment>());
}
