using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Requirements.Models;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-BYRD-SVC-001: Validates the file-backed Byrd execution workflow service, including
/// test-plan gating, bounded context hydration, checkpoint deltas, validation-driven completion,
/// and safe Android device interactions through structured ADB commands.
/// </summary>
public sealed class TodoExecutionServiceTests : IDisposable
{
    private readonly McpDbContext _db;
    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
    private readonly IRequirementsDocumentService _requirementsDocumentService = Substitute.For<IRequirementsDocumentService>();
    private readonly TodoExecutionService _sut;
    private readonly ITodoService _todoService = Substitute.For<ITodoService, ITodoCompensationService, ITodoCompensationCapability>();
    private readonly string _workspacePath;

    /// <summary>
    /// Initializes a new test fixture with an isolated workspace directory, an in-memory session-log
    /// database, and substituted collaborators for requirements, TODO storage, workspace lookup, and ADB.
    /// </summary>
    public TodoExecutionServiceTests()
    {
        _workspacePath = Path.Combine(Path.GetTempPath(), "TodoExecutionServiceTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspacePath);

        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseInMemoryDatabase($"TodoExecutionServiceTests_{Guid.NewGuid():N}")
            .Options;
        _db = new McpDbContext(dbOptions, new WorkspaceContext { WorkspacePath = _workspacePath });
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(_workspacePath);

        var workspaceService = Substitute.For<IWorkspaceService>();
        workspaceService.GetAsync(_workspacePath, Arg.Any<CancellationToken>())
            .Returns(new WorkspaceDto
            {
                WorkspacePath = _workspacePath,
                Name = "TodoExecutionServiceTests",
                TodoPath = "docs/Project/TODO.yaml",
                DataDirectory = _workspacePath,
                DateTimeCreated = DateTimeOffset.UtcNow,
                DateTimeModified = DateTimeOffset.UtcNow,
                StatusPrompt = "status",
                ImplementPrompt = "implement",
                PlanPrompt = "plan",
            });

        _todoService.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((McpServer.Support.Mcp.Services.TodoFlatItem?)null);
        _todoService.CreateAsync(Arg.Any<McpServer.Support.Mcp.Services.TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<McpServer.Support.Mcp.Services.TodoCreateRequest>()!;
                return new McpServer.Support.Mcp.Services.TodoMutationResult(
                    true,
                    null,
                    new McpServer.Support.Mcp.Services.TodoFlatItem
                    {
                        Id = request.Id,
                        Title = request.Title,
                        Section = request.Section,
                        Priority = request.Priority,
                        Done = false,
                        Description = request.Description,
                        Remaining = request.Remaining,
                    });
            });
        ((ITodoCompensationCapability)_todoService).SupportsRollbackCompensation.Returns(true);
        ((ITodoCompensationService)_todoService)
            .DeleteCreatedAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(new McpServer.Support.Mcp.Services.TodoMutationResult(true));

        var resolver = new TodoServiceResolver(
            _todoService,
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions { RepoRoot = _workspacePath }),
            Substitute.For<ITodoServiceFactory>());

