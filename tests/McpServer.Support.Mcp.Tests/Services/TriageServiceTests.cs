using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-TRIAGE-001, TEST-MCP-TRIAGE-002, TEST-MCP-TRIAGE-004,
/// TEST-MCP-TRIAGE-005, TEST-MCP-TRIAGE-006: acceptance tests for durable triage intake,
/// deterministic grouping, asynchronous TODO conversion, failure preservation, and workspace isolation.
/// </summary>
public sealed class TriageServiceTests : IDisposable
{
    private const string PrimaryWorkspace = "F:\\GitHub\\IncidentSource";
    private const string AlternateWorkspace = "F:\\GitHub\\OtherSource";
    private const string McpServerWorkspace = "F:\\GitHub\\McpServer";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _dbOptions;
    private readonly ManualTimeProvider _time = new(new DateTimeOffset(2026, 6, 25, 5, 0, 0, TimeSpan.Zero));

    /// <summary>Creates an isolated relational database used by each triage test.</summary>
    public TriageServiceTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var db = CreateDb(PrimaryWorkspace);
        db.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    /// <summary>
    /// TEST-MCP-TRIAGE-001: valid intake persists a report, returns accepted queue state,
    /// and does not invoke research or TODO creation synchronously.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_ValidReport_PersistsAcceptedStateWithoutRunningResearch()
    {
        var runner = Substitute.For<ITriageResearchRunner>();
        var todo = Substitute.For<ITodoService>();
        var sut = CreateService(PrimaryWorkspace, runner: runner, todo: todo);

        var result = await sut.SubmitReportAsync(new TriageReportRequest
        {
            Title = "REPL wrapper drops validation errors",
            Summary = "The wrapper returns ok true while the server rejects the YAML envelope.",
            Component = "repl",
            AffectedPaths = ["src/McpServer.Repl.Core/ReplYamlMessageValidator.cs"],
            ReporterAgent = "Codex",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.True(result.Success, result.Error);
        Assert.StartsWith("triage-report-", result.ReportId, StringComparison.Ordinal);
        Assert.StartsWith("triage-group-", result.GroupId, StringComparison.Ordinal);
        Assert.Equal("collecting", result.Status);
        Assert.Equal(_time.GetUtcNow().AddMinutes(15), result.QuietDeadlineUtc);

        using var db = CreateDb(PrimaryWorkspace);
        Assert.Equal(1, await db.TriageReports.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, await db.TriageGroups.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
        await runner.DidNotReceiveWithAnyArgs().RunAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-002: matching reports use deterministic grouping and each new report
    /// resets the fifteen minute quiet deadline.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_MatchingDedupeKey_ReusesGroupAndResetsQuietDeadline()
    {
        var sut = CreateService(PrimaryWorkspace);

        var first = await sut.SubmitReportAsync(CreateReport("same-wrapper-bug"), cancellationToken: TestContext.Current.CancellationToken);
        _time.Advance(TimeSpan.FromMinutes(5));
        var second = await sut.SubmitReportAsync(CreateReport("same-wrapper-bug"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(first.GroupId, second.GroupId);
        Assert.Equal(_time.GetUtcNow().AddMinutes(15), second.QuietDeadlineUtc);

        using var db = CreateDb(PrimaryWorkspace);
        var group = await db.TriageGroups.SingleAsync(g => g.GroupId == first.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(2, group.ReportCount);
        Assert.Equal(second.QuietDeadlineUtc, group.QuietDeadlineUtc);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-001, TEST-MCP-TRIAGE-002: idempotency keys return the original accepted
    /// report without inserting a duplicate report or extending the group's quiet window.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_SameIdempotencyKey_ReturnsOriginalReportWithoutDuplicate()
    {
        var sut = CreateService(PrimaryWorkspace);
        var request = CreateReport("idempotent-wrapper-bug") with { IdempotencyKey = "triage-idempotency-001" };

        var first = await sut.SubmitReportAsync(request, cancellationToken: TestContext.Current.CancellationToken);
        _time.Advance(TimeSpan.FromMinutes(5));
        var second = await sut.SubmitReportAsync(request, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(first.ReportId, second.ReportId);
        Assert.Equal(first.GroupId, second.GroupId);
        Assert.Equal(first.QuietDeadlineUtc, second.QuietDeadlineUtc);

        using var db = CreateDb(PrimaryWorkspace);
        Assert.Equal(1, await db.TriageReports.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
        var group = await db.TriageGroups.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(1, group.ReportCount);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-007: DeleteGroupAsync soft-deletes the group and its reports so they no
    /// longer appear in queries, while the underlying rows remain in storage marked deleted.
    /// </summary>
    [Fact]
    public async Task DeleteGroupAsync_ExistingGroup_SoftDeletesGroupAndReports()
    {
        var sut = CreateService(PrimaryWorkspace);
        var submit = await sut.SubmitReportAsync(CreateReport("delete-me"), cancellationToken: TestContext.Current.CancellationToken);

        var result = await sut.DeleteGroupAsync(submit.GroupId, reason: "fixed upstream", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(submit.GroupId, result.GroupId);
        Assert.Equal(1, result.DeletedReportCount);

        var groups = await sut.QueryGroupsAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Empty(groups.Items);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.GetGroupAsync(submit.GroupId, TestContext.Current.CancellationToken));

        using var db = CreateDb(PrimaryWorkspace);
        Assert.Equal(0, await db.TriageGroups.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, await db.TriageGroups.IgnoreQueryFilters().CountAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(0, await db.TriageReports.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, await db.TriageReports.IgnoreQueryFilters().CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>TEST-MCP-TRIAGE-007: deleting a missing triage group throws KeyNotFoundException.</summary>
    [Fact]
    public async Task DeleteGroupAsync_MissingGroup_ThrowsKeyNotFound()
    {
        var sut = CreateService(PrimaryWorkspace);
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            sut.DeleteGroupAsync("triage-group-does-not-exist", reason: null, cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-006: the same grouping signature in two workspaces creates isolated
    /// groups and does not leak status across workspace filters.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_SameSignatureAcrossWorkspaces_CreatesIsolatedGroups()
    {
        var first = await CreateService(PrimaryWorkspace).SubmitReportAsync(CreateReport("shared-bug"), cancellationToken: TestContext.Current.CancellationToken);
        var second = await CreateService(AlternateWorkspace).SubmitReportAsync(CreateReport("shared-bug"), cancellationToken: TestContext.Current.CancellationToken);

        Assert.NotEqual(first.GroupId, second.GroupId);

        using var primaryDb = CreateDb(PrimaryWorkspace);
        using var alternateDb = CreateDb(AlternateWorkspace);
        Assert.Equal(1, await primaryDb.TriageGroups.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal(1, await alternateDb.TriageGroups.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-006: MCP Server core and plugin bugs are moved into the registered
    /// McpServer workspace so triage is grouped with the owning product.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_McpServerPluginBug_RoutesToRegisteredMcpServerWorkspace()
    {
        var sut = CreateService(
            PrimaryWorkspace,
            workspaces:
            [
                Workspace(PrimaryWorkspace, "IncidentSource"),
                Workspace(McpServerWorkspace, "McpServer"),
            ]);

        var result = await sut.SubmitReportAsync(new TriageReportRequest
        {
            Title = "mcpserver-codex-plugin masks method_not_found",
            Summary = "The MCP Server Codex plugin reports success after a workflow.triage call fails.",
            Component = "mcpserver-codex-plugin",
            AffectedPaths = ["F:\\GitHub\\mcpserver-codex-plugin\\lib\\repl-invoke.sh"],
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(McpServerWorkspace, result.WorkspacePath);
        using var db = CreateDb(McpServerWorkspace);
        var report = await db.TriageReports.SingleAsync(cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal(McpServerWorkspace, report.WorkspaceId);
        Assert.Equal(PrimaryWorkspace, report.OriginalWorkspacePath);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-006: MCP Server-related bugs stay in the submitting workspace when
    /// no registered workspace named McpServer exists.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_McpServerPluginBugWithoutMcpServerWorkspace_StaysInSubmittingWorkspace()
    {
        var sut = CreateService(
            PrimaryWorkspace,
            workspaces: [Workspace(PrimaryWorkspace, "IncidentSource")]);

        var result = await sut.SubmitReportAsync(new TriageReportRequest
        {
            Title = "mcpserver-grok-plugin rejects triage_status",
            Summary = "The MCP Server Grok plugin does not expose triage status.",
            Component = "mcpserver-grok-plugin",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal(PrimaryWorkspace, result.WorkspacePath);
        using var db = CreateDb(PrimaryWorkspace);
        Assert.Equal(1, await db.TriageReports.CountAsync(cancellationToken: TestContext.Current.CancellationToken));
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-004: schema-valid research output creates exactly one
    /// BUG-TRIAGE-### TODO and marks the group completed.
    /// </summary>
    [Fact]
    public async Task ProcessDueGroupsAsync_ValidResearchOutput_CreatesExactlyOneBugTriageTodo()
    {
        var runner = Substitute.For<ITriageResearchRunner>();
        TriageResearchRequest? researchRequest = null;
        runner.RunAsync(Arg.Do<TriageResearchRequest>(request => researchRequest = request), Arg.Any<CancellationToken>())
            .Returns(new TriageResearchRunResult(
                true,
                """
                {"title":"Fix triage wrapper failure","summary":"The wrapper hides failures.","severity":"high","acceptanceCriteria":["Wrapper returns error envelopes"],"implementationNotes":["Route workflow.triage through typed wrappers."]}
                """,
                null));

        var todo = Substitute.For<ITodoService>();
        todo.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([], 0));
        todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new TodoMutationResult(
                true,
                Item: new TodoFlatItem
                {
                    Id = ((TodoCreateRequest)call[0]!).Id,
                    Title = ((TodoCreateRequest)call[0]!).Title,
                    Section = ((TodoCreateRequest)call[0]!).Section,
                    Priority = ((TodoCreateRequest)call[0]!).Priority,
                    Done = false,
                }));

        var sut = CreateService(PrimaryWorkspace, runner: runner, todo: todo, quietPeriod: TimeSpan.Zero);
        var submit = await sut.SubmitReportAsync(CreateReport("research-valid"), cancellationToken: TestContext.Current.CancellationToken);
        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        Assert.NotNull(researchRequest);
        Assert.Contains("rendered triage prompt", researchRequest.Prompt, StringComparison.Ordinal);
        Assert.Contains("Runtime shell note", researchRequest.Prompt, StringComparison.Ordinal);
        Assert.Contains("pwsh.exe", researchRequest.Prompt, StringComparison.Ordinal);
        Assert.Contains("Do not hard-code", researchRequest.Prompt, StringComparison.Ordinal);
        Assert.Contains(submit.GroupId, researchRequest.GroupJson, StringComparison.Ordinal);
        Assert.Equal(PrimaryWorkspace, researchRequest.WorkspacePath);
        await todo.Received(1).CreateAsync(
            Arg.Is<TodoCreateRequest>(request =>
                request != null &&
                request.Id == "BUG-TRIAGE-001" &&
                request.Title == "Fix triage wrapper failure" &&
                request.FunctionalRequirements != null &&
                request.FunctionalRequirements.Contains("FR-MCP-TRIAGE-002")),
            Arg.Any<CancellationToken>());

        var group = await sut.GetGroupAsync(submit.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("completed", group.Status);
        Assert.Equal("BUG-TRIAGE-001", group.CreatedTodoId);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-004: agent output with incidental preamble text still yields the
    /// schema-valid JSON object used to create the BUG-TRIAGE TODO.
    /// </summary>
    [Fact]
    public async Task ProcessDueGroupsAsync_ResearchOutputWithPreamble_ExtractsJsonAndCreatesTodo()
    {
        var runner = Substitute.For<ITriageResearchRunner>();
        runner.RunAsync(Arg.Any<TriageResearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageResearchRunResult(
                true,
                """
                Investigating how the triage group should be classified.
                {"title":"Fix triage JSON extraction","summary":"Triage agents may write progress before the final JSON.","severity":"high","acceptanceCriteria":["Preamble text before JSON does not fail schema validation"],"implementationNotes":["Extract the first balanced JSON object from successful agent output."]}
                """,
                null));

        var todo = Substitute.For<ITodoService>();
        todo.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([], 0));
        todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new TodoMutationResult(
                true,
                Item: new TodoFlatItem
                {
                    Id = ((TodoCreateRequest)call[0]!).Id,
                    Title = ((TodoCreateRequest)call[0]!).Title,
                    Section = ((TodoCreateRequest)call[0]!).Section,
                    Priority = ((TodoCreateRequest)call[0]!).Priority,
                    Done = false,
                }));

        var sut = CreateService(PrimaryWorkspace, runner: runner, todo: todo, quietPeriod: TimeSpan.Zero);
        var submit = await sut.SubmitReportAsync(CreateReport("research-preamble"), cancellationToken: TestContext.Current.CancellationToken);

        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        await todo.Received(1).CreateAsync(
            Arg.Is<TodoCreateRequest>(request =>
                request != null &&
                request.Id == "BUG-TRIAGE-001" &&
                request.Title == "Fix triage JSON extraction"),
            Arg.Any<CancellationToken>());

        var group = await sut.GetGroupAsync(submit.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("completed", group.Status);
        Assert.Equal("BUG-TRIAGE-001", group.CreatedTodoId);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-004: BUG-TRIAGE id allocation considers orphaned TODO rows
    /// that were created before a triage group recorded CreatedTodoId.
    /// </summary>
    [Fact]
    public async Task ProcessDueGroupsAsync_ExistingUnownedBugTriageTodo_UsesNextId()
    {
        using (var db = CreateDb(PrimaryWorkspace))
        {
            db.TodoItems.Add(SeedTodoItem("BUG-TRIAGE-052", "Orphaned triage TODO"));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        var runner = Substitute.For<ITriageResearchRunner>();
        runner.RunAsync(Arg.Any<TriageResearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageResearchRunResult(
                true,
                """
                {"title":"Fix orphaned triage id allocation","summary":"Triage must skip BUG-TRIAGE ids already present in TodoItems.","severity":"high","acceptanceCriteria":["Existing orphaned BUG-TRIAGE TODO ids are skipped"],"implementationNotes":["Scan TodoItems directly before allocating the next id."]}
                """,
                null));

        TodoCreateRequest? createdRequest = null;
        var todo = Substitute.For<ITodoService>();
        todo.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([], 0));
        todo.CreateAsync(Arg.Do<TodoCreateRequest>(request => createdRequest = request), Arg.Any<CancellationToken>())
            .Returns(call => new TodoMutationResult(
                true,
                Item: new TodoFlatItem
                {
                    Id = ((TodoCreateRequest)call[0]!).Id,
                    Title = ((TodoCreateRequest)call[0]!).Title,
                    Section = ((TodoCreateRequest)call[0]!).Section,
                    Priority = ((TodoCreateRequest)call[0]!).Priority,
                    Done = false,
                }));

        var sut = CreateService(PrimaryWorkspace, runner: runner, todo: todo, quietPeriod: TimeSpan.Zero);
        var submit = await sut.SubmitReportAsync(CreateReport("research-orphaned-todo-id"), cancellationToken: TestContext.Current.CancellationToken);

        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        Assert.NotNull(createdRequest);
        Assert.Equal("BUG-TRIAGE-053", createdRequest.Id);

        var group = await sut.GetGroupAsync(submit.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("completed", group.Status);
        Assert.Equal("BUG-TRIAGE-053", group.CreatedTodoId);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-004: TODO id collision during triage creation retries with the next
    /// BUG-TRIAGE id instead of wedging the group in failed state.
    /// </summary>
    [Fact]
    public async Task ProcessDueGroupsAsync_WhenGeneratedTodoIdAlreadyExists_RetriesWithNextBugTriageId()
    {
        var runner = Substitute.For<ITriageResearchRunner>();
        runner.RunAsync(Arg.Any<TriageResearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageResearchRunResult(
                true,
                """
                {"title":"Fix duplicate triage id","summary":"The first generated BUG-TRIAGE id already exists.","severity":"medium","acceptanceCriteria":["Retry uses the next id"],"implementationNotes":[]}
                """,
                null));

        var requestedIds = new List<string>();
        var todo = Substitute.For<ITodoService>();
        todo.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([], 0));
        todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = (TodoCreateRequest)call[0]!;
                requestedIds.Add(request.Id);
                if (request.Id == "BUG-TRIAGE-001")
                {
                    return new TodoMutationResult(
                        false,
                        "Item with id 'BUG-TRIAGE-001' already exists.",
                        null,
                        TodoMutationFailureKind.Conflict);
                }

                return new TodoMutationResult(
                    true,
                    Item: new TodoFlatItem
                    {
                        Id = request.Id,
                        Title = request.Title,
                        Section = request.Section,
                        Priority = request.Priority,
                        Done = false,
                    });
            });

        var sut = CreateService(PrimaryWorkspace, runner: runner, todo: todo, quietPeriod: TimeSpan.Zero);
        var submit = await sut.SubmitReportAsync(CreateReport("duplicate-triage-id"), cancellationToken: TestContext.Current.CancellationToken);

        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        Assert.Equal(["BUG-TRIAGE-001", "BUG-TRIAGE-002"], requestedIds);

        var group = await sut.GetGroupAsync(submit.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("completed", group.Status);
        Assert.Equal("BUG-TRIAGE-002", group.CreatedTodoId);
        Assert.Null(group.LastError);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-004: successful research output with no JSON creates a fallback
    /// TODO from the grouped report data and records diagnostics instead of wedging triage.
    /// </summary>
    [Fact]
    public async Task ProcessDueGroupsAsync_ResearchOutputWithoutJson_CreatesFallbackTodoWithDiagnostics()
    {
        var runner = Substitute.For<ITriageResearchRunner>();
        runner.RunAsync(Arg.Any<TriageResearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageResearchRunResult(
                true,
                """
                I'll inspect the PowerShell.MCP text-edit helper and return triage JSON only.
                Looking for the text-edit helper and any atomic replace/move logic tied to TEMP.
                """,
                null));

        TodoCreateRequest? createdRequest = null;
        var todo = Substitute.For<ITodoService>();
        todo.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([], 0));
        todo.CreateAsync(Arg.Do<TodoCreateRequest>(request => createdRequest = request), Arg.Any<CancellationToken>())
            .Returns(call => new TodoMutationResult(
                true,
                Item: new TodoFlatItem
                {
                    Id = ((TodoCreateRequest)call[0]!).Id,
                    Title = ((TodoCreateRequest)call[0]!).Title,
                    Section = ((TodoCreateRequest)call[0]!).Section,
                    Priority = ((TodoCreateRequest)call[0]!).Priority,
                    Done = false,
                }));

        var sut = CreateService(PrimaryWorkspace, runner: runner, todo: todo, quietPeriod: TimeSpan.Zero);
        var submit = await sut.SubmitReportAsync(CreateReport("research-no-json"), cancellationToken: TestContext.Current.CancellationToken);

        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        Assert.NotNull(createdRequest);
        Assert.Equal("BUG-TRIAGE-001", createdRequest.Id);
        Assert.Equal("REPL triage wrapper failure", createdRequest.Title);
        Assert.Equal("medium", createdRequest.Priority);
        var technicalDetails = createdRequest.TechnicalDetails ?? [];
        Assert.Contains(technicalDetails, detail => detail.Contains("Fallback TODO created", StringComparison.Ordinal));
        Assert.Contains(technicalDetails, detail => detail.Contains("Raw triage research output is preserved", StringComparison.Ordinal));

        var group = await sut.GetGroupAsync(submit.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("completed", group.Status);
        Assert.Equal("BUG-TRIAGE-001", group.CreatedTodoId);

        var run = Assert.Single((await sut.QueryRunsAsync(
            status: "completed",
            groupId: submit.GroupId,
            workspacePath: PrimaryWorkspace,
            cancellationToken: TestContext.Current.CancellationToken)).Items);
        Assert.Equal("BUG-TRIAGE-001", run.CreatedTodoId);
        Assert.Contains("Fallback TODO created", run.ResponseJson, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-004: triage-created TODOs are routed through the host-selected
    /// triage TODO creator instead of directly mutating the read/query TODO service.
    /// </summary>
    [Fact]
    public async Task ProcessDueGroupsAsync_ValidResearchOutput_UsesTriageTodoCreatorForMutation()
    {
        var runner = Substitute.For<ITriageResearchRunner>();
        runner.RunAsync(Arg.Any<TriageResearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageResearchRunResult(
                true,
                """
                {"title":"Fix triage TODO routing","summary":"Triage TODO creation must use the transaction gate.","severity":"high","acceptanceCriteria":["Triage uses the host-selected TODO creator"],"implementationNotes":["Route creation through ITriageTodoCreator."]}
                """,
                null));

        var todo = Substitute.For<ITodoService>();
        todo.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([], 0));
        var todoCreator = Substitute.For<ITriageTodoCreator>();
        todoCreator.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new TodoMutationResult(
                true,
                Item: new TodoFlatItem
                {
                    Id = ((TodoCreateRequest)call[0]!).Id,
                    Title = ((TodoCreateRequest)call[0]!).Title,
                    Section = ((TodoCreateRequest)call[0]!).Section,
                    Priority = ((TodoCreateRequest)call[0]!).Priority,
                    Done = false,
                }));

        var sut = CreateService(
            PrimaryWorkspace,
            runner: runner,
            todo: todo,
            todoCreator: todoCreator,
            quietPeriod: TimeSpan.Zero);
        var submit = await sut.SubmitReportAsync(CreateReport("creator-route"), cancellationToken: TestContext.Current.CancellationToken);

        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        await todoCreator.Received(1).CreateAsync(
            Arg.Is<TodoCreateRequest>(request =>
                request != null &&
                request.Id == "BUG-TRIAGE-001" &&
                request.Title == "Fix triage TODO routing"),
            Arg.Any<CancellationToken>());
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
        var group = await sut.GetGroupAsync(submit.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("completed", group.Status);
        Assert.Equal("BUG-TRIAGE-001", group.CreatedTodoId);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: when TODO creation returns an item with a projection failure but
    /// the created item cannot be read back, triage records failure instead of a phantom TODO id.
    /// </summary>
    [Fact]
    public async Task ProcessDueGroupsAsync_ProjectionFailureWithUnreadableCreatedItem_FailsWithoutCreatedTodoId()
    {
        var runner = Substitute.For<ITriageResearchRunner>();
        runner.RunAsync(Arg.Any<TriageResearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageResearchRunResult(
                true,
                """
                {"title":"Fix triage created TODO visibility","summary":"Created TODO ids disappear when projection fails.","severity":"high","acceptanceCriteria":["Created TODO id remains visible"],"implementationNotes":["Record item id returned by TODO service."]}
                """,
                null));

        var todo = Substitute.For<ITodoService>();
        todo.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([], 0));
        todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new TodoMutationResult(
                false,
                "projection failed",
                new TodoFlatItem
                {
                    Id = ((TodoCreateRequest)call[0]!).Id,
                    Title = ((TodoCreateRequest)call[0]!).Title,
                    Section = ((TodoCreateRequest)call[0]!).Section,
                    Priority = ((TodoCreateRequest)call[0]!).Priority,
                    Done = false,
                },
                TodoMutationFailureKind.ProjectionFailed));

        var sut = CreateService(PrimaryWorkspace, runner: runner, todo: todo, quietPeriod: TimeSpan.Zero);
        var submit = await sut.SubmitReportAsync(CreateReport("projection-failure-created"), cancellationToken: TestContext.Current.CancellationToken);
        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        var group = await sut.GetGroupAsync(submit.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("failed", group.Status);
        Assert.Null(group.CreatedTodoId);
        Assert.Equal("projection failed", group.LastError);

        var runs = await sut.QueryRunsAsync(groupId: submit.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        var run = Assert.Single(runs.Items);
        Assert.Equal("failed", run.Status);
        Assert.Null(run.CreatedTodoId);
        Assert.Equal("projection failed", run.Error);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-004: when TODO creation returns a projection warning and the
    /// created item is readable, triage records the TODO id and preserves the warning.
    /// </summary>
    [Fact]
    public async Task ProcessDueGroupsAsync_ProjectionFailureWithReadableCreatedItem_RecordsCreatedTodoWithWarning()
    {
        var runner = Substitute.For<ITriageResearchRunner>();
        runner.RunAsync(Arg.Any<TriageResearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageResearchRunResult(
                true,
                """
                {"title":"Fix triage created TODO visibility","summary":"Created TODO ids disappear when projection fails.","severity":"high","acceptanceCriteria":["Created TODO id remains visible"],"implementationNotes":["Record item id returned by TODO service."]}
                """,
                null));

        var created = new TodoFlatItem
        {
            Id = "BUG-TRIAGE-001",
            Title = "Fix triage created TODO visibility",
            Section = "Backlog",
            Priority = "high",
            Done = false,
        };
        var todo = Substitute.For<ITodoService>();
        todo.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([], 0));
        todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                using var db = CreateDb(PrimaryWorkspace);
                db.TodoItems.Add(SeedTodoItem("BUG-TRIAGE-001", "Fix triage created TODO visibility"));
                db.SaveChanges();
                return new TodoMutationResult(
                    false,
                    "projection failed",
                    created,
                    TodoMutationFailureKind.ProjectionFailed);
            });

        var sut = CreateService(PrimaryWorkspace, runner: runner, todo: todo, quietPeriod: TimeSpan.Zero);
        var submit = await sut.SubmitReportAsync(CreateReport("projection-failure-readable-created"), cancellationToken: TestContext.Current.CancellationToken);
        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        var group = await sut.GetGroupAsync(submit.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("completed", group.Status);
        Assert.Equal("BUG-TRIAGE-001", group.CreatedTodoId);
        Assert.Equal("projection failed", group.LastError);

        var runs = await sut.QueryRunsAsync(groupId: submit.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        var run = Assert.Single(runs.Items);
        Assert.Equal("completed", run.Status);
        Assert.Equal("BUG-TRIAGE-001", run.CreatedTodoId);
        Assert.Equal("projection failed", run.Error);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-004: background worker scopes do not have an ambient HTTP workspace,
    /// but due workspace triage groups must still run and create one backlog TODO.
    /// </summary>
    [Fact]
    public async Task ProcessDueGroupsAsync_BackgroundScopeWithoutWorkspace_ProcessesDueWorkspaceGroup()
    {
        var runner = Substitute.For<ITriageResearchRunner>();
        TriageResearchRequest? researchRequest = null;
        runner.RunAsync(Arg.Do<TriageResearchRequest>(request => researchRequest = request), Arg.Any<CancellationToken>())
            .Returns(new TriageResearchRunResult(
                true,
                """
                {"title":"Fix background triage","summary":"The worker must see workspace groups.","severity":"medium","acceptanceCriteria":["Background worker processes due workspace groups"],"implementationNotes":[]}
                """,
                null));

        var todo = Substitute.For<ITodoService>();
        todo.QueryAsync(Arg.Any<TodoQueryRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TodoQueryResult([], 0));
        todo.CreateAsync(Arg.Any<TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call => new TodoMutationResult(
                true,
                Item: new TodoFlatItem
                {
                    Id = ((TodoCreateRequest)call[0]!).Id,
                    Title = ((TodoCreateRequest)call[0]!).Title,
                    Section = ((TodoCreateRequest)call[0]!).Section,
                    Priority = ((TodoCreateRequest)call[0]!).Priority,
                    Done = false,
                }));

        var foreground = CreateService(PrimaryWorkspace, runner: runner, todo: todo, quietPeriod: TimeSpan.Zero);
        var submit = await foreground.SubmitReportAsync(CreateReport("background-scope"), cancellationToken: TestContext.Current.CancellationToken);
        var background = CreateService(string.Empty, runner: runner, todo: todo, quietPeriod: TimeSpan.Zero);

        var processed = await background.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        Assert.NotNull(researchRequest);
        Assert.Equal(PrimaryWorkspace, researchRequest.WorkspacePath);
        await todo.Received(1).CreateAsync(
            Arg.Is<TodoCreateRequest>(request =>
                request != null &&
                request.Id == "BUG-TRIAGE-001" &&
                request.Title == "Fix background triage"),
            Arg.Any<CancellationToken>());

        var verifier = CreateService(PrimaryWorkspace, runner: runner, todo: todo, quietPeriod: TimeSpan.Zero);
        var group = await verifier.GetGroupAsync(submit.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("completed", group.Status);
        Assert.Equal("BUG-TRIAGE-001", group.CreatedTodoId);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: invalid research output creates no TODO and leaves an inspectable
    /// failure state on the group and research run.
    /// </summary>
    [Fact]
    public async Task ProcessDueGroupsAsync_InvalidResearchOutput_CreatesNoTodoAndPreservesFailure()
    {
        var runner = Substitute.For<ITriageResearchRunner>();
        runner.RunAsync(Arg.Any<TriageResearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(new TriageResearchRunResult(true, "{}", null));

        var todo = Substitute.For<ITodoService>();
        var sut = CreateService(PrimaryWorkspace, runner: runner, todo: todo, quietPeriod: TimeSpan.Zero);
        var submit = await sut.SubmitReportAsync(CreateReport("research-invalid"), cancellationToken: TestContext.Current.CancellationToken);

        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
        var group = await sut.GetGroupAsync(submit.GroupId, cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("failed", group.Status);
        Assert.Contains("schema", group.LastError, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: streamed triage-agent stdout and stderr are appended to
    /// the durable research-run record before the final runner result is applied.
    /// </summary>
    [Fact]
    public async Task ProcessDueGroupsAsync_WhenRunnerStreamsOutput_AppendsOutputToResearchRun()
    {
        var runner = Substitute.For<ITriageResearchRunner>();
        runner.RunAsync(Arg.Any<TriageResearchRequest>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                var request = (TriageResearchRequest)call[0]!;
                Assert.NotNull(request.OutputReceivedAsync);
                await request.OutputReceivedAsync!(new TriageResearchOutputUpdate("stdout", "analysis started"));
                await request.OutputReceivedAsync!(new TriageResearchOutputUpdate("stderr", "loading context"));
                return new TriageResearchRunResult(false, null, "agent failed");
            });

        var todo = Substitute.For<ITodoService>();
        var sut = CreateService(PrimaryWorkspace, runner: runner, todo: todo, quietPeriod: TimeSpan.Zero);
        await sut.SubmitReportAsync(CreateReport("research-streamed-output"), cancellationToken: TestContext.Current.CancellationToken);

        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        var run = Assert.Single((await sut.QueryRunsAsync(workspacePath: PrimaryWorkspace, cancellationToken: TestContext.Current.CancellationToken)).Items);
        Assert.Equal("failed", run.Status);
        Assert.Contains("analysis started", run.AgentStdout, StringComparison.Ordinal);
        Assert.Contains("loading context", run.AgentStderr, StringComparison.Ordinal);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: stale processing runs are failed durably so the
    /// group can be inspected and resubmitted instead of remaining wedged.
    /// </summary>
    [Fact]
    public async Task ProcessDueGroupsAsync_WhenProcessingRunExceedsMaxRunTime_MarksRunAndGroupFailedForRetry()
    {
        var runner = Substitute.For<ITriageResearchRunner>();
        var now = _time.GetUtcNow();
        using (var db = CreateDb(PrimaryWorkspace))
        {
            db.TriageGroups.Add(SeedGroup("triage-group-stale", "processing", now.AddMinutes(-31)));
            db.TriageReports.Add(SeedReport("triage-report-stale", "triage-group-stale", "Stale processing report", now.AddMinutes(-31)));
            db.TriageResearchRuns.Add(SeedRun("triage-run-stale", "triage-group-stale", "processing", now.AddMinutes(-31)));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        var sut = CreateService(PrimaryWorkspace, runner: runner, maxRunTime: TimeSpan.FromMinutes(30));

        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(0, processed.ProcessedGroups);
        await runner.DidNotReceiveWithAnyArgs().RunAsync(default!, cancellationToken: TestContext.Current.CancellationToken);
        var group = await sut.GetGroupAsync("triage-group-stale", cancellationToken: TestContext.Current.CancellationToken);
        Assert.Equal("failed", group.Status);
        Assert.Contains("maximum duration", group.LastError, StringComparison.OrdinalIgnoreCase);
        var run = Assert.Single((await sut.QueryRunsAsync(
            status: "failed",
            groupId: "triage-group-stale",
            workspacePath: PrimaryWorkspace, cancellationToken: TestContext.Current.CancellationToken)).Items);
        Assert.Equal("triage-run-stale", run.RunId);
        Assert.Contains("maximum duration", run.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(now, run.CompletedUtc);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: retrying a stale processing group first closes the
    /// abandoned run, then makes the group immediately due for a fresh run.
    /// </summary>
    [Fact]
    public async Task RetryGroupAsync_WhenProcessingRunIsStale_FailsPreviousRunAndRequeuesGroup()
    {
        var now = _time.GetUtcNow();
        using (var db = CreateDb(PrimaryWorkspace))
        {
            db.TriageGroups.Add(SeedGroup("triage-group-stale-retry", "processing", now.AddMinutes(-31)));
            db.TriageReports.Add(SeedReport("triage-report-stale-retry", "triage-group-stale-retry", "Stale retry report", now.AddMinutes(-31)));
            db.TriageResearchRuns.Add(SeedRun("triage-run-stale-retry", "triage-group-stale-retry", "processing", now.AddMinutes(-31)));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        var sut = CreateService(PrimaryWorkspace, maxRunTime: TimeSpan.FromMinutes(30));

        var group = await sut.RetryGroupAsync("triage-group-stale-retry", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("collecting", group.Status);
        Assert.Null(group.LastError);
        Assert.Equal(now, group.QuietDeadlineUtc);
        var run = Assert.Single((await sut.QueryRunsAsync(
            status: "failed",
            groupId: "triage-group-stale-retry",
            workspacePath: PrimaryWorkspace, cancellationToken: TestContext.Current.CancellationToken)).Items);
        Assert.Equal("triage-run-stale-retry", run.RunId);
        Assert.Contains("maximum duration", run.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(now, run.CompletedUtc);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: forced retry fails the current processing run and
    /// requeues the group immediately without waiting for max run time.
    /// </summary>
    [Fact]
    public async Task RetryGroupAsync_WhenForceTrueAndRunIsProcessing_FailsRunAndRequeuesGroup()
    {
        var now = _time.GetUtcNow();
        using (var db = CreateDb(PrimaryWorkspace))
        {
            db.TriageGroups.Add(SeedGroup("triage-group-force-retry", "processing", now.AddMinutes(-1)));
            db.TriageReports.Add(SeedReport("triage-report-force-retry", "triage-group-force-retry", "Force retry report", now.AddMinutes(-1)));
            db.TriageResearchRuns.Add(SeedRun("triage-run-force-retry", "triage-group-force-retry", "processing", now.AddMinutes(-1)));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        var sut = CreateService(PrimaryWorkspace, maxRunTime: TimeSpan.FromMinutes(30));

        var group = await sut.RetryGroupAsync("triage-group-force-retry", force: true, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("collecting", group.Status);
        Assert.Null(group.LastError);
        Assert.Equal(now, group.QuietDeadlineUtc);
        var run = Assert.Single((await sut.QueryRunsAsync(
            status: "failed",
            groupId: "triage-group-force-retry",
            workspacePath: PrimaryWorkspace, cancellationToken: TestContext.Current.CancellationToken)).Items);
        Assert.Equal("triage-run-force-retry", run.RunId);
        Assert.Contains("force retried", run.Error, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(now, run.CompletedUtc);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-005: retrying a group with a stale CreatedTodoId clears the
    /// unreadable TODO reference so the next sweep can run instead of skipping it.
    /// </summary>
    [Fact]
    public async Task RetryGroupAsync_WhenCreatedTodoIdIsUnreadable_ClearsReferenceAndRequeuesGroup()
    {
        var now = _time.GetUtcNow();
        using (var db = CreateDb(PrimaryWorkspace))
        {
            db.TriageGroups.Add(SeedGroup(
                "triage-group-stale-created-todo",
                "completed",
                now.AddMinutes(-10),
                createdTodoId: "BUG-TRIAGE-006"));
            db.TriageReports.Add(SeedReport(
                "triage-report-stale-created-todo",
                "triage-group-stale-created-todo",
                "Stale created TODO report",
                now.AddMinutes(-10)));
            db.TriageResearchRuns.Add(SeedRun(
                "triage-run-stale-created-todo",
                "triage-group-stale-created-todo",
                "completed",
                now.AddMinutes(-11),
                completedUtc: now.AddMinutes(-10),
                createdTodoId: "BUG-TRIAGE-006"));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        var sut = CreateService(PrimaryWorkspace);

        var group = await sut.RetryGroupAsync("triage-group-stale-created-todo", cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("collecting", group.Status);
        Assert.Null(group.CreatedTodoId);
        Assert.Null(group.LastError);
        Assert.Equal(now, group.QuietDeadlineUtc);
        var run = Assert.Single((await sut.QueryRunsAsync(
            status: "failed",
            groupId: "triage-group-stale-created-todo",
            workspacePath: PrimaryWorkspace, cancellationToken: TestContext.Current.CancellationToken)).Items);
        Assert.Equal("triage-run-stale-created-todo", run.RunId);
        Assert.Null(run.CreatedTodoId);
        Assert.Contains("stale created TODO reference", run.Error, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TEST-TRIAGE-001: the Director/Web dashboard endpoint returns read-only queue buckets,
    /// linked reports, and AI run history with result/status fields for the active workspace.
    /// </summary>
    [Fact]
    public async Task GetDashboardAsync_WithGroupsReportsAndRuns_ReturnsQueueBucketsAndRunHistory()
    {
        var now = _time.GetUtcNow();
        using (var db = CreateDb(PrimaryWorkspace))
        {
            db.TriageGroups.AddRange(
                SeedGroup("triage-group-new", "new", now),
                SeedGroup("triage-group-processing", "processing", now.AddMinutes(-1)),
                SeedGroup("triage-group-completed", "completed", now.AddMinutes(-2), createdTodoId: "BUG-TRIAGE-001"));
            db.TriageReports.AddRange(
                SeedReport("triage-report-new", "triage-group-new", "New queue report", now),
                SeedReport("triage-report-processing", "triage-group-processing", "Processing queue report", now.AddMinutes(-1)),
                SeedReport("triage-report-completed", "triage-group-completed", "Completed report", now.AddMinutes(-2)));
            db.TriageResearchRuns.Add(SeedRun(
                "triage-run-completed",
                "triage-group-completed",
                "completed",
                now.AddMinutes(-3),
                completedUtc: now.AddMinutes(-2),
                responseJson: """{"title":"Fix dashboard","summary":"Expose run status."}""",
                rawOutput: """{"title":"Fix dashboard"}""",
                createdTodoId: "BUG-TRIAGE-001"));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        var dashboard = await CreateService(PrimaryWorkspace).GetDashboardAsync(PrimaryWorkspace, cancellationToken: TestContext.Current.CancellationToken);

        var triageQueueGroup = Assert.Single(dashboard.TriageQueue);
        Assert.Equal("triage-group-new", triageQueueGroup.GroupId);
        Assert.Equal("New queue report", triageQueueGroup.Reports.Single().Title);
        var reportGroupQueueGroup = Assert.Single(dashboard.ReportGroupQueue);
        Assert.Equal("triage-group-processing", reportGroupQueueGroup.GroupId);
        var run = Assert.Single(dashboard.RunHistory);
        Assert.Equal("triage-run-completed", run.RunId);
        Assert.Equal("completed", run.Status);
        Assert.Equal("completed", run.GroupStatus);
        Assert.Equal("BUG-TRIAGE-001", run.CreatedTodoId);
        Assert.Contains("Expose run status", run.ResponseJson, StringComparison.Ordinal);
    }

    /// <summary>TEST-TRIAGE-001: dashboard reads return explicit empty collections when no triage rows exist.</summary>
    [Fact]
    public async Task GetDashboardAsync_NoRows_ReturnsEmptyCollections()
    {
        var dashboard = await CreateService(PrimaryWorkspace).GetDashboardAsync(PrimaryWorkspace, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Empty(dashboard.TriageQueue);
        Assert.Empty(dashboard.ReportGroupQueue);
        Assert.Empty(dashboard.RunHistory);
        Assert.Equal(0, dashboard.TotalGroupCount);
        Assert.Equal(0, dashboard.TotalRunCount);
    }

    /// <summary>TEST-TRIAGE-001: run-history queries preserve workspace, status, and group filters.</summary>
    [Fact]
    public async Task QueryRunsAsync_WithFilters_ReturnsWorkspaceScopedRuns()
    {
        var now = _time.GetUtcNow();
        using (var db = CreateDb(PrimaryWorkspace))
        {
            db.TriageGroups.Add(SeedGroup("triage-group-primary", "completed", now));
            db.TriageResearchRuns.AddRange(
                SeedRun(
                    "triage-run-primary",
                    "triage-group-primary",
                    "completed",
                    now,
                    agentStdout: "codex stdout",
                    agentStderr: "codex stderr",
                    agentExitCode: 0),
                SeedRun("triage-run-processing", "triage-group-primary", "processing", now.AddMinutes(1)));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        using (var db = CreateDb(AlternateWorkspace))
        {
            db.TriageGroups.Add(SeedGroup("triage-group-other", "completed", now, workspacePath: AlternateWorkspace));
            db.TriageResearchRuns.Add(SeedRun(
                "triage-run-other",
                "triage-group-other",
                "completed",
                now,
                workspacePath: AlternateWorkspace));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        var result = await CreateService(PrimaryWorkspace).QueryRunsAsync(
            status: "completed",
            groupId: "triage-group-primary",
            workspacePath: PrimaryWorkspace, cancellationToken: TestContext.Current.CancellationToken);

        var run = Assert.Single(result.Items);
        Assert.Equal("triage-run-primary", run.RunId);
        Assert.Equal(PrimaryWorkspace, run.WorkspacePath);
        Assert.Equal("codex stdout", run.AgentStdout);
        Assert.Equal("codex stderr", run.AgentStderr);
        Assert.Equal(0, run.AgentExitCode);
        Assert.Equal(1, result.TotalCount);
    }

    /// <summary>
    /// TEST-TRIAGE-002: triage-created TODO queries return TODO ids and persisted TODO creation
    /// timestamps while preserving group/run context and workspace isolation.
    /// </summary>
    [Fact]
    public async Task QueryCreatedTodosAsync_ReturnsTodoIdsCreatedAtUtcAndTriageContext()
    {
        var now = _time.GetUtcNow();
        using (var db = CreateDb(PrimaryWorkspace))
        {
            db.TriageGroups.Add(SeedGroup(
                "triage-group-completed",
                "completed",
                now,
                createdTodoId: "BUG-TRIAGE-001"));
            db.TriageGroups.Add(SeedGroup(
                "triage-group-missing-anchor",
                "completed",
                now.AddMinutes(-1),
                createdTodoId: "BUG-TRIAGE-999"));
            db.TriageResearchRuns.Add(SeedRun(
                "triage-run-completed",
                "triage-group-completed",
                "completed",
                now.AddMinutes(1),
                completedUtc: now.AddMinutes(2),
                createdTodoId: "BUG-TRIAGE-001"));
            db.TodoItems.Add(SeedTodoItem("BUG-TRIAGE-001", "Created triage TODO"));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        using (var db = CreateDb(AlternateWorkspace))
        {
            db.TriageGroups.Add(SeedGroup(
                "triage-group-other",
                "completed",
                now,
                createdTodoId: "BUG-TRIAGE-002",
                workspacePath: AlternateWorkspace));
            db.TodoItems.Add(SeedTodoItem("BUG-TRIAGE-002", "Other workspace triage TODO", AlternateWorkspace));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        var result = await CreateService(PrimaryWorkspace).QueryCreatedTodosAsync(PrimaryWorkspace, cancellationToken: TestContext.Current.CancellationToken);

        var item = Assert.Single(result.Items);
        Assert.Equal("BUG-TRIAGE-001", item.TodoId);
        Assert.Equal(now.AddMinutes(2), item.CreatedAtUtc);
        Assert.Equal(PrimaryWorkspace, item.WorkspacePath);
        Assert.Equal("triage-group-completed", item.GroupId);
        Assert.Equal("triage-run-completed", item.RunId);
        Assert.Equal("completed", item.GroupStatus);
        Assert.Equal("completed", item.RunStatus);
        Assert.Equal(1, result.TotalCount);
    }

    /// <summary>TEST-TRIAGE-003: selected reports can be moved into a new editable triage group.</summary>
    [Fact]
    public async Task CreateGroupFromSelectionAsync_MovesSelectedReportsToNewGroup()
    {
        var now = _time.GetUtcNow();
        using (var db = CreateDb(PrimaryWorkspace))
        {
            db.TriageGroups.AddRange(
                SeedGroup("triage-group-source-a", "collecting", now.AddMinutes(-2)),
                SeedGroup("triage-group-source-b", "collecting", now.AddMinutes(-1)));
            db.TriageReports.AddRange(
                SeedReport("triage-report-a", "triage-group-source-a", "First report", now.AddMinutes(-2)),
                SeedReport("triage-report-b", "triage-group-source-b", "Second report", now.AddMinutes(-1)));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        var result = await CreateService(PrimaryWorkspace).CreateGroupFromSelectionAsync(new TriageGroupSelectionRequest
        {
            ReportIds = ["triage-report-a"],
            GroupIds = ["triage-group-source-b"],
            Title = "Manual group",
            Summary = "Grouped manually",
        }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("Manual group", result.Group.Title);
        Assert.Equal("Grouped manually", result.Group.Summary);
        Assert.Equal("queued", result.Group.Status);
        Assert.Equal(now, result.Group.QuietDeadlineUtc);
        Assert.Equal(2, result.Group.ReportCount);
        Assert.Equal(2, result.MovedReportCount);
        Assert.Equal(["triage-group-source-a", "triage-group-source-b"], result.RemovedGroupIds.Order(StringComparer.Ordinal));
        Assert.All(result.Group.Reports, report => Assert.Equal(result.Group.GroupId, report.GroupId));

        var dashboard = await CreateService(PrimaryWorkspace).GetDashboardAsync(PrimaryWorkspace, cancellationToken: TestContext.Current.CancellationToken);
        Assert.DoesNotContain(dashboard.TriageQueue, group => group.GroupId == result.Group.GroupId);
        var queuedGroup = Assert.Single(dashboard.ReportGroupQueue, group => group.GroupId == result.Group.GroupId);
        Assert.Equal("queued", queuedGroup.Status);
    }

    /// <summary>TEST-TRIAGE-003: selected groups can be merged into an existing editable target group.</summary>
    [Fact]
    public async Task MergeGroupsAsync_MovesSourceGroupReportsIntoTargetGroup()
    {
        var now = _time.GetUtcNow();
        using (var db = CreateDb(PrimaryWorkspace))
        {
            db.TriageGroups.AddRange(
                SeedGroup("triage-group-target", "collecting", now.AddMinutes(-3)),
                SeedGroup("triage-group-source", "collecting", now.AddMinutes(-2)));
            db.TriageReports.AddRange(
                SeedReport("triage-report-target", "triage-group-target", "Target report", now.AddMinutes(-3)),
                SeedReport("triage-report-source", "triage-group-source", "Source report", now.AddMinutes(-2)));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        var result = await CreateService(PrimaryWorkspace).MergeGroupsAsync(
            "triage-group-target",
            new TriageGroupSelectionRequest { GroupIds = ["triage-group-source"] }, cancellationToken: TestContext.Current.CancellationToken);

        Assert.Equal("triage-group-target", result.Group.GroupId);
        Assert.Equal("queued", result.Group.Status);
        Assert.Equal(now, result.Group.QuietDeadlineUtc);
        Assert.Equal(2, result.Group.ReportCount);
        Assert.Equal(1, result.MovedReportCount);
        Assert.Equal(["triage-group-source"], result.RemovedGroupIds);
        Assert.Contains(result.Group.Reports, report => report.ReportId == "triage-report-source");

        using var verifyDb = CreateDb(PrimaryWorkspace);
        Assert.Null(await verifyDb.TriageGroups.FindAsync(["triage-group-source"], cancellationToken: TestContext.Current.CancellationToken));
        Assert.Equal("triage-group-target", (await verifyDb.TriageReports.FindAsync(["triage-report-source"], cancellationToken: TestContext.Current.CancellationToken))!.GroupId);
    }

    /// <summary>TEST-TRIAGE-003: groups with run history cannot be regrouped.</summary>
    [Fact]
    public async Task MergeGroupsAsync_WhenSourceGroupHasRunHistory_Throws()
    {
        var now = _time.GetUtcNow();
        using (var db = CreateDb(PrimaryWorkspace))
        {
            db.TriageGroups.AddRange(
                SeedGroup("triage-group-target", "collecting", now),
                SeedGroup("triage-group-source", "collecting", now.AddMinutes(-1)));
            db.TriageReports.Add(SeedReport("triage-report-source", "triage-group-source", "Source report", now.AddMinutes(-1)));
            db.TriageResearchRuns.Add(SeedRun("triage-run-source", "triage-group-source", "completed", now));
            await db.SaveChangesAsync(cancellationToken: TestContext.Current.CancellationToken);
        }

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(PrimaryWorkspace).MergeGroupsAsync(
                "triage-group-target",
                new TriageGroupSelectionRequest { GroupIds = ["triage-group-source"] }, cancellationToken: TestContext.Current.CancellationToken));
        Assert.Contains("run history", error.Message, StringComparison.OrdinalIgnoreCase);
    }

    private static TriageReportRequest CreateReport(string dedupeKey) => new()
    {
        Title = "REPL triage wrapper failure",
        Summary = "workflow.triage.report returns the wrong envelope.",
        Component = "repl",
        ErrorSignature = "method_not_found",
        DedupeKey = dedupeKey,
        AffectedPaths = ["src/McpServer.Repl.Core/ReplCommandDispatcher.cs"],
    };

    private static TriageGroupEntity SeedGroup(
        string groupId,
        string status,
        DateTimeOffset timestamp,
        string? createdTodoId = null,
        string workspacePath = PrimaryWorkspace)
        => new()
        {
            WorkspaceId = workspacePath,
            GroupId = groupId,
            GroupKey = $"{workspacePath}|{groupId}",
            EffectiveWorkspacePath = workspacePath,
            Title = $"{groupId} title",
            Summary = $"{groupId} summary",
            Status = status,
            ReportCount = 1,
            FirstReportAtUtc = timestamp,
            LastReportAtUtc = timestamp,
            QuietDeadlineUtc = timestamp.AddMinutes(15),
            CreatedTodoId = createdTodoId,
        };

    private static TriageReportEntity SeedReport(
        string reportId,
        string groupId,
        string title,
        DateTimeOffset timestamp,
        string workspacePath = PrimaryWorkspace)
        => new()
        {
            WorkspaceId = workspacePath,
            ReportId = reportId,
            GroupId = groupId,
            OriginalWorkspacePath = workspacePath,
            EffectiveWorkspacePath = workspacePath,
            Title = title,
            Summary = $"{title} summary",
            Fingerprint = $"{reportId}-fingerprint",
            Status = "grouped",
            CreatedUtc = timestamp,
        };

    private static TriageResearchRunEntity SeedRun(
        string runId,
        string groupId,
        string status,
        DateTimeOffset startedUtc,
        DateTimeOffset? completedUtc = null,
        string? responseJson = null,
        string? rawOutput = null,
        string? agentStdout = null,
        string? agentStderr = null,
        int? agentExitCode = null,
        string? createdTodoId = null,
        string workspacePath = PrimaryWorkspace)
        => new()
        {
            WorkspaceId = workspacePath,
            RunId = runId,
            GroupId = groupId,
            Status = status,
            PromptTemplateId = "triage-research-bug-report",
            Prompt = "rendered prompt",
            GroupJson = """{"groupId":"test"}""",
            RawOutput = rawOutput,
            AgentStdout = agentStdout,
            AgentStderr = agentStderr,
            AgentExitCode = agentExitCode,
            ResponseJson = responseJson,
            StartedUtc = startedUtc,
            CompletedUtc = completedUtc,
            CreatedTodoId = createdTodoId,
        };

    private static TodoItemEntity SeedTodoItem(
        string todoId,
        string title,
        string workspacePath = PrimaryWorkspace)
        => new()
        {
            WorkspaceId = workspacePath,
            Id = todoId,
            Title = title,
            Section = "Backlog",
            Priority = "high",
        };

    private TriageService CreateService(
        string workspacePath,
        IReadOnlyList<WorkspaceDto>? workspaces = null,
        ITriageResearchRunner? runner = null,
        ITodoService? todo = null,
        ITriageTodoCreator? todoCreator = null,
        IPromptTemplateService? promptTemplates = null,
        TimeSpan? quietPeriod = null,
        TimeSpan? maxRunTime = null)
    {
        var workspaceContext = new WorkspaceContext { WorkspacePath = workspacePath, WorkspaceName = Path.GetFileName(workspacePath) };
        var workspaceService = Substitute.For<IWorkspaceService>();
        workspaceService.ListAsync(Arg.Any<CancellationToken>())
            .Returns(new WorkspaceListResult(workspaces ?? [Workspace(workspacePath, Path.GetFileName(workspacePath))], workspaces?.Count ?? 1));
        var promptTemplateService = promptTemplates ?? Substitute.For<IPromptTemplateService>();
        promptTemplateService.TestAsync(Arg.Any<string>(), Arg.Any<PromptTemplateTestRequest>(), Arg.Any<CancellationToken>())
            .Returns(new PromptTemplateTestResult
            {
                Success = true,
                RenderedContent = "rendered triage prompt",
            });

        var todoService = todo ?? Substitute.For<ITodoService>();

        return new TriageService(
            CreateDb(workspacePath),
            workspaceContext,
            workspaceService,
            runner ?? Substitute.For<ITriageResearchRunner>(),
            todoService,
            todoCreator ?? new ForwardingTriageTodoCreator(todoService),
            promptTemplateService,
            Microsoft.Extensions.Options.Options.Create(new TriageOptions
            {
                QuietPeriod = quietPeriod ?? TimeSpan.FromMinutes(15),
                MaxRunTime = maxRunTime ?? TimeSpan.FromMinutes(30),
            }),
            _time,
            NullLogger<TriageService>.Instance);
    }

    private sealed class ForwardingTriageTodoCreator : ITriageTodoCreator
    {
        private readonly ITodoService _todoService;

        public ForwardingTriageTodoCreator(ITodoService todoService)
        {
            _todoService = todoService;
        }

        public Task<TodoMutationResult> CreateAsync(TodoCreateRequest request, CancellationToken cancellationToken = default)
            => _todoService.CreateAsync(request, cancellationToken);
    }

    private McpDbContext CreateDb(string workspacePath)
        => new(_dbOptions, new WorkspaceContext { WorkspacePath = workspacePath });

    private static WorkspaceDto Workspace(string path, string name) => new()
    {
        WorkspacePath = path,
        Name = name,
        TodoPath = "docs/Project/TODO.yaml",
        StatusPrompt = string.Empty,
        ImplementPrompt = string.Empty,
        PlanPrompt = string.Empty,
    };

    private sealed class ManualTimeProvider : TimeProvider
    {
        private DateTimeOffset _utcNow;

        public ManualTimeProvider(DateTimeOffset utcNow) => _utcNow = utcNow;

        public override DateTimeOffset GetUtcNow() => _utcNow;

        public void Advance(TimeSpan value) => _utcNow = _utcNow.Add(value);
    }
}
