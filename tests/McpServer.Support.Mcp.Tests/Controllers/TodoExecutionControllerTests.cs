using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Controllers;

/// <summary>
/// TEST-MCP-BYRD-CTRL-001: Verifies controller-level behavior for the Byrd TODO execution endpoints.
/// The tests use a mocked <see cref="ITodoExecutionService"/> so routing and HTTP result shaping can be
/// validated independently of the file-backed execution-state implementation.
/// </summary>
public sealed class TodoExecutionControllerTests
{
    /// <summary>
    /// TEST-MCP-BYRD-CTRL-001: Verifies that GET /mcpserver/todo-execution/active returns 404 when the
    /// execution service does not report an active TODO for the resolved workspace.
    /// </summary>
    [Fact]
    public async Task GetActiveTodoAsync_WhenNoTodoExists_ReturnsNotFound()
    {
        var service = Substitute.For<ITodoExecutionService>();
        service.GetActiveTodoAsync(@"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .Returns((ActiveTodoResult?)null);

        var controller = CreateController(service);
        var actionResult = await controller.GetActiveTodoAsync(CancellationToken.None).ConfigureAwait(true);

        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult.Result);
        Assert.NotNull(notFound.Value);
    }

    /// <summary>
    /// TEST-MCP-BYRD-CTRL-001: Verifies that POST /mcpserver/todo-execution/phases rejects a missing JSON
    /// body with HTTP 400 before it reaches the execution service.
    /// </summary>
    [Fact]
    public async Task CreateIterationPhaseAsync_WithNullBody_ReturnsBadRequest()
    {
        var controller = CreateController(Substitute.For<ITodoExecutionService>());
        var actionResult = await controller.CreateIterationPhaseAsync(null, CancellationToken.None).ConfigureAwait(true);

        var badRequest = Assert.IsType<BadRequestObjectResult>(actionResult.Result);
        Assert.NotNull(badRequest.Value);
    }

