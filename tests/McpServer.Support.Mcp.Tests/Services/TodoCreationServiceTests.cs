using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Tests;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-092: Validates the shared TODO creation flow for canonical ISSUE-* persistence and the
/// create-time ISSUE-NEW alias. The tests use mocked GitHub CLI and TODO storage services so the
/// orchestration logic can be verified without mutating a real repository or GitHub project.
/// </summary>
public sealed class TodoCreationServiceTests
{
    private readonly IGitHubCliService _gitHubCliService = Substitute.For<IGitHubCliService>();
    private readonly ITodoService _todoService = Substitute.For<ITodoService>();
    private readonly TodoCreationService _sut;

    /// <summary>
    /// Initializes a new test fixture using a workspace accessor that resolves to a mock TODO service.
    /// The accessor uses the current repository root so the create orchestration follows the same
    /// workspace-aware path used by the production service.
    /// </summary>
    public TodoCreationServiceTests()
    {
        var accessor = TestWorkspaceAccessorHelper.Create(_todoService, repoRoot: ".");
        _sut = new TodoCreationService(accessor, _gitHubCliService, NullLogger<TodoCreationService>.Instance);
    }

    /// <summary>
    /// TEST-MCP-092: Verifies that ISSUE-NEW creates a GitHub issue first, rewrites the persisted TODO id
    /// to the canonical ISSUE-{number} format, and preserves the note/body metadata needed to correlate the
    /// local TODO with the newly created GitHub issue.
    /// </summary>
    [Fact]
    public async Task CreateAsync_IssueNew_PersistsCanonicalIssueTodo()
    {
        _gitHubCliService.CreateIssueAsync(
                "GraphRAG diagnostic follow-up",
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new GitHubCreateIssueResult(true, 28, "https://github.com/test/issues/28", null));

        _todoService.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<TodoCreateRequest>()!;
                return new TodoMutationResult(
                    true,
                    null,
                    new TodoFlatItem
                    {
                        Id = request.Id,
                        Title = request.Title,
                        Section = request.Section,
                        Priority = request.Priority,
                        Done = false,
                        Note = request.Note,
                        Description = request.Description
                    });
            });

        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = TodoCreationService.NewGitHubIssueTodoId,
            Title = "GraphRAG diagnostic follow-up",
            Section = "diagnostics",
            Priority = "high",
            Description = ["Capture the verified local indexing failure."],
            TechnicalDetails = ["Context and GraphRAG queries returned no hits for exact local snippets."],
            Note = "Preserve diagnosis-only framing."
        }).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal("ISSUE-28", result.Item?.Id);
        Assert.Contains("status: OPEN", result.Item?.Note ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("github-url: https://github.com/test/issues/28", result.Item?.Note ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("Preserve diagnosis-only framing.", result.Item?.Note ?? string.Empty, StringComparison.Ordinal);

        await _gitHubCliService.Received(1).CreateIssueAsync(
            "GraphRAG diagnostic follow-up",
            Arg.Is<string>(body => body != null
                && body.Contains("Requested section: diagnostics", StringComparison.Ordinal)
                && body.Contains("Capture the verified local indexing failure.", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);

        await _todoService.Received(1).CreateAsync(
            Arg.Is<TodoCreateRequest>(request => request != null
                && request.Id == "ISSUE-28"
                && request.Title == "GraphRAG diagnostic follow-up"
                && request.Note != null
                && request.Note.Contains("github-url: https://github.com/test/issues/28", StringComparison.Ordinal)),
            Arg.Any<CancellationToken>()).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-092: Verifies that the create flow reports when GitHub issue creation succeeded but local
    /// TODO persistence failed. The returned error must keep the GitHub issue URL visible so the caller can
    /// reconcile the partially completed operation without guessing which issue was created.
    /// </summary>
    [Fact]
    public async Task CreateAsync_IssueNew_LocalPersistenceFailure_IncludesIssueUrl()
    {
        _gitHubCliService.CreateIssueAsync(
                "Persist canonical issue todo",
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new GitHubCreateIssueResult(true, 91, "https://github.com/test/issues/91", null));

        _todoService.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(false, "Item with id 'ISSUE-91' already exists."));

        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = TodoCreationService.NewGitHubIssueTodoId,
            Title = "Persist canonical issue todo",
            Section = "issues",
            Priority = "low"
        }).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Contains("ISSUE-91", result.Error ?? string.Empty, StringComparison.Ordinal);
        Assert.Contains("https://github.com/test/issues/91", result.Error ?? string.Empty, StringComparison.Ordinal);
    }
}