        _sut = new TodoExecutionService(
            resolver,
            workspaceService,
            _requirementsDocumentService,
            _db,
            _processRunner,
            NullLogger<TodoExecutionService>.Instance);
    }

    /// <summary>
    /// Deletes the temporary workspace directory and disposes the in-memory EF Core context used by the
    /// test fixture so no execution-state files leak into subsequent test runs.
    /// </summary>
    public void Dispose()
    {
        _db.Dispose();
        if (Directory.Exists(_workspacePath))
            Directory.Delete(_workspacePath, recursive: true);
    }

    /// <summary>
    /// TEST-MCP-BYRD-SVC-001: Verifies Byrd plan expansion emits canonical TODO identifiers that comply
    /// with the shared TODO naming contract enforced by the legacy MCP TODO store.
    /// </summary>
    [Fact]
    public async Task CreateTodosFromPlanAsync_GeneratesCanonicalTodoIds()
    {
        var phase = await _sut.CreateIterationPhaseAsync(
            _workspacePath,
            new CreateIterationPhaseRequest
            {
                Name = "Execution phase",
                Summary = "Bounded execution tests"
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await _sut.CreateTodosFromPlanAsync(
            _workspacePath,
            new CreateTodosFromPlanRequest
            {
                PhaseId = phase.PhaseId,
                PlanId = "PLAN-001",
                Todos =
                [
                    new PlanTodoInput
                    {
                        Title = "Execution todo one",
                        Goal = "Support Byrd execution.",
                        Summary = "Create first canonical execution TODO."
                    },
                    new PlanTodoInput
                    {
                        Title = "Execution todo two",
                        Goal = "Support Byrd execution.",
                        Summary = "Create second canonical execution TODO."
                    }
                ]
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal("EXEC-TODO-001", result.TodoIds[0]);
        Assert.Equal("EXEC-TODO-002", result.TodoIds[1]);
    }

    /// <summary>
    /// TEST-MCP-161: Verifies plan expansion deletes already-created legacy TODO rows when a later
    /// legacy create fails before the execution-state file can be saved.
    /// </summary>
    [Fact]
    public async Task CreateTodosFromPlanAsync_WhenLaterLegacyCreateFails_DeletesAlreadyCreatedTodo()
    {
        var phase = await _sut.CreateIterationPhaseAsync(
            _workspacePath,
            new CreateIterationPhaseRequest
            {
                Name = "Execution phase",
                Summary = "Bounded execution tests"
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var createCalls = 0;
        _todoService.CreateAsync(Arg.Any<McpServer.Support.Mcp.Services.TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                createCalls++;
                var request = call.Arg<McpServer.Support.Mcp.Services.TodoCreateRequest>()!;
                return createCalls == 1
                    ? new McpServer.Support.Mcp.Services.TodoMutationResult(
                        true,
                        null,
                        new McpServer.Support.Mcp.Services.TodoFlatItem
                        {
                            Id = request.Id,
                            Title = request.Title,
                            Section = request.Section,
                            Priority = request.Priority,
                            Done = false,
                        })
                    : new McpServer.Support.Mcp.Services.TodoMutationResult(
                        false,
                        "second create failed",
                        FailureKind: McpServer.Support.Mcp.Services.TodoMutationFailureKind.Conflict);
            });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.CreateTodosFromPlanAsync(
                    _workspacePath,
                    new CreateTodosFromPlanRequest
                    {
                        PhaseId = phase.PhaseId,
                        PlanId = "PLAN-001",
                        Todos =
                        [
                            new PlanTodoInput
                            {
                                Title = "Execution todo one",
                                Goal = "Support Byrd execution.",
                                Summary = "Create first canonical execution TODO."
                            },
                            new PlanTodoInput
                            {
                                Title = "Execution todo two",
                                Goal = "Support Byrd execution.",
                                Summary = "Create second canonical execution TODO."
                            }
                        ]
                    }, cancellationToken: TestContext.Current.CancellationToken))
            .ConfigureAwait(true);
        var executionTodo = await _sut.GetTodoAsync(_workspacePath, "EXEC-TODO-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Contains("second create failed", ex.Message, StringComparison.Ordinal);
        Assert.Null(executionTodo);
        await ((ITodoCompensationService)_todoService)
            .Received(1)
            .DeleteCreatedAsync("EXEC-TODO-001", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-161: Verifies projection failures that return a created legacy item are compensated
    /// because the authoritative TODO row may already exist even though the mutation result failed.
    /// </summary>
    [Fact]
    public async Task CreateTodosFromPlanAsync_WhenLegacyCreateProjectionFailsAfterDatabaseCommit_DeletesReturnedCreatedTodo()
    {
        var phase = await _sut.CreateIterationPhaseAsync(
            _workspacePath,
            new CreateIterationPhaseRequest
            {
                Name = "Execution phase",
                Summary = "Bounded execution tests"
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var createCalls = 0;
        _todoService.CreateAsync(Arg.Any<McpServer.Support.Mcp.Services.TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                createCalls++;
                var request = call.Arg<McpServer.Support.Mcp.Services.TodoCreateRequest>()!;
                var item = new McpServer.Support.Mcp.Services.TodoFlatItem
                {
                    Id = request.Id,
                    Title = request.Title,
                    Section = request.Section,
                    Priority = request.Priority,
                    Done = false,
                };
                return createCalls == 1
                    ? new McpServer.Support.Mcp.Services.TodoMutationResult(true, null, item)
                    : new McpServer.Support.Mcp.Services.TodoMutationResult(
                        false,
                        "projection failed",
                        item,
                        McpServer.Support.Mcp.Services.TodoMutationFailureKind.ProjectionFailed);
            });

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sut.CreateTodosFromPlanAsync(
                    _workspacePath,
                    new CreateTodosFromPlanRequest
                    {
                        PhaseId = phase.PhaseId,
                        PlanId = "PLAN-001",
                        Todos =
                        [
                            new PlanTodoInput
                            {
                                Title = "Execution todo one",
                                Goal = "Support Byrd execution.",
                                Summary = "Create first canonical execution TODO."
                            },
                            new PlanTodoInput
                            {
                                Title = "Execution todo two",
                                Goal = "Support Byrd execution.",
                                Summary = "Create second canonical execution TODO."
                            }
                        ]
                    }, cancellationToken: TestContext.Current.CancellationToken))
            .ConfigureAwait(true);

        Assert.Contains("projection failed", ex.Message, StringComparison.Ordinal);
        await ((ITodoCompensationService)_todoService)
            .Received(1)
            .DeleteCreatedAsync("EXEC-TODO-001", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
        await ((ITodoCompensationService)_todoService)
            .Received(1)
            .DeleteCreatedAsync("EXEC-TODO-002", Arg.Any<CancellationToken>())
            .ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-BYRD-SVC-001: Verifies that storing a test plan marks the TODO as <see cref="TodoExecutionStatus.TestReady"/>
    /// when unit tests are defined, preserving the Byrd requirement that implementation begins only after tests exist.
    /// </summary>
    [Fact]
    public async Task SetTestPlanAsync_WhenUnitTestsDefined_MarksTodoTestReady()
    {
        var todoId = await CreateExecutionTodoAsync().ConfigureAwait(true);

        var result = await _sut.SetTestPlanAsync(
            _workspacePath,
            todoId,
            new SetTodoTestPlanRequest
            {
                UnitTestsDefined = true,
                TestFilePaths = ["tests/TodoExecutionServiceTests.cs"],
                TestCommands = ["dotnet test tests/McpServer.Support.Mcp.Tests"]
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(todoId, result.TodoId);
        Assert.Equal(TodoExecutionStatus.TestReady, result.Status);
    }

    /// <summary>
    /// TEST-MCP-BYRD-SVC-001: Verifies that TODOs cannot transition to <see cref="TodoExecutionStatus.Implementing"/>
    /// until unit tests are defined, enforcing the Byrd TDD-first progression rule.
    /// </summary>
    [Fact]
    public async Task UpdateStatusAsync_WhenTestsNotDefined_ThrowsForImplementing()
    {
        var todoId = await CreateExecutionTodoAsync().ConfigureAwait(true);

        await Assert.ThrowsAsync<InvalidOperationException>(() => _sut.UpdateStatusAsync(
            _workspacePath,
            todoId,
            new UpdateTodoStatusRequest
            {
                TargetStatus = TodoExecutionStatus.Implementing
            }, cancellationToken: TestContext.Current.CancellationToken)).ConfigureAwait(true);
    }

    /// <summary>
    /// TEST-MCP-BYRD-SVC-001: Verifies that bounded execution context hydration returns concise
    /// requirement snippets, recent turn summaries, and linked modified files without depending on broad session history.
    /// </summary>
    [Fact]
    public async Task GetExecutionContextAsync_ReturnsRequirementAndTurnEvidence()
    {
        var todoId = await CreateExecutionTodoAsync(
            requirementIds: ["FR-BYRD-001"],
            relevantFiles: ["src/McpServer.Services/Services/TodoExecutionService.cs"]).ConfigureAwait(true);
        await SeedSessionTurnAsync("req-001", "Earlier design decision: TODOs are the bounded execution unit.", "src/McpServer.Support.Mcp/Controllers/TodoExecutionController.cs").ConfigureAwait(true);
        _requirementsDocumentService.GetFrAsync("FR-BYRD-001", Arg.Any<CancellationToken>())
            .Returns(new FrEntry("FR-BYRD-001", "Bounded execution", "Planning remains bounded to the active TODO."));

        await _sut.LinkTodoToSessionTurnsAsync(
            _workspacePath,
            todoId,
            new LinkTodoToSessionTurnsRequest
            {
                SessionTurnIds = ["req-001"]
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var context = await _sut.GetExecutionContextAsync(_workspacePath, todoId, 1, 1, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(context);
        Assert.Equal(todoId, context!.TodoId);
        Assert.Single(context.RecentRequirementSnippets);
        Assert.Contains("FR-BYRD-001", context.RecentRequirementSnippets[0], StringComparison.Ordinal);
        Assert.Single(context.RecentTurnSummaries);
        Assert.Contains("bounded execution unit", context.RecentTurnSummaries[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("src/McpServer.Support.Mcp/Controllers/TodoExecutionController.cs", context.RelevantFiles);
    }

    /// <summary>
    /// TEST-MCP-BYRD-SVC-001: Verifies that delta hydration returns only the evidence recorded after a
    /// checkpoint baseline, allowing the agent to resume from a compact change set instead of rereading history.
    /// </summary>
    [Fact]
    public async Task GetDeltaContextAsync_WhenCheckpointProvided_ReturnsOnlyNewChanges()
    {
        var todoId = await CreateExecutionTodoAsync().ConfigureAwait(true);

        var first = await _sut.AppendCheckpointAsync(
            _workspacePath,
            todoId,
            new AppendTodoCheckpointRequest
            {
                Kind = TodoCheckpointKind.TestDefined,
                Summary = "Defined the unit tests.",
                SessionTurnIds = ["req-001"],
                ArtifactIds = ["artifacts/test-plan.md"],
                CommitShas = ["1111111"]
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        await _sut.AppendCheckpointAsync(
            _workspacePath,
            todoId,
            new AppendTodoCheckpointRequest
            {
                Kind = TodoCheckpointKind.ImplementationProgress,
                Summary = "Implemented the first slice.",
                SessionTurnIds = ["req-002"],
                ArtifactIds = ["artifacts/diff.patch"],
                CommitShas = ["2222222"],
                NextAction = "Validate the updated execution rules."
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await SeedSessionTurnAsync("req-002", "Implemented the first execution slice.", "src/McpServer.Services/Services/TodoExecutionService.cs").ConfigureAwait(true);

        var delta = await _sut.GetDeltaContextAsync(_workspacePath, todoId, first.CheckpointId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(delta);
        Assert.Equal(todoId, delta!.TodoId);
        Assert.Single(delta.NewTurnIds);
        Assert.Equal("req-002", delta.NewTurnIds[0]);
        Assert.Single(delta.NewArtifactIds);
        Assert.Equal("artifacts/diff.patch", delta.NewArtifactIds[0]);
        Assert.Single(delta.NewCommitShas);
        Assert.Equal("2222222", delta.NewCommitShas[0]);
        Assert.Equal("Validate the updated execution rules.", delta.UpdatedNextAction);
    }

    /// <summary>
    /// TEST-MCP-BYRD-SVC-001: Verifies that a TODO can reach <see cref="TodoExecutionStatus.Complete"/>
    /// only after tests are defined, implementation evidence exists, validation passes, and the service marks
    /// acceptance criteria satisfied from the successful validation result.
    /// </summary>
    [Fact]
    public async Task UpdateStatusAsync_WhenValidationPasses_AllowsCompletion()
    {
        var todoId = await CreateExecutionTodoAsync().ConfigureAwait(true);
        await _sut.SetTestPlanAsync(
            _workspacePath,
            todoId,
            new SetTodoTestPlanRequest
            {
                UnitTestsDefined = true,
                UnitTestsPassing = true,
                TestFilePaths = ["tests/McpServer.Support.Mcp.Tests/Services/TodoExecutionServiceTests.cs"],
                TestCommands = ["dotnet test tests/McpServer.Support.Mcp.Tests --filter TodoExecutionServiceTests"]
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.UpdateStatusAsync(
            _workspacePath,
            todoId,
            new UpdateTodoStatusRequest
            {
                TargetStatus = TodoExecutionStatus.Implementing,
                Reason = "Unit tests are defined and reviewed."
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.AppendCheckpointAsync(
            _workspacePath,
            todoId,
            new AppendTodoCheckpointRequest
            {
                Kind = TodoCheckpointKind.ImplementationProgress,
                Summary = "Implemented the bounded execution flow."
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.UpdateStatusAsync(
            _workspacePath,
            todoId,
            new UpdateTodoStatusRequest
            {
                TargetStatus = TodoExecutionStatus.Validating,
                Reason = "Implementation evidence is available."
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        await _sut.RecordValidationResultAsync(
            _workspacePath,
            todoId,
            new RecordTodoValidationResultRequest
            {
                Result = "pass",
                Summary = "Unit tests pass and acceptance criteria are satisfied.",
                ArtifactIds = ["artifacts/validation.json"],
                UnitTestsPassing = true
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var result = await _sut.UpdateStatusAsync(
            _workspacePath,
            todoId,
            new UpdateTodoStatusRequest
            {
                TargetStatus = TodoExecutionStatus.Complete,
                Reason = "Validation passed."
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(TodoExecutionStatus.Complete, result.CurrentStatus);
    }

    /// <summary>
    /// TEST-MCP-BYRD-SVC-001: Verifies that a screenshot ADB step resolves the device, captures focus,
    /// and returns a relative artifact path without exposing arbitrary shell passthrough.
    /// </summary>
    [Fact]
    public async Task AdbStepAsync_WhenScreenshotRequested_ReturnsStructuredArtifact()
    {
        _processRunner.RunAsync(Arg.Any<ProcessRunRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<ProcessRunRequest>()!;
                return request.Arguments switch
                {
                    "devices" => new ProcessRunResult(0, "List of devices attached\nemulator-5554\tdevice\n", null),
                    "-s emulator-5554 shell dumpsys window windows" => new ProcessRunResult(0, "mCurrentFocus=Window{42 u0 com.example/.MainActivity}", null),
                    "-s emulator-5554 shell screencap -p /sdcard/mcpserver-screen.png" => new ProcessRunResult(0, string.Empty, null),
                    var args when args.StartsWith("-s emulator-5554 pull ", StringComparison.Ordinal) => new ProcessRunResult(0, "1 file pulled", null),
                    "-s emulator-5554 shell rm /sdcard/mcpserver-screen.png" => new ProcessRunResult(0, string.Empty, null),
                    _ => new ProcessRunResult(0, string.Empty, null)
                };
            });

        var result = await _sut.AdbStepAsync(
            _workspacePath,
            new AdbStepRequest
            {
                Action = AdbStepAction.Screenshot,
                CaptureScreenshot = true,
                Instruction = "Capture the current UI."
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(result.Success);
        Assert.Equal(AdbStepAction.Screenshot, result.Action);
        Assert.Equal("emulator-5554", result.DeviceSerial);
        Assert.Equal("com.example/.MainActivity", result.CurrentFocus);
        Assert.NotNull(result.ScreenshotPath);
        Assert.Contains(".mcpServer/artifacts/device", result.ScreenshotPath!, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<string> CreateExecutionTodoAsync(
        IReadOnlyList<string>? requirementIds = null,
        IReadOnlyList<string>? relevantFiles = null)
    {
        var phase = await _sut.CreateIterationPhaseAsync(
            _workspacePath,
            new CreateIterationPhaseRequest
            {
                Name = "Execution phase",
                Summary = "Bounded execution tests"
            }).ConfigureAwait(true);

        var result = await _sut.CreateTodosFromPlanAsync(
            _workspacePath,
            new CreateTodosFromPlanRequest
            {
                PhaseId = phase.PhaseId,
                PlanId = "PLAN-001",
                Todos =
                [
                    new PlanTodoInput
                    {
                        Title = "Execution todo",
                        Goal = "Support Byrd execution.",
                        Summary = "Bound the working set to the active TODO.",
                        AcceptanceCriteria = ["Hydrates requirement snippets", "Stores validation state"],
                        Constraints = ["Do not depend on chat history"],
                        RequirementIds = requirementIds,
                        RelevantFiles = relevantFiles,
                    }
                ]
            }).ConfigureAwait(true);

        return result.TodoIds[0];
    }

    private async Task SeedSessionTurnAsync(string requestId, string queryTitle, string fileModified)
    {
        var session = new SessionLogEntity
        {
            SourceType = "Codex",
            SessionId = $"Codex-{Guid.NewGuid():N}",
            Model = "gpt-5",
            Started = DateTimeOffset.UtcNow,
            LastUpdated = DateTimeOffset.UtcNow,
        };
        _db.SessionLogs.Add(session);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        var turn = new SessionLogTurnEntity
        {
            SessionLogId = session.Id,
            RequestId = requestId,
            Timestamp = DateTimeOffset.UtcNow,
            QueryTitle = queryTitle,
            Response = queryTitle,
        };
        _db.SessionLogTurns.Add(turn);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        _db.SessionLogTurnStringLists.Add(new SessionLogTurnStringListEntity
        {
            SessionLogTurnId = turn.Id,
            ListType = "filesModified",
            Ordinal = 0,
            Value = fileModified,
        });
        await _db.SaveChangesAsync().ConfigureAwait(true);
    }
}
