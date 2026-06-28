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
        });

        Assert.True(result.Success, result.Error);
        Assert.StartsWith("triage-report-", result.ReportId, StringComparison.Ordinal);
        Assert.StartsWith("triage-group-", result.GroupId, StringComparison.Ordinal);
        Assert.Equal("collecting", result.Status);
        Assert.Equal(_time.GetUtcNow().AddMinutes(15), result.QuietDeadlineUtc);

        using var db = CreateDb(PrimaryWorkspace);
        Assert.Equal(1, await db.TriageReports.CountAsync());
        Assert.Equal(1, await db.TriageGroups.CountAsync());
        await runner.DidNotReceiveWithAnyArgs().RunAsync(default!, default);
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-002: matching reports use deterministic grouping and each new report
    /// resets the fifteen minute quiet deadline.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_MatchingDedupeKey_ReusesGroupAndResetsQuietDeadline()
    {
        var sut = CreateService(PrimaryWorkspace);

        var first = await sut.SubmitReportAsync(CreateReport("same-wrapper-bug"));
        _time.Advance(TimeSpan.FromMinutes(5));
        var second = await sut.SubmitReportAsync(CreateReport("same-wrapper-bug"));

        Assert.Equal(first.GroupId, second.GroupId);
        Assert.Equal(_time.GetUtcNow().AddMinutes(15), second.QuietDeadlineUtc);

        using var db = CreateDb(PrimaryWorkspace);
        var group = await db.TriageGroups.SingleAsync(g => g.GroupId == first.GroupId);
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

        var first = await sut.SubmitReportAsync(request);
        _time.Advance(TimeSpan.FromMinutes(5));
        var second = await sut.SubmitReportAsync(request);

        Assert.Equal(first.ReportId, second.ReportId);
        Assert.Equal(first.GroupId, second.GroupId);
        Assert.Equal(first.QuietDeadlineUtc, second.QuietDeadlineUtc);

        using var db = CreateDb(PrimaryWorkspace);
        Assert.Equal(1, await db.TriageReports.CountAsync());
        var group = await db.TriageGroups.SingleAsync();
        Assert.Equal(1, group.ReportCount);
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-006: the same grouping signature in two workspaces creates isolated
    /// groups and does not leak status across workspace filters.
    /// </summary>
    [Fact]
    public async Task SubmitReportAsync_SameSignatureAcrossWorkspaces_CreatesIsolatedGroups()
    {
        var first = await CreateService(PrimaryWorkspace).SubmitReportAsync(CreateReport("shared-bug"));
        var second = await CreateService(AlternateWorkspace).SubmitReportAsync(CreateReport("shared-bug"));

        Assert.NotEqual(first.GroupId, second.GroupId);

        using var primaryDb = CreateDb(PrimaryWorkspace);
        using var alternateDb = CreateDb(AlternateWorkspace);
        Assert.Equal(1, await primaryDb.TriageGroups.CountAsync());
        Assert.Equal(1, await alternateDb.TriageGroups.CountAsync());
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
        });

        Assert.Equal(McpServerWorkspace, result.WorkspacePath);
        using var db = CreateDb(McpServerWorkspace);
        var report = await db.TriageReports.SingleAsync();
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
        });

        Assert.Equal(PrimaryWorkspace, result.WorkspacePath);
        using var db = CreateDb(PrimaryWorkspace);
        Assert.Equal(1, await db.TriageReports.CountAsync());
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
        var submit = await sut.SubmitReportAsync(CreateReport("research-valid"));
        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        Assert.NotNull(researchRequest);
        Assert.Equal("rendered triage prompt", researchRequest.Prompt);
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

        var group = await sut.GetGroupAsync(submit.GroupId);
        Assert.Equal("completed", group.Status);
        Assert.Equal("BUG-TRIAGE-001", group.CreatedTodoId);
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
        var submit = await foreground.SubmitReportAsync(CreateReport("background-scope"));
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
        var group = await verifier.GetGroupAsync(submit.GroupId);
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
        var submit = await sut.SubmitReportAsync(CreateReport("research-invalid"));

        var processed = await sut.ProcessDueGroupsAsync(CancellationToken.None);

        Assert.Equal(1, processed.ProcessedGroups);
        await todo.DidNotReceiveWithAnyArgs().CreateAsync(default!, default);
        var group = await sut.GetGroupAsync(submit.GroupId);
        Assert.Equal("failed", group.Status);
        Assert.Contains("schema", group.LastError, StringComparison.OrdinalIgnoreCase);
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
            await db.SaveChangesAsync();
        }

        var dashboard = await CreateService(PrimaryWorkspace).GetDashboardAsync(PrimaryWorkspace);

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
        var dashboard = await CreateService(PrimaryWorkspace).GetDashboardAsync(PrimaryWorkspace);

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
            await db.SaveChangesAsync();
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
            await db.SaveChangesAsync();
        }

        var result = await CreateService(PrimaryWorkspace).QueryRunsAsync(
            status: "completed",
            groupId: "triage-group-primary",
            workspacePath: PrimaryWorkspace);

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
            await db.SaveChangesAsync();
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
            await db.SaveChangesAsync();
        }

        var result = await CreateService(PrimaryWorkspace).QueryCreatedTodosAsync(PrimaryWorkspace);

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
            await db.SaveChangesAsync();
        }

        var result = await CreateService(PrimaryWorkspace).CreateGroupFromSelectionAsync(new TriageGroupSelectionRequest
        {
            ReportIds = ["triage-report-a"],
            GroupIds = ["triage-group-source-b"],
            Title = "Manual group",
            Summary = "Grouped manually",
        });

        Assert.Equal("Manual group", result.Group.Title);
        Assert.Equal("Grouped manually", result.Group.Summary);
        Assert.Equal("queued", result.Group.Status);
        Assert.Equal(now, result.Group.QuietDeadlineUtc);
        Assert.Equal(2, result.Group.ReportCount);
        Assert.Equal(2, result.MovedReportCount);
        Assert.Equal(["triage-group-source-a", "triage-group-source-b"], result.RemovedGroupIds.Order(StringComparer.Ordinal));
        Assert.All(result.Group.Reports, report => Assert.Equal(result.Group.GroupId, report.GroupId));

        var dashboard = await CreateService(PrimaryWorkspace).GetDashboardAsync(PrimaryWorkspace);
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
            await db.SaveChangesAsync();
        }

        var result = await CreateService(PrimaryWorkspace).MergeGroupsAsync(
            "triage-group-target",
            new TriageGroupSelectionRequest { GroupIds = ["triage-group-source"] });

        Assert.Equal("triage-group-target", result.Group.GroupId);
        Assert.Equal("queued", result.Group.Status);
        Assert.Equal(now, result.Group.QuietDeadlineUtc);
        Assert.Equal(2, result.Group.ReportCount);
        Assert.Equal(1, result.MovedReportCount);
        Assert.Equal(["triage-group-source"], result.RemovedGroupIds);
        Assert.Contains(result.Group.Reports, report => report.ReportId == "triage-report-source");

        using var verifyDb = CreateDb(PrimaryWorkspace);
        Assert.Null(await verifyDb.TriageGroups.FindAsync("triage-group-source"));
        Assert.Equal("triage-group-target", (await verifyDb.TriageReports.FindAsync("triage-report-source"))!.GroupId);
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
            await db.SaveChangesAsync();
        }

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            CreateService(PrimaryWorkspace).MergeGroupsAsync(
                "triage-group-target",
                new TriageGroupSelectionRequest { GroupIds = ["triage-group-source"] }));
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
        IPromptTemplateService? promptTemplates = null,
        TimeSpan? quietPeriod = null)
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

        return new TriageService(
            CreateDb(workspacePath),
            workspaceContext,
            workspaceService,
            runner ?? Substitute.For<ITriageResearchRunner>(),
            todo ?? Substitute.For<ITodoService>(),
            promptTemplateService,
            Microsoft.Extensions.Options.Options.Create(new TriageOptions { QuietPeriod = quietPeriod ?? TimeSpan.FromMinutes(15) }),
            _time,
            NullLogger<TriageService>.Instance);
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
