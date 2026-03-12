using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Tests;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-092: Validates the TODO controller's create contract when ISSUE-NEW requests are rewritten
/// to canonical ISSUE-{number} identifiers. The tests use a real <see cref="TodoCreationService"/> backed
/// by mocked GitHub and TODO services so the controller's response URI reflects the persisted canonical id.
/// </summary>
public sealed class TodoControllerTests
{
    /// <summary>
    /// TEST-MCP-092: Verifies that POST /mcpserver/todo returns a Created response whose location matches
    /// the canonical ISSUE-{number} identifier produced by the shared ISSUE-NEW creation flow rather than
    /// echoing the create-time ISSUE-NEW sentinel back to the caller.
    /// </summary>
    [Fact]
    public async Task CreateAsync_IssueNew_UsesCanonicalCreatedLocation()
    {
        var todoService = Substitute.For<ITodoService>();
        var gitHubCliService = Substitute.For<IGitHubCliService>();

        gitHubCliService.CreateIssueAsync(
                "Create canonical issue todo",
                Arg.Any<string>(),
                Arg.Any<CancellationToken>())
            .Returns(new GitHubCreateIssueResult(true, 28, "https://github.com/test/issues/28", null));

        todoService.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(
                true,
                null,
                new TodoFlatItem
                {
                    Id = "ISSUE-28",
                    Title = "Create canonical issue todo",
                    Section = "issues",
                    Priority = "high",
                    Done = false
                }));

        var creationService = new TodoCreationService(
            TestWorkspaceAccessorHelper.Create(todoService, repoRoot: "."),
            gitHubCliService,
            NullLogger<TodoCreationService>.Instance);
        var updateService = new TodoUpdateService(
            TestWorkspaceAccessorHelper.Create(todoService, repoRoot: "."),
            Substitute.For<IIssueTodoSyncService>(),
            NullLogger<TodoUpdateService>.Instance);

        var ingestionOptions = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = "." });
        var todoServiceFactory = Substitute.For<ITodoServiceFactory>();
        var resolver = new TodoServiceResolver(todoService, ingestionOptions, todoServiceFactory);

        var controller = new TodoController(
            resolver,
            new WorkspaceContext(),
            Substitute.For<IWorkspaceService>(),
            Substitute.For<IRequirementsService>(),
            Substitute.For<ITodoPromptService>(),
            creationService,
            updateService);

        var actionResult = await controller.CreateAsync(
            new TodoCreateRequest
            {
                Id = TodoCreationService.NewGitHubIssueTodoId,
                Title = "Create canonical issue todo",
                Section = "issues",
                Priority = "high"
            },
            CancellationToken.None).ConfigureAwait(true);

        var created = Assert.IsType<CreatedResult>(actionResult.Result);
        var mutation = Assert.IsType<TodoMutationResult>(created.Value);
        Assert.Equal("/mcpserver/todo/ISSUE-28", created.Location);
        Assert.True(mutation.Success);
        Assert.Equal("ISSUE-28", mutation.Item?.Id);
    }
}
