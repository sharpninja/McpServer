using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Models;
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

    private static TriageReportRequest CreateReport(string dedupeKey) => new()
    {
        Title = "REPL triage wrapper failure",
        Summary = "workflow.triage.report returns the wrong envelope.",
        Component = "repl",
        ErrorSignature = "method_not_found",
        DedupeKey = dedupeKey,
        AffectedPaths = ["src/McpServer.Repl.Core/ReplCommandDispatcher.cs"],
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
