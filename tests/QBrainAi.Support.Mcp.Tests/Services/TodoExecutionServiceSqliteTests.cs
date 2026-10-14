using QBrainAi.Support.Mcp.Ingestion;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Requirements;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-BYRD-SVC-002: Regression guard that exercises <see cref="TodoExecutionService"/> against
/// the real SQLite provider. The EF InMemory provider used by <see cref="TodoExecutionServiceTests"/>
/// translates DateTimeOffset ordering unconditionally, masking a translation gap in SQLite where
/// DateTimeOffset values cannot appear in ORDER BY clauses. These tests keep the provider-specific
/// hydration path honest by asserting the same scenarios succeed against SQLite.
/// </summary>
public sealed class TodoExecutionServiceSqliteTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly McpDbContext _db;
    private readonly IRequirementsDocumentService _requirementsDocumentService = Substitute.For<IRequirementsDocumentService>();
    private readonly IProcessRunner _processRunner = Substitute.For<IProcessRunner>();
    private readonly TodoExecutionService _sut;
    private readonly ITodoService _todoService = Substitute.For<ITodoService>();
    private readonly string _workspacePath;

    /// <summary>
    /// Initializes a SQLite-backed <see cref="McpDbContext"/> so that ORDER BY translation issues
    /// surface during tests. Keeps a single open connection for the fixture lifetime so the in-memory
    /// database survives across EF saves.
    /// </summary>
    public TodoExecutionServiceSqliteTests()
    {
        _workspacePath = Path.Combine(Path.GetTempPath(), "TodoExecutionServiceSqliteTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workspacePath);

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var dbOptions = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;
        _db = new McpDbContext(dbOptions, new WorkspaceContext { WorkspacePath = _workspacePath });
        _db.Database.EnsureCreated();
        _db.OverrideWorkspaceId(_workspacePath);

        var workspaceService = Substitute.For<IWorkspaceService>();
        workspaceService.GetAsync(_workspacePath, Arg.Any<CancellationToken>())
            .Returns(new WorkspaceDto
            {
                WorkspacePath = _workspacePath,
                Name = "TodoExecutionServiceSqliteTests",
                TodoPath = "docs/Project/TODO.yaml",
                DataDirectory = _workspacePath,
                DateTimeCreated = DateTimeOffset.UtcNow,
                DateTimeModified = DateTimeOffset.UtcNow,
                StatusPrompt = "status",
                ImplementPrompt = "implement",
                PlanPrompt = "plan",
            });

        _todoService.GetByIdAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((QBrainAi.Support.Mcp.Services.TodoFlatItem?)null);
        _todoService.CreateAsync(Arg.Any<QBrainAi.Support.Mcp.Services.TodoCreateRequest>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var request = call.Arg<QBrainAi.Support.Mcp.Services.TodoCreateRequest>()!;
                return new QBrainAi.Support.Mcp.Services.TodoMutationResult(
                    true,
                    null,
                    new QBrainAi.Support.Mcp.Services.TodoFlatItem
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

    /// <summary>Disposes the SQLite connection, EF context, and workspace scratch directory.</summary>
    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
        try { Directory.Delete(_workspacePath, recursive: true); } catch { /* best effort */ }
    }

    /// <summary>
    /// TEST-MCP-BYRD-SVC-002: Hydrating a TODO with linked session turns against SQLite must succeed.
    /// Prior to the fix, <c>OrderByDescending(turn => turn.Timestamp)</c> in
    /// <see cref="TodoExecutionService"/>.GetTurnSummariesAsync and GetFilesModifiedAsync threw
    /// <see cref="NotSupportedException"/> because SQLite cannot compare DateTimeOffset in ORDER BY.
    /// The regression protects the live deployment path from regressing onto InMemory-only tests.
    /// </summary>
    [Fact]
    public async Task GetExecutionContextAsync_WithLinkedSessionTurns_OrdersByTimestampOnSqlite()
    {
        var todoId = await CreateExecutionTodoAsync(
            requirementIds: ["FR-BYRD-001"],
            relevantFiles: ["src/QBrainAi.Services/Services/TodoExecutionService.cs"]).ConfigureAwait(true);

        // Seed two turns with distinct DateTimeOffset timestamps so ordering is observable. The
        // session-log writer itself stores them (triage-report-04659db1), so the reader is held to
        // the list type the writer uses rather than to a hand-seeded literal.
        await SubmitTurnThroughWriterAsync("sqlite-older", "req-20260211T100100Z-sqlite-001", "Older turn evidence.", "docs/older.md", DateTimeOffset.UtcNow.AddMinutes(-10)).ConfigureAwait(true);
        await SubmitTurnThroughWriterAsync("sqlite-newer", "req-20260211T100200Z-sqlite-002", "Newer turn evidence.", "docs/newer.md", DateTimeOffset.UtcNow).ConfigureAwait(true);

        await _sut.LinkTodoToSessionTurnsAsync(
            _workspacePath,
            todoId,
            new LinkTodoToSessionTurnsRequest
            {
                SessionTurnIds = ["req-20260211T100100Z-sqlite-001", "req-20260211T100200Z-sqlite-002"]
            }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        var context = await _sut.GetExecutionContextAsync(_workspacePath, todoId, 1, 2, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.NotNull(context);
        Assert.Equal(todoId, context!.TodoId);
        Assert.Equal(2, context.RecentTurnSummaries.Count);
        // Newest turn first verifies the post-materialization ordering preserved the original intent.
        Assert.Contains("Newer", context.RecentTurnSummaries[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Older", context.RecentTurnSummaries[1], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docs/newer.md", context.RelevantFiles);
        Assert.Contains("docs/older.md", context.RelevantFiles);
    }

    /// <summary>
    /// TEST-MCP-104 / FR-MCP-082 / TR-MCP-BYRD-002 (triage-report-04659db1, mock gate): the session-log
    /// writer stores a submitted turn's files-modified value as exactly one string-list row of that
    /// turn, so the execution-context test that reads it has a non-vacuous fixture written by the real
    /// writer rather than hand-seeded rows. Data: SQLite; one turn submitted through SessionLogService.
    /// </summary>
    [Fact]
    public async Task FilesModifiedFixture_SessionLogWriterStoresOneRowForTheTurnFile()
    {
        const string requestId = "req-20260211T100100Z-fixture-001";
        await SubmitTurnThroughWriterAsync("a6-fixture", requestId, "Fixture turn.", "docs/fixture.md", DateTimeOffset.UtcNow).ConfigureAwait(true);

        var rows = await _db.SessionLogTurnStringLists
            .AsNoTracking()
            .Where(row => row.Value == "docs/fixture.md" && row.SessionLogTurn!.RequestId == requestId)
            .ToListAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Single(rows);
    }

    /// <summary>
    /// TEST-MCP-104 / FR-MCP-082 (triage-report-04659db1): the list type the session-log writer stores
    /// for files modified is the one shared definition the execution-context reader filters on, so the
    /// writer and the reader cannot drift apart again. Data: SQLite; one turn submitted through
    /// SessionLogService.
    /// </summary>
    [Fact]
    public async Task FilesModifiedListType_WriterStoresTheSharedDefinition()
    {
        const string requestId = "req-20260211T100100Z-listtype-001";
        await SubmitTurnThroughWriterAsync("a6-listtype", requestId, "List type turn.", "docs/listtype.md", DateTimeOffset.UtcNow).ConfigureAwait(true);

        var row = await _db.SessionLogTurnStringLists
            .AsNoTracking()
            .SingleAsync(item => item.Value == "docs/listtype.md" && item.SessionLogTurn!.RequestId == requestId, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(SessionLogTurnStringListTypes.FileModified, row.ListType);
    }

    // FR-MCP-082: a turn written by the session-log service itself, so the reader is bound to the writer's contract.
    private async Task SubmitTurnThroughWriterAsync(string sessionSuffix, string requestId, string queryTitle, string fileModified, DateTimeOffset timestamp)
    {
        var writer = new SessionLogService(
            _db,
            NullLogger<SessionLogService>.Instance,
            Substitute.For<IChangeEventBus>(),
            new WorkspaceContext { WorkspacePath = _workspacePath });
        await writer.SubmitAsync(new UnifiedSessionLogDto
        {
            SourceType = "Codex",
            SessionId = $"Codex-20260211T100000Z-{sessionSuffix}",
            Title = queryTitle,
            Model = "gpt-5",
            Started = timestamp.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
            LastUpdated = timestamp.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
            Status = "in_progress",
            Turns =
            [
                new UnifiedRequestEntryDto
                {
                    RequestId = requestId,
                    Timestamp = timestamp.ToString("o", System.Globalization.CultureInfo.InvariantCulture),
                    QueryTitle = queryTitle,
                    Response = queryTitle,
                    Status = "in_progress",
                    PlanFile = SessionLogTurnContextValidator.NoneSentinel,
                    TodoId = SessionLogTurnContextValidator.NoneSentinel,
                    FilesModified = [fileModified],
                },
            ],
        }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
    }

    private async Task<string> CreateExecutionTodoAsync(
        IReadOnlyList<string>? requirementIds = null,
        IReadOnlyList<string>? relevantFiles = null)
    {
        var phase = await _sut.CreateIterationPhaseAsync(
            _workspacePath,
            new CreateIterationPhaseRequest
            {
                Name = "SQLite regression phase",
                Summary = "Guards DateTimeOffset ordering translation"
            }).ConfigureAwait(true);

        var result = await _sut.CreateTodosFromPlanAsync(
            _workspacePath,
            new CreateTodosFromPlanRequest
            {
                PhaseId = phase.PhaseId,
                PlanId = "PLAN-SQLITE-001",
                Todos =
                [
                    new PlanTodoInput
                    {
                        Title = "SQLite regression todo",
                        Goal = "Exercise DateTimeOffset ordering via SQLite.",
                        Summary = "Reproduces the real provider path.",
                        AcceptanceCriteria = ["Hydrates turns without throwing"],
                        Constraints = ["Must run on SQLite"],
                        RequirementIds = requirementIds,
                        RelevantFiles = relevantFiles,
                    }
                ]
            }).ConfigureAwait(true);

        return result.TodoIds[0];
    }
}
