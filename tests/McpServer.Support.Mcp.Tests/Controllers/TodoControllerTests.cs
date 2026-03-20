using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Tests;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-092 and TEST-MCP-097: Validates controller-level TODO behavior for canonical ISSUE creation,
/// audit-history retrieval, and failure-kind-aware mutation status mapping. The tests use a real
/// <see cref="TodoCreationService"/> / <see cref="TodoUpdateService"/> with mocked collaborators so the
/// controller exercises its production routing and result-shaping logic without requiring a live server.
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

        var controller = CreateController(todoService, gitHubCliService: gitHubCliService);
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

    /// <summary>
    /// TEST-MCP-097: Verifies that GET /mcpserver/todo/{id}/audit returns an OK payload when audit entries
    /// exist. The fixture supplies one append-only history row so the controller can be validated without
    /// involving the database implementation details.
    /// </summary>
    [Fact]
    public async Task GetAuditAsync_WhenHistoryExists_ReturnsOk()
    {
        var todoService = Substitute.For<ITodoService>();
        todoService.GetAuditAsync("MCP-TODO-097", 25, 5, Arg.Any<CancellationToken>())
            .Returns(new TodoAuditQueryResult(
            [
                new TodoAuditEntry
                {
                    AuditId = 7,
                    TodoId = "MCP-TODO-097",
                    Version = 2,
                    Action = "updated",
                    RecordedAtUtc = "2026-03-20T16:00:00.0000000Z",
                    Snapshot = new TodoFlatItem
                    {
                        Id = "MCP-TODO-097",
                        Title = "Updated",
                        Section = "mvp-app",
                        Priority = "high",
                        Done = false,
                    },
                    PreviousSnapshot = new TodoFlatItem
                    {
                        Id = "MCP-TODO-097",
                        Title = "Before",
                        Section = "mvp-app",
                        Priority = "high",
                        Done = false,
                    },
                    Source = "api"
                }
            ],
            1));

        var controller = CreateController(todoService);
        var actionResult = await controller.GetAuditAsync("MCP-TODO-097", 25, 5, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var result = Assert.IsType<TodoAuditQueryResult>(ok.Value);
        Assert.Single(result.Entries);
        Assert.Equal(7, result.Entries[0].AuditId);
        Assert.Equal("updated", result.Entries[0].Action);
    }

    /// <summary>
    /// TEST-MCP-097: Verifies that GET /mcpserver/todo/{id}/audit returns 404 when the authoritative store
    /// reports no current item and no history. The fixture returns an empty audit query result specifically
    /// to exercise the controller's not-found response path.
    /// </summary>
    [Fact]
    public async Task GetAuditAsync_WhenHistoryMissing_ReturnsNotFound()
    {
        var todoService = Substitute.For<ITodoService>();
        todoService.GetAuditAsync("MISSING-097", 50, 0, Arg.Any<CancellationToken>())
            .Returns(new TodoAuditQueryResult([], 0));

        var controller = CreateController(todoService);
        var actionResult = await controller.GetAuditAsync("MISSING-097", 50, 0, CancellationToken.None).ConfigureAwait(true);

        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        Assert.NotNull(notFound.Value);
    }

    /// <summary>
    /// TEST-MCP-097: Verifies that controller mutation failures classified as projection failures become
    /// HTTP 500 responses instead of generic conflicts. The fixture returns a projection-failed delete
    /// result because delete goes directly through <see cref="ITodoService"/> without extra orchestration.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WhenProjectionFails_ReturnsInternalServerError()
    {
        var todoService = Substitute.For<ITodoService>();
        todoService.DeleteAsync("MCP-TODO-500", Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(
                false,
                "projection failed",
                new TodoFlatItem
                {
                    Id = "MCP-TODO-500",
                    Title = "Projection failure",
                    Section = "mvp-app",
                    Priority = "high",
                    Done = false,
                },
                TodoMutationFailureKind.ProjectionFailed));

        var controller = CreateController(todoService);
        var actionResult = await controller.DeleteAsync("MCP-TODO-500", CancellationToken.None).ConfigureAwait(true);

        var objectResult = Assert.IsType<ObjectResult>(actionResult.Result);
        Assert.Equal(StatusCodes.Status500InternalServerError, objectResult.StatusCode);
        var mutation = Assert.IsType<TodoMutationResult>(objectResult.Value);
        Assert.Equal(TodoMutationFailureKind.ProjectionFailed, mutation.FailureKind);
    }

    private static TodoController CreateController(
        ITodoService todoService,
        IGitHubCliService? gitHubCliService = null,
        IIssueTodoSyncService? issueTodoSyncService = null)
    {
        gitHubCliService ??= Substitute.For<IGitHubCliService>();
        issueTodoSyncService ??= Substitute.For<IIssueTodoSyncService>();

        var creationService = new TodoCreationService(
            TestWorkspaceAccessorHelper.Create(todoService, repoRoot: "."),
            gitHubCliService,
            NullLogger<TodoCreationService>.Instance);
        var updateService = new TodoUpdateService(
            TestWorkspaceAccessorHelper.Create(todoService, repoRoot: "."),
            issueTodoSyncService,
            NullLogger<TodoUpdateService>.Instance);

        var ingestionOptions = Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = "." });
        var todoServiceFactory = Substitute.For<ITodoServiceFactory>();
        var resolver = new TodoServiceResolver(todoService, ingestionOptions, todoServiceFactory);

        return new TodoController(
            resolver,
            new WorkspaceContext(),
            Substitute.For<IWorkspaceService>(),
            Substitute.For<IRequirementsService>(),
            Substitute.For<ITodoPromptService>(),
            creationService,
            updateService)
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
