using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

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
            relevantFiles: ["src/McpServer.Services/Services/TodoExecutionService.cs"]).ConfigureAwait(true);

        // Seed two turns with distinct DateTimeOffset timestamps so ordering is observable.
        await SeedSessionTurnAsync("req-sqlite-001", "Older turn evidence.", "docs/older.md", DateTimeOffset.UtcNow.AddMinutes(-10)).ConfigureAwait(true);
        await SeedSessionTurnAsync("req-sqlite-002", "Newer turn evidence.", "docs/newer.md", DateTimeOffset.UtcNow).ConfigureAwait(true);

        await _sut.LinkTodoToSessionTurnsAsync(
            _workspacePath,
            todoId,
            new LinkTodoToSessionTurnsRequest
            {
                SessionTurnIds = ["req-sqlite-001", "req-sqlite-002"]
            }).ConfigureAwait(true);

        var context = await _sut.GetExecutionContextAsync(_workspacePath, todoId, 1, 2).ConfigureAwait(true);

        Assert.NotNull(context);
        Assert.Equal(todoId, context!.TodoId);
        Assert.Equal(2, context.RecentTurnSummaries.Count);
        // Newest turn first verifies the post-materialization ordering preserved the original intent.
        Assert.Contains("Newer", context.RecentTurnSummaries[0], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Older", context.RecentTurnSummaries[1], StringComparison.OrdinalIgnoreCase);
        Assert.Contains("docs/newer.md", context.RelevantFiles);
        Assert.Contains("docs/older.md", context.RelevantFiles);
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

    private async Task SeedSessionTurnAsync(string requestId, string queryTitle, string fileModified, DateTimeOffset timestamp)
    {
        var session = new SessionLogEntity
        {
            SourceType = "Codex",
            SessionId = $"Codex-{Guid.NewGuid():N}",
            Model = "gpt-5",
            Started = timestamp,
            LastUpdated = timestamp,
        };
        _db.SessionLogs.Add(session);
        await _db.SaveChangesAsync().ConfigureAwait(true);

        var turn = new SessionLogTurnEntity
        {
            SessionLogId = session.Id,
            RequestId = requestId,
            Timestamp = timestamp,
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
