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

        var result = await _sut.SyncIssueToTodoAsync(issue, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

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

        var result = await _sut.SyncIssueToTodoAsync(issue, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _todoService.Received(1).UpdateAsync("ISSUE-42",
            Arg.Is<TodoUpdateRequest>(r => r != null && r.Title == "Updated Bug"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task SyncIssueToTodo_ExistingIssue_PreservesLocalPriorityAndDescription()
    {
        var issue = new GitHubIssueDetail(
            42,
            "Updated Bug",
            "Fresh body from GitHub",
            "OPEN",
            "https://github.com/test/issues/42",
            new[] { new GitHubLabel("priority: LOW", null, null) },
            Array.Empty<string>(),
            null,
            "2026-02-15T00:00:00Z",
            "2026-02-16T00:00:00Z",
            null,
            "testuser",
            Array.Empty<GitHubIssueComment>());

        _todoService.GetByIdAsync("ISSUE-42", Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem
            {
                Id = "ISSUE-42",
                Title = "Old Title",
                Section = "mvp-support",
                Priority = "high",
                Done = false,
                Description = ["Keep existing body preview"],
                Note = "status: OPEN"
            });
        _todoService.UpdateAsync("ISSUE-42", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));

        var result = await _sut.SyncIssueToTodoAsync(issue, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _todoService.Received(1).UpdateAsync(
            "ISSUE-42",
            Arg.Is<TodoUpdateRequest>(request => MatchesPreservedIssueUpdate(request)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-095: Verifies that an existing <c>ISSUE-*</c> TODO keeps its local description while
    /// GitHub-origin comments are merged into a generated note section. The test uses an existing TODO
    /// with user-authored note text plus a stale generated comments block so the assertion proves that
    /// sync refreshes GitHub comments without deleting the local note body or reintroducing description
    /// churn. Validates FR-MCP-071 and TR-MCP-GH-007.
    /// </summary>
    [Fact]
    public async Task SyncIssueToTodo_ExistingIssue_MergesGitHubCommentsIntoNoteAndPreservesDescription()
    {
        var issue = new GitHubIssueDetail(
            42,
            "Updated Bug",
            "Fresh body from GitHub",
            "OPEN",
            "https://github.com/test/issues/42",
            new[] { new GitHubLabel("priority: LOW", null, null) },
            Array.Empty<string>(),
            null,
            "2026-02-15T00:00:00Z",
            "2026-02-16T00:00:00Z",
            null,
            "testuser",
            new[]
            {
                new GitHubIssueComment("octocat", "GitHub says hello from the issue thread.", "2026-03-11T12:00:00Z")
            });

        _todoService.GetByIdAsync("ISSUE-42", Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem
            {
                Id = "ISSUE-42",
                Title = "Old Title",
                Section = "issues",
                Priority = "high",
                Done = false,
                Description = ["Keep existing body preview"],
                Note = """
                    status: OPEN
                    github-url: https://github.com/test/issues/42

                    Local analyst note that must survive sync.

                    <!-- BEGIN MCP GITHUB COMMENTS -->
                    ## GitHub Comments

                    ### stale-user | 2026-03-10T00:00:00Z
                    Old generated comment
                    <!-- END MCP GITHUB COMMENTS -->
                    """
            });
        _todoService.UpdateAsync("ISSUE-42", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));

        var result = await _sut.SyncIssueToTodoAsync(issue, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _todoService.Received(1).UpdateAsync(
            "ISSUE-42",
            Arg.Is<TodoUpdateRequest>(request => MatchesGitHubCommentMergeUpdate(request)),
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

        var result = await _sut.SyncIssueToTodoAsync(issue, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

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

        var result = await _sut.SyncTodoToIssueAsync("ISSUE-42", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

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

        var result = await _sut.SyncTodoToIssueAsync("ISSUE-42", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

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

        var result = await _sut.SyncTodoToIssueAsync("ISSUE-42", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _github.Received(1).UpdateIssueAsync(42,
            Arg.Is<GitHubIssueUpdateRequest>(r => r != null && r.Title == "New Title"),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task SyncTodoToIssue_PriorityChange_CanonicalizesGitHubPriorityLabel()
    {
        _todoService.GetByIdAsync("ISSUE-42", Arg.Any<CancellationToken>())
            .Returns(new TodoFlatItem { Id = "ISSUE-42", Title = "Bug", Section = "mvp-support", Priority = "high", Done = false });
        _github.GetIssueAsync(42, Arg.Any<CancellationToken>())
            .Returns(new GitHubIssueDetailResult(
                true,
                new GitHubIssueDetail(
                    42,
                    "Bug",
                    "Body",
                    "OPEN",
                    "https://github.com/test/issues/42",
                    new[] { new GitHubLabel("priority:low", null, null), new GitHubLabel("bug", null, null) },
                    Array.Empty<string>(),
                    null,
                    "2026-02-15T00:00:00Z",
                    "2026-02-16T00:00:00Z",
                    null,
                    "testuser",
                    Array.Empty<GitHubIssueComment>()),
                null));
        _github.UpdateIssueAsync(42, Arg.Any<GitHubIssueUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubMutationResult(true, "https://github.com/test/issues/42", null));

        var result = await _sut.SyncTodoToIssueAsync("ISSUE-42", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _github.Received(1).UpdateIssueAsync(
            42,
            Arg.Is<GitHubIssueUpdateRequest>(request => MatchesPriorityLabelUpdate(request)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    [Fact]
    public async Task SyncTodoToIssue_NonIssueId_ReturnsError()
    {
        var result = await _sut.SyncTodoToIssueAsync("MVP-APP-001", ct: TestContext.Current.CancellationToken).ConfigureAwait(true);
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
    public void MapPriority_CanonicalPriorityHighLabel_ReturnsHigh()
    {
        var labels = new[] { new GitHubLabel("priority: HIGH", null, null) };
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

    [Fact]
    public async Task CommentOnTodoUpdateAsync_ChangedFields_PostsComment()
    {
        var previous = new TodoFlatItem
        {
            Id = "ISSUE-42",
            Title = "Old Title",
            Section = "issues",
            Priority = "low",
            Done = false
        };
        var current = previous with
        {
            Title = "New Title",
            Priority = "high",
            Done = true
        };

        _github.CommentOnIssueAsync("42", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubCommentResult(true, null));

        var result = await _sut.CommentOnTodoUpdateAsync(previous, current, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _github.Received(1).CommentOnIssueAsync(
            "42",
            Arg.Is<string>(body => HasExpectedChangeComment(body)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-095: Verifies that TODO-authored note text appended outside the generated GitHub comment
    /// block is exported back to GitHub as a real comment body instead of collapsing to a generic
    /// "note updated" summary. The fixture uses frontmatter plus a generated GitHub comments section so
    /// extraction proves the service isolates user-authored text from generated sync metadata. Validates
    /// FR-MCP-071 and TR-MCP-GH-007.
    /// </summary>
    [Fact]
    public async Task CommentOnTodoUpdateAsync_AppendedNote_PostsGitHubCommentWithCommentText()
    {
        var previous = new TodoFlatItem
        {
            Id = "ISSUE-42",
            Title = "Bug",
            Section = "issues",
            Priority = "low",
            Done = false,
            Note = """
                status: OPEN
                github-url: https://github.com/test/issues/42

                <!-- BEGIN MCP GITHUB COMMENTS -->
                ## GitHub Comments

                ### octocat | 2026-03-11T12:00:00Z
                Existing GitHub discussion.
                <!-- END MCP GITHUB COMMENTS -->
                """
        };
        var current = previous with
        {
            Priority = "high",
            Note = previous.Note + Environment.NewLine + Environment.NewLine + "Local TODO follow-up comment."
        };

        _github.CommentOnIssueAsync("42", Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new GitHubCommentResult(true, null));

        var result = await _sut.CommentOnTodoUpdateAsync(previous, current, ct: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        await _github.Received(1).CommentOnIssueAsync(
            "42",
            Arg.Is<string>(body => HasExpectedAppendedComment(body)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    private static bool MatchesPreservedIssueUpdate(TodoUpdateRequest? request)
        => request != null
           && string.Equals(request.Priority, "high", StringComparison.Ordinal)
           && request.Description == null
           && request.Note != null
           && request.Note.Contains("github-url: https://github.com/test/issues/42", StringComparison.Ordinal);

    private static bool MatchesGitHubCommentMergeUpdate(TodoUpdateRequest? request)
        => request != null
           && string.Equals(request.Priority, "high", StringComparison.Ordinal)
           && request.Description == null
           && request.Note != null
           && request.Note.Contains("Local analyst note that must survive sync.", StringComparison.Ordinal)
           && request.Note.Contains("GitHub says hello from the issue thread.", StringComparison.Ordinal)
           && !request.Note.Contains("Old generated comment", StringComparison.Ordinal);

    private static bool MatchesPriorityLabelUpdate(GitHubIssueUpdateRequest? request)
        => request != null
           && request.Title == null
           && request.Body == null
           && request.AddLabels != null
           && request.AddLabels.Contains("priority: HIGH", StringComparer.Ordinal)
           && request.RemoveLabels != null
           && request.RemoveLabels.Contains("priority:low", StringComparer.Ordinal);

    private static bool HasExpectedChangeComment(string? body)
        => body != null
           && body.Contains("Title: \"Old Title\" -> \"New Title\"", StringComparison.Ordinal)
           && body.Contains("Priority: priority: LOW -> priority: HIGH", StringComparison.Ordinal)
           && body.Contains("Done: false -> true", StringComparison.Ordinal);

    private static bool HasExpectedAppendedComment(string? body)
        => body != null
           && body.Contains("Priority: priority: LOW -> priority: HIGH", StringComparison.Ordinal)
           && body.Contains("Comment added:", StringComparison.Ordinal)
           && body.Contains("Local TODO follow-up comment.", StringComparison.Ordinal);

    private static GitHubIssueDetail CreateTestIssue(int number, string title, string state) =>
        new(number, title, "Test body", state,
            $"https://github.com/test/issues/{number}",
            new[] { new GitHubLabel("priority: LOW", null, null) },
            Array.Empty<string>(),
            null, "2026-02-15T00:00:00Z", "2026-02-16T00:00:00Z", null, "testuser",
            Array.Empty<GitHubIssueComment>());
}
