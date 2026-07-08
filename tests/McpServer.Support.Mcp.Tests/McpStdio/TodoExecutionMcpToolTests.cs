using System.Text.Json;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.McpStdio;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Services.AgentHelp;
using McpServer.Support.Mcp.Storage;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using MsOptions = Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Tests.McpStdio;

/// <summary>
/// TEST-MCP-BYRD-TOOLS-001: Verifies that the Byrd STDIO MCP tools delegate to
/// <see cref="ITodoExecutionService"/> and preserve the structured JSON contracts returned to the agent.
/// </summary>
public sealed class TodoExecutionMcpToolTests : IDisposable
{
    private readonly McpDbContext _db;
    private readonly ITodoExecutionService _todoExecutionService = Substitute.For<ITodoExecutionService>();
    private readonly ITransactionGatedTodoMutationService _todoMutations = Substitute.For<ITransactionGatedTodoMutationService>();
    private readonly FwhMcpTools _tools;

    /// <summary>
    /// Initializes the STDIO tool fixture with an in-memory metadata store and substituted service
    /// dependencies so the tool methods can be exercised without a live HTTP server or adb process.
    /// </summary>
    public TodoExecutionMcpToolTests()
    {
        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"TodoExecutionMcpToolTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(dbOptions);
        _db.Database.EnsureCreated();

        var ingestionOptions = MsOptions.Options.Create(new IngestionOptions { RepoRoot = "." });
        var workspaceContext = new WorkspaceContext { WorkspacePath = "." };
        var httpContextAccessor = Substitute.For<IHttpContextAccessor>();
        var gitHubCliService = Substitute.For<IGitHubCliService>();
        var chunker = new Chunker();
        var repoIngestor = new RepoIngestor(chunker, ingestionOptions, workspaceContext, NullLogger<RepoIngestor>.Instance);
        var sessionLogIngestor = new SessionLogIngestor(chunker, ingestionOptions, workspaceContext, Substitute.For<ISessionLogService>(), NullLogger<SessionLogIngestor>.Instance);
        var externalDocsIngestor = new ExternalDocsIngestor(chunker, ingestionOptions, workspaceContext, NullLogger<ExternalDocsIngestor>.Instance);
        var gitHubIngestor = new GitHubIngestor(chunker, gitHubCliService, NullLogger<GitHubIngestor>.Instance);
        var issueIngestor = new IssueIngestor(chunker, gitHubCliService, NullLogger<IssueIngestor>.Instance);
        var websiteIngestor = Substitute.For<IWebsiteIngestor>();
        var coordinator = new IngestionCoordinator(
            _db,
            repoIngestor,
            sessionLogIngestor,
            externalDocsIngestor,
            gitHubIngestor,
            issueIngestor,
            websiteIngestor,
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IEmbeddingService>(),
            Substitute.For<IVectorIndexService>(),
            null,
            workspaceContext,
            NullLogger<IngestionCoordinator>.Instance);
        var todoService = Substitute.For<ITodoService>();
        var todoServiceResolver = new TodoServiceResolver(todoService, ingestionOptions, Substitute.For<ITodoServiceFactory>());
        var workspaceAccessor = new WorkspaceServiceAccessor(todoServiceResolver, httpContextAccessor, ingestionOptions);
        var desktopLaunchService = new DesktopLaunchService(
            Substitute.For<IConfiguration>(),
            MsOptions.Options.Create(new DesktopLaunchOptions()),
            Substitute.For<IProcessRunner>(),
            NullLogger<DesktopLaunchService>.Instance);
        var todoCreationService = new TodoCreationService(workspaceAccessor, gitHubCliService, NullLogger<TodoCreationService>.Instance);
        var todoUpdateService = new TodoUpdateService(workspaceAccessor, null, NullLogger<TodoUpdateService>.Instance);

        _tools = new FwhMcpTools(
            _db,
            Substitute.For<IRepoFileService>(),
            coordinator,
            Substitute.For<ISyncStatusStore>(),
            Substitute.For<IContextSearchService>(),
            Substitute.For<IGraphRagService>(),
            workspaceAccessor,
            Substitute.For<ITodoPromptService>(),
            Substitute.For<ISessionLogService>(),
            Substitute.For<IMemoryService>(),
            gitHubCliService,
            Substitute.For<IRequirementsDocumentService>(),
            desktopLaunchService,
            httpContextAccessor,
            workspaceContext,
            Substitute.For<IWorkspaceService>(),
            Substitute.For<IWorkspacePolicyService>(),
            todoServiceResolver,
            todoCreationService,
            todoUpdateService,
            _todoExecutionService,
            Substitute.For<IPromptTemplateService>(),
            NullLogger<FwhMcpTools>.Instance,
            todoMutations: _todoMutations,
            agentHelpService: Substitute.For<IAgentHelpConversationService>());
    }