    /// <summary>
    /// TEST-MCP-BYRD-CTRL-001: Verifies that GET /mcpserver/todo-execution/next-ready returns HTTP 200
    /// with the structured active TODO payload when the execution service reports a ready item.
    /// </summary>
    [Fact]
    public async Task GetNextReadyTodoAsync_WhenTodoExists_ReturnsOk()
    {
        var service = Substitute.For<ITodoExecutionService>();
        service.GetNextReadyTodoAsync(@"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .Returns(new ActiveTodoResult
            {
                TodoId = "TODO-202",
                Title = "Validation todo",
                Status = TodoExecutionStatus.Validating,
                NextAction = "Run validation"
            });

        var controller = CreateController(service);
        var actionResult = await controller.GetNextReadyTodoAsync(CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var result = Assert.IsType<ActiveTodoResult>(ok.Value);
        Assert.Equal("TODO-202", result.TodoId);
        Assert.Equal(TodoExecutionStatus.Validating, result.Status);
    }

    /// <summary>
    /// TEST-MCP-BYRD-CTRL-001: Verifies that GET /mcpserver/todo-execution/todos/{todoId} returns HTTP 200
    /// with the bounded execution context when the TODO exists.
    /// </summary>
    [Fact]
    public async Task GetExecutionContextAsync_WhenTodoExists_ReturnsOk()
    {
        var service = Substitute.For<ITodoExecutionService>();
        service.GetExecutionContextAsync(@"F:\GitHub\McpServer", "TODO-201", 3, 2, Arg.Any<CancellationToken>())
            .Returns(new ActiveTodoContext
            {
                TodoId = "TODO-201",
                Title = "Execution todo",
                Status = TodoExecutionStatus.TestDesign,
                RecentRequirementSnippets = ["FR-BYRD-001: Keep context bounded."]
            });

        var controller = CreateController(service);
        var actionResult = await controller.GetExecutionContextAsync("TODO-201", 3, 2, CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var result = Assert.IsType<ActiveTodoContext>(ok.Value);
        Assert.Equal("TODO-201", result.TodoId);
        Assert.Single(result.RecentRequirementSnippets);
    }

    /// <summary>
    /// TEST-MCP-BYRD-CTRL-001: Verifies that POST /mcpserver/todo-execution/phases/{phaseId}/todos uses
    /// the route phase identifier and returns the structured created TODO payload.
    /// </summary>
    [Fact]
    public async Task CreateTodosFromPlanAsync_WhenRequestValid_ReturnsOkAndUsesRoutePhaseId()
    {
        var service = Substitute.For<ITodoExecutionService>();
        service.CreateTodosFromPlanAsync(
                @"F:\GitHub\McpServer",
                Arg.Any<CreateTodosFromPlanRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new CreateTodosFromPlanResult
            {
                PhaseId = "PHASE-ROUTE",
                TodoIds = ["TODO-201"]
            });

        var controller = CreateController(service);
        var actionResult = await controller.CreateTodosFromPlanAsync(
            "PHASE-ROUTE",
            new CreateTodosFromPlanRequest
            {
                PhaseId = "IGNORED",
                PlanId = "PLAN-001",
                Todos =
                [
                    new PlanTodoInput
                    {
                        Title = "Execution todo",
                        Goal = "Bound context",
                        Summary = "Use active TODO only."
                    }
                ]
            },
            CancellationToken.None).ConfigureAwait(true);

        var ok = Assert.IsType<OkObjectResult>(actionResult.Result);
        var result = Assert.IsType<CreateTodosFromPlanResult>(ok.Value);
        Assert.Equal("PHASE-ROUTE", result.PhaseId);
        await service.Received(1).CreateTodosFromPlanAsync(
            @"F:\GitHub\McpServer",
            Arg.Is<CreateTodosFromPlanRequest>(request => request != null && request.PhaseId == "PHASE-ROUTE"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST-MCP-BYRD-CTRL-001: Verifies that POST /mcpserver/todo-execution/todos/{todoId}/status maps
    /// invalid execution transitions to HTTP 422 while preserving the error payload.
    /// </summary>
    [Fact]
    public async Task UpdateStatusAsync_WhenTransitionRejected_ReturnsUnprocessableEntity()
    {
        var service = Substitute.For<ITodoExecutionService>();
        service.UpdateStatusAsync(
                @"F:\GitHub\McpServer",
                "TODO-201",
                Arg.Any<UpdateTodoStatusRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(_ => Task.FromException<UpdateTodoStatusResult>(
                new InvalidOperationException("Unit tests must be defined before implementation.")));

        var controller = CreateController(service);
        var actionResult = await controller.UpdateStatusAsync(
            "TODO-201",
            new UpdateTodoStatusRequest
            {
                TargetStatus = TodoExecutionStatus.Implementing
            },
            CancellationToken.None).ConfigureAwait(true);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(actionResult.Result);
        Assert.NotNull(unprocessable.Value);
    }

    /// <summary>
    /// TEST-MCP-BYRD-CTRL-001: Verifies that POST /mcpserver/todo-execution/adb/step translates an
    /// unsuccessful ADB result into HTTP 422 while preserving the structured failure payload.
    /// </summary>
    [Fact]
    public async Task AdbStepAsync_WhenAdbFails_ReturnsUnprocessableEntity()
    {
        var service = Substitute.For<ITodoExecutionService>();
        service.AdbStepAsync(
                @"F:\GitHub\McpServer",
                Arg.Any<AdbStepRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new AdbStepResult
            {
                Success = false,
                Action = AdbStepAction.Screenshot,
                Error = "No connected ADB devices were found.",
                TimestampUtc = "2026-04-23T22:01:01.0000000Z",
            });

        var controller = CreateController(service);
        var actionResult = await controller.AdbStepAsync(
            new AdbStepRequest
            {
                Action = AdbStepAction.Screenshot,
                CaptureScreenshot = true
            },
            CancellationToken.None).ConfigureAwait(true);

        var unprocessable = Assert.IsType<UnprocessableEntityObjectResult>(actionResult.Result);
        var result = Assert.IsType<AdbStepResult>(unprocessable.Value);
        Assert.False(result.Success);
        Assert.Equal("No connected ADB devices were found.", result.Error);
    }

    private static TodoExecutionController CreateController(ITodoExecutionService service)
    {
        return new TodoExecutionController(
            service,
            new WorkspaceContext
            {
                WorkspacePath = @"F:\GitHub\McpServer"
            })
        {
            ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext()
            }
        };
    }
}