    /// <summary>
    /// Disposes the in-memory metadata context used by the STDIO tool fixture.
    /// </summary>
    public void Dispose()
    {
        _db.Dispose();
    }

    /// <summary>
    /// TEST-MCP-BYRD-TOOLS-001: Verifies that the <c>get_active_todo</c> STDIO tool delegates to the
    /// execution service and returns the compact JSON payload expected by Codex.
    /// </summary>
    [Fact]
    public async Task GetActiveTodo_DelegatesToExecutionService()
    {
        _todoExecutionService.GetActiveTodoAsync(@"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .Returns(new ActiveTodoResult
            {
                TodoId = "TODO-201",
                Title = "Execution todo",
                Status = TodoExecutionStatus.TestDesign,
                NextAction = "Define unit tests"
            });

        var json = await _tools.GetActiveTodo(@"F:\GitHub\McpServer", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<ActiveTodoResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("TODO-201", result!.TodoId);
        Assert.Equal(TodoExecutionStatus.TestDesign, result.Status);
    }

    /// <summary>
    /// TEST-MCP-161: Verifies that the <c>todo_update</c> STDIO MCP tool routes through the transaction gate.
    /// </summary>
    [Fact]
    public async Task TodoUpdate_WhenTransactionGateRegistered_UsesGatedUpdateService()
    {
        _todoMutations.UpdateAsync("TODO-TXN-STDIO-001", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(
                true,
                null,
                new TodoFlatItem
                {
                    Id = "TODO-TXN-STDIO-001",
                    Title = "After",
                    Section = "Backlog",
                    Priority = "high",
                    Done = false,
                }));

        var json = await _tools.TodoUpdate("TODO-TXN-STDIO-001", ".", title: "After", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        await _todoMutations.Received(1)
            .UpdateAsync("TODO-TXN-STDIO-001", Arg.Any<TodoUpdateRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Verifies that the <c>todo_create</c> STDIO MCP tool routes through the transaction gate.
    /// </summary>
    [Fact]
    public async Task TodoCreate_WhenTransactionGateRegistered_UsesGatedCreateService()
    {
        _todoMutations.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(
                true,
                null,
                new TodoFlatItem
                {
                    Id = "TODO-TXN-STDIO-CREATE-001",
                    Title = "Created",
                    Section = "Backlog",
                    Priority = "high",
                    Done = false,
                }));

        var json = await _tools.TodoCreate("TODO-TXN-STDIO-CREATE-001", "Created", "Backlog", "high", ".", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        await _todoMutations.Received(1)
            .CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Verifies that the <c>todo_delete</c> STDIO MCP tool routes through the transaction gate.
    /// </summary>
    [Fact]
    public async Task TodoDelete_WhenTransactionGateRegistered_UsesGatedDeleteService()
    {
        _todoMutations.DeleteAsync("TODO-TXN-STDIO-DELETE-001", Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(true));

        var json = await _tools.TodoDelete("TODO-TXN-STDIO-DELETE-001", ".", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        await _todoMutations.Received(1)
            .DeleteAsync("TODO-TXN-STDIO-DELETE-001", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Verifies that the <c>todo_move</c> STDIO MCP tool routes through the transaction gate.
    /// </summary>
    [Fact]
    public async Task TodoMove_WhenTransactionGateRegistered_UsesGatedMoveService()
    {
        _todoMutations.MoveAsync("TODO-TXN-STDIO-MOVE-001", Arg.Any<TodoMoveRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(
                true,
                null,
                new TodoFlatItem
                {
                    Id = "TODO-TXN-STDIO-MOVE-001",
                    Title = "Moved",
                    Section = "Backlog",
                    Priority = "high",
                    Done = false,
                }));

        var json = await _tools.TodoMove(
                "TODO-TXN-STDIO-MOVE-001",
                ".",
                @"F:\GitHub\McpServer.Target", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.True(document.RootElement.GetProperty("success").GetBoolean());
        await _todoMutations.Received(1)
            .MoveAsync(
                "TODO-TXN-STDIO-MOVE-001",
                Arg.Is<TodoMoveRequest>(request => request != null && request.TargetWorkspacePath == @"F:\GitHub\McpServer.Target"),
                Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Verifies that gated <c>todo_move</c> failures return the standard error JSON.
    /// </summary>
    [Fact]
    public async Task TodoMove_WhenGatedMoveFails_ReturnsErrorJson()
    {
        _todoMutations.MoveAsync("TODO-TXN-STDIO-MOVE-002", Arg.Any<TodoMoveRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoMutationResult(
                false,
                "move rejected",
                FailureKind: TodoMutationFailureKind.Conflict));

        var json = await _tools.TodoMove(
                "TODO-TXN-STDIO-MOVE-002",
                ".",
                @"F:\GitHub\McpServer.Target", cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        using var document = JsonDocument.Parse(json);

        Assert.Equal("move rejected", document.RootElement.GetProperty("error").GetString());
        await _todoMutations.Received(1)
            .MoveAsync("TODO-TXN-STDIO-MOVE-002", Arg.Any<TodoMoveRequest>(), Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-BYRD-TOOLS-001: Verifies that the <c>create_todos_from_plan</c> STDIO tool forwards the
    /// phase and plan payload to the execution service and returns the created TODO identifiers.
    /// </summary>
    [Fact]
    public async Task CreateTodosFromPlan_DelegatesToExecutionService()
    {
        _todoExecutionService.CreateTodosFromPlanAsync(
                @"F:\GitHub\McpServer",
                Arg.Any<CreateTodosFromPlanRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new CreateTodosFromPlanResult
            {
                PhaseId = "PHASE-001",
                TodoIds = ["TODO-201", "TODO-202"],
            });

        var json = await _tools.CreateTodosFromPlan(
            @"F:\GitHub\McpServer",
            "PHASE-001",
            "PLAN-001",
            [
                new PlanTodoInput
                {
                    Title = "Execution todo",
                    Goal = "Bound execution context",
                    Summary = "Hydrate active TODO only.",
                    AcceptanceCriteria = ["Return concise requirement snippets"]
                }
            ], cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<CreateTodosFromPlanResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("PHASE-001", result!.PhaseId);
        Assert.Equal(2, result.TodoIds.Count);
        await _todoExecutionService.Received(1).CreateTodosFromPlanAsync(
            @"F:\GitHub\McpServer",
            Arg.Is<CreateTodosFromPlanRequest>(request => request != null
                && request.PhaseId == "PHASE-001"
                && request.PlanId == "PLAN-001"),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST-MCP-BYRD-TOOLS-001: Verifies that the <c>get_todo_execution_context</c> STDIO tool delegates
    /// to the execution service and preserves the bounded context payload returned to Codex.
    /// </summary>
    [Fact]
    public async Task GetTodoExecutionContext_DelegatesToExecutionService()
    {
        _todoExecutionService.GetExecutionContextAsync(
                @"F:\GitHub\McpServer",
                "TODO-201",
                3,
                2,
                Arg.Any<CancellationToken>())
            .Returns(new ActiveTodoContext
            {
                TodoId = "TODO-201",
                Title = "Execution todo",
                Status = TodoExecutionStatus.TestDesign,
                RecentRequirementSnippets = ["FR-BYRD-001: Keep context bounded."],
                RecentTurnSummaries = ["Defined test-first workflow."],
                RelevantFiles = ["src/McpServer.Services/Services/TodoExecutionService.cs"]
            });

        var json = await _tools.GetTodoExecutionContext(@"F:\GitHub\McpServer", "TODO-201", 3, 2, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<ActiveTodoContext>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("TODO-201", result!.TodoId);
        Assert.Single(result.RecentRequirementSnippets);
        Assert.Single(result.RecentTurnSummaries);
    }

    /// <summary>
    /// TEST-MCP-BYRD-TOOLS-001: Verifies that the <c>set_todo_test_plan</c> STDIO tool stores the test
    /// plan before implementation and surfaces the structured status response.
    /// </summary>
    [Fact]
    public async Task SetTodoTestPlan_DelegatesToExecutionService()
    {
        _todoExecutionService.SetTestPlanAsync(
                @"F:\GitHub\McpServer",
                "TODO-201",
                Arg.Any<SetTodoTestPlanRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new SetTodoTestPlanResult
            {
                TodoId = "TODO-201",
                Status = TodoExecutionStatus.TestReady,
            });

        var json = await _tools.SetTodoTestPlan(
            @"F:\GitHub\McpServer",
            "TODO-201",
            unitTestsDefined: true,
            testFilePaths: ["tests/TodoExecutionServiceTests.cs"],
            testCommands: ["dotnet test tests/McpServer.Support.Mcp.Tests"], cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<SetTodoTestPlanResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(TodoExecutionStatus.TestReady, result!.Status);
        await _todoExecutionService.Received(1).SetTestPlanAsync(
            @"F:\GitHub\McpServer",
            "TODO-201",
            Arg.Is<SetTodoTestPlanRequest>(request => request != null
                && request.UnitTestsDefined
                && request.TestFilePaths != null
                && request.TestFilePaths.Count == 1),
            Arg.Any<CancellationToken>());
    }

    /// <summary>
    /// TEST-MCP-BYRD-TOOLS-001: Verifies that the <c>update_todo_status</c> STDIO tool forwards the target
    /// state transition and returns the structured execution-status result.
    /// </summary>
    [Fact]
    public async Task UpdateTodoStatus_DelegatesToExecutionService()
    {
        _todoExecutionService.UpdateStatusAsync(
                @"F:\GitHub\McpServer",
                "TODO-201",
                Arg.Any<UpdateTodoStatusRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new UpdateTodoStatusResult
            {
                TodoId = "TODO-201",
                PreviousStatus = TodoExecutionStatus.TestReady,
                CurrentStatus = TodoExecutionStatus.Implementing,
            });

        var json = await _tools.UpdateTodoStatus(
            @"F:\GitHub\McpServer",
            "TODO-201",
            TodoExecutionStatus.Implementing,
            "Unit tests are defined", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<UpdateTodoStatusResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal(TodoExecutionStatus.Implementing, result!.CurrentStatus);
    }

    /// <summary>
    /// TEST-MCP-BYRD-TOOLS-001: Verifies that the <c>append_todo_checkpoint</c> STDIO tool preserves the
    /// structured checkpoint payload used to resume work from compact deltas.
    /// </summary>
    [Fact]
    public async Task AppendTodoCheckpoint_DelegatesToExecutionService()
    {
        _todoExecutionService.AppendCheckpointAsync(
                @"F:\GitHub\McpServer",
                "TODO-201",
                Arg.Any<AppendTodoCheckpointRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new AppendTodoCheckpointResult
            {
                TodoId = "TODO-201",
                CheckpointId = "CHK-001",
            });

        var json = await _tools.AppendTodoCheckpoint(
            @"F:\GitHub\McpServer",
            "TODO-201",
            TodoCheckpointKind.ImplementationProgress,
            "Implemented execution gating.",
            nextAction: "Run validation.",
            artifactIds: ["artifacts/diff.patch"], cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<AppendTodoCheckpointResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("CHK-001", result!.CheckpointId);
        Assert.Equal("TODO-201", result.TodoId);
    }

    /// <summary>
    /// TEST-MCP-BYRD-TOOLS-001: Verifies that the <c>record_todo_validation_result</c> STDIO tool forwards
    /// validation evidence and returns the structured validation state without lossy translation.
    /// </summary>
    [Fact]
    public async Task RecordTodoValidationResult_DelegatesToExecutionService()
    {
        _todoExecutionService.RecordValidationResultAsync(
                @"F:\GitHub\McpServer",
                "TODO-201",
                Arg.Any<RecordTodoValidationResultRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new RecordTodoValidationResultResult
            {
                TodoId = "TODO-201",
                ValidationState = new TodoValidationState
                {
                    LastResult = "pass",
                    Summary = "Validation succeeded."
                }
            });

        var json = await _tools.RecordTodoValidationResult(
            @"F:\GitHub\McpServer",
            "TODO-201",
            "pass",
            summary: "Validation succeeded.",
            artifactIds: ["artifacts/validation.json"],
            unitTestsPassing: true, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<RecordTodoValidationResultResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("TODO-201", result!.TodoId);
        Assert.Equal("pass", result.ValidationState.LastResult);
        Assert.Equal("Validation succeeded.", result.ValidationState.Summary);
    }

    /// <summary>
    /// TEST-MCP-BYRD-TOOLS-001: Verifies that the <c>get_next_ready_todo</c> STDIO tool delegates to the
    /// execution service and returns the next bounded TODO instead of broad plan history.
    /// </summary>
    [Fact]
    public async Task GetNextReadyTodo_DelegatesToExecutionService()
    {
        _todoExecutionService.GetNextReadyTodoAsync(@"F:\GitHub\McpServer", Arg.Any<CancellationToken>())
            .Returns(new ActiveTodoResult
            {
                TodoId = "TODO-202",
                Title = "Validate todo",
                Status = TodoExecutionStatus.Validating,
                NextAction = "Run device validation"
            });

        var json = await _tools.GetNextReadyTodo(@"F:\GitHub\McpServer", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<ActiveTodoResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.Equal("TODO-202", result!.TodoId);
        Assert.Equal(TodoExecutionStatus.Validating, result.Status);
    }

    /// <summary>
    /// TEST-MCP-BYRD-TOOLS-001: Verifies that the <c>adb_step</c> STDIO tool forwards the structured
    /// request to the execution service and returns the structured ADB response unmodified.
    /// </summary>
    [Fact]
    public async Task AdbStep_DelegatesToExecutionService()
    {
        _todoExecutionService.AdbStepAsync(
                @"F:\GitHub\McpServer",
                Arg.Any<AdbStepRequest>(),
                Arg.Any<CancellationToken>())
            .Returns(new AdbStepResult
            {
                Success = true,
                Action = AdbStepAction.Screenshot,
                DeviceSerial = "emulator-5554",
                ScreenshotPath = "artifacts/device/test.png",
                TimestampUtc = "2026-04-23T22:01:01.0000000Z",
            });

        var json = await _tools.AdbStep(
            @"F:\GitHub\McpServer",
            AdbStepAction.Screenshot,
            captureScreenshot: true, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var result = JsonSerializer.Deserialize<AdbStepResult>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(result);
        Assert.True(result!.Success);
        Assert.Equal("emulator-5554", result.DeviceSerial);
        Assert.Equal("artifacts/device/test.png", result.ScreenshotPath);
    }
}
