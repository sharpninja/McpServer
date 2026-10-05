using System.Text.Json;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Services;

/// <summary>
/// TEST-MCP-SESSIONLIFE-004: An isolated SQLite store reproduces stale hook snapshots
/// so terminal history is protected without disabling deliberate turn updates.
/// </summary>
public sealed class SessionLogSupersessionTests : IDisposable
{
    private const string SessionId = "Codex-20261001T190000Z-supersession";
    private const string RequestId = "req-20261001T190000Z-supersession";
    private readonly SqliteConnection _connection;
    private readonly McpDbContext _db;
    private readonly SessionLogService _service;

    /// <summary>Creates an isolated relational fixture; no operator database is accessed.</summary>
    public SessionLogSupersessionTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        var workspace = new WorkspaceContext { WorkspacePath = "supersession-test-workspace" };
        _db = new McpDbContext(new DbContextOptionsBuilder<McpDbContext>().UseSqlite(_connection).Options, workspace);
        _db.Database.EnsureCreated();
        _service = new SessionLogService(_db, NullLogger<SessionLogService>.Instance, workspaceContext: workspace);
    }

    /// <summary>Disposes the isolated database and connection.</summary>
    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    /// <summary>Late cancellation snapshots cannot overwrite any fields or history of a terminal turn.</summary>
    [Theory]
    [InlineData("completed", "canceled")]
    [InlineData("failed", "cancelled")]
    [InlineData("closed", "CANCELED")]
    public async Task SubmitAsync_StaleCancellation_PreservesTerminalTurn(string terminalStatus, string cancellationStatus)
    {
        await SubmitAsync(CreateTurn(terminalStatus, "Verified terminal response")).ConfigureAwait(true);
        var before = JsonSerializer.Serialize(await ReadTurnAsync().ConfigureAwait(true));
        var stale = CreateTurn(cancellationStatus, "Superseded by req-20261001T190001Z-next before it was completed.");
        stale.QueryTitle = "Stale title";
        stale.Interpretation = "Stale interpretation";
        stale.Timestamp = "2026-10-01T18:00:00Z";
        stale.Actions!.Single().Description = "Stale action";

        await SubmitAsync(stale).ConfigureAwait(true);

        Assert.Equal(before, JsonSerializer.Serialize(await ReadTurnAsync().ConfigureAwait(true)));
    }

    /// <summary>Replayed active snapshots cannot reopen or replace any terminal history.</summary>
    [Theory]
    [InlineData("completed")]
    [InlineData("failed")]
    [InlineData("closed")]
    [InlineData("canceled")]
    [InlineData("cancelled")]
    public async Task SubmitAsync_StaleActiveSnapshot_PreservesTerminalTurn(string status)
    {
        await SubmitAsync(CreateTurn(status, "Verified terminal response")).ConfigureAwait(true);
        var before = JsonSerializer.Serialize(await ReadTurnAsync().ConfigureAwait(true));

        await SubmitAsync(CreateTurn("in_progress", "Old pending response")).ConfigureAwait(true);

        Assert.Equal(before, JsonSerializer.Serialize(await ReadTurnAsync().ConfigureAwait(true)));
    }

    /// <summary>Omitted status remains an additive update rather than a request to reopen.</summary>
    [Fact]
    public async Task SubmitAsync_OmittedStatus_EnrichesTerminalTurn()
    {
        await SubmitAsync(CreateTurn("completed", "Verified terminal response")).ConfigureAwait(true);
        await SubmitAsync(new UnifiedRequestEntryDto
        {
            RequestId = RequestId, DesignDecisions = ["Additional evidence"]
        }).ConfigureAwait(true);

        var stored = await ReadTurnAsync().ConfigureAwait(true);
        Assert.Equal("completed", stored.Status);
        Assert.Equal("Verified terminal response", stored.Response);
        Assert.Contains("Additional evidence", stored.DesignDecisions!);
    }

    /// <summary>A completed failsafe restores a response previously replaced by cancellation.</summary>
    [Fact]
    public async Task SubmitAsync_CompletedRecovery_ReplacesCanceledSnapshot()
    {
        await SubmitAsync(CreateTurn("canceled", "Superseded")).ConfigureAwait(true);
        await SubmitAsync(CreateTurn("completed", "Recovered completed response")).ConfigureAwait(true);

        var stored = await ReadTurnAsync().ConfigureAwait(true);
        Assert.Equal("completed", stored.Status);
        Assert.Equal("Recovered completed response", stored.Response);
    }

    /// <summary>Cancellation still works for a genuinely active turn through the snapshot path.</summary>
    [Theory]
    [InlineData("canceled")]
    [InlineData("cancelled")]
    public async Task SubmitAsync_ActiveTurn_CanBeCanceled(string cancellationStatus)
    {
        await SubmitAsync(CreateTurn("in_progress", "Working")).ConfigureAwait(true);
        await SubmitAsync(CreateTurn(cancellationStatus, "Superseded active turn")).ConfigureAwait(true);

        Assert.Equal(cancellationStatus, (await ReadTurnAsync().ConfigureAwait(true)).Status);
    }

    /// <summary>Direct turn updates retain their explicit ability to correct a terminal status.</summary>
    [Theory]
    [InlineData("canceled")]
    [InlineData("in_progress")]
    public async Task UpsertTurnAsync_ExplicitStatus_CanCorrectTerminalTurn(string status)
    {
        await SubmitAsync(CreateTurn("completed", "Original response")).ConfigureAwait(true);
        await _service.UpsertTurnAsync("Codex", SessionId, CreateTurn(status, "Explicit correction"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var stored = await ReadTurnAsync().ConfigureAwait(true);
        Assert.Equal(status, stored.Status);
        Assert.Equal("Explicit correction", stored.Response);
    }

    /// <summary>Same-state snapshots can still enrich or recover terminal responses.</summary>
    [Theory]
    [InlineData("completed")]
    [InlineData("canceled")]
    public async Task SubmitAsync_SameTerminalStatus_CanUpdateResponse(string status)
    {
        await SubmitAsync(CreateTurn(status, "Original response")).ConfigureAwait(true);
        await SubmitAsync(CreateTurn(status, "Recovered response")).ConfigureAwait(true);

        Assert.Equal("Recovered response", (await ReadTurnAsync().ConfigureAwait(true)).Response);
    }

    private Task<long> SubmitAsync(UnifiedRequestEntryDto turn) => _service.SubmitAsync(new UnifiedSessionLogDto
    {
        SourceType = "Codex", SessionId = SessionId, Title = "Supersession regression", Turns = [turn]
    }, cancellationToken: TestContext.Current.CancellationToken);

    /// <summary>A reused context refreshes stale session history while preserving unrelated pending entities.</summary>
    [Fact]
    public async Task SubmitAsync_ReusedContext_PreservesConcurrentCompletionAndUnrelatedChanges()
    {
        await SubmitAsync(CreateTurn("in_progress", "Working")).ConfigureAwait(true);
        var workspace = new WorkspaceContext { WorkspacePath = "supersession-test-workspace" };
        await using var otherDb = new McpDbContext(
            new DbContextOptionsBuilder<McpDbContext>().UseSqlite(_connection).Options, workspace);
        var otherService = new SessionLogService(otherDb, NullLogger<SessionLogService>.Instance, workspaceContext: workspace);
        await otherService.UpsertTurnAsync("Codex", SessionId, CreateTurn("completed", "Concurrent final response"),
            TestContext.Current.CancellationToken).ConfigureAwait(true);

        var unrelated = new SessionLogEntity { SourceType = "Codex", SessionId = "Codex-20261001T190100Z-unrelated", Title = "Pending unrelated session" };
        _db.SessionLogs.Add(unrelated);
        await SubmitAsync(CreateTurn("canceled", "Stale cancellation")).ConfigureAwait(true);

        var stored = await ReadTurnAsync().ConfigureAwait(true);
        Assert.Equal("completed", stored.Status);
        Assert.Equal("Concurrent final response", stored.Response);
        Assert.True(await _db.SessionLogs.AnyAsync(s => s.SessionId == unrelated.SessionId,
            TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>Pending edits to the same tracked graph are rejected rather than silently discarded.</summary>
    [Fact]
    public async Task SubmitAsync_PendingTrackedEdit_RejectsWithoutDiscardingEdit()
    {
        await SubmitAsync(CreateTurn("in_progress", "Working")).ConfigureAwait(true);
        var tracked = Assert.Single(_db.ChangeTracker.Entries<SessionLogTurnEntity>()).Entity;
        tracked.Response = "Unsaved explicit edit";

        await Assert.ThrowsAsync<InvalidOperationException>(() => SubmitAsync(CreateTurn("canceled", "Stale snapshot")));

        Assert.Equal("Unsaved explicit edit", tracked.Response);
        Assert.Equal(EntityState.Modified, _db.Entry(tracked).State);
    }

    /// <summary>Submission never acknowledges a write inside a caller-owned transaction that can roll back.</summary>
    [Fact]
    public async Task SubmitAsync_CallerTransaction_RejectsBeforeMutation()
    {
        await using var transaction = await _db.Database.BeginTransactionAsync(
            System.Data.IsolationLevel.Serializable, TestContext.Current.CancellationToken).ConfigureAwait(true);
        await Assert.ThrowsAsync<InvalidOperationException>(() => SubmitAsync(CreateTurn("completed", "Not durable")));
        Assert.Empty(_db.ChangeTracker.Entries<SessionLogEntity>());
        await transaction.RollbackAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Empty(await _db.SessionLogs.ToListAsync(TestContext.Current.CancellationToken).ConfigureAwait(true));
    }

    /// <summary>An ambient transaction is rejected explicitly before any session write.</summary>
    [Fact]
    public async Task SubmitAsync_AmbientTransaction_RejectsBeforeMutation()
    {
        using var transaction = new System.Transactions.TransactionScope(System.Transactions.TransactionScopeAsyncFlowOption.Enabled);
        var error = await Assert.ThrowsAsync<InvalidOperationException>(() => SubmitAsync(CreateTurn("completed", "Not durable")));
        Assert.Contains("caller-owned", error.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Empty(_db.ChangeTracker.Entries<SessionLogEntity>());
    }

    /// <summary>Two independent contexts cannot commit stale cancellation over a concurrent completion.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SubmitAsync_ConcurrentCompletion_PreservesCompletedResponse(bool explicitTurnWriter)
    {
        var path = Path.Combine(Path.GetTempPath(), $"sessionack-race-{Guid.NewGuid():N}.db");
        var connectionString = $"Data Source={path};Default Timeout=1;Pooling=False";
        var workspace = new WorkspaceContext { WorkspacePath = "concurrent-sessionack-workspace" };
        var options = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connectionString).Options;
        try
        {
            await using var completionDb = new McpDbContext(options, workspace);
            await completionDb.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var completion = new SessionLogService(completionDb, NullLogger<SessionLogService>.Instance, workspaceContext: workspace);
            await completion.SubmitAsync(Session(CreateTurn("in_progress", "Working")),
                cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            completionDb.ChangeTracker.Clear();

            Task<long> CompleteAsync() => explicitTurnWriter
                ? completion.UpsertTurnAsync("Codex", SessionId, CreateTurn("completed", "Concurrent completed response"),
                    TestContext.Current.CancellationToken)
                : completion.SubmitAsync(Session(CreateTurn("completed", "Concurrent completed response")),
                    cancellationToken: TestContext.Current.CancellationToken);

            Exception? completionError = null;
            var interleaving = new SaveInterleaving(async () =>
            {
                completionError = await Record.ExceptionAsync(CompleteAsync).ConfigureAwait(true);
            });
            var cancellationOptions = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connectionString)
                .AddInterceptors(interleaving).Options;
            await using (var cancellationDb = new McpDbContext(cancellationOptions, workspace))
            {
                var cancellation = new SessionLogService(cancellationDb, NullLogger<SessionLogService>.Instance, workspaceContext: workspace);
                await cancellation.SubmitAsync(Session(CreateTurn("canceled", "Stale cancellation")),
                    cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
            }

            if (completionError is not null)
            {
                var busy = Assert.IsType<SqliteException>(completionError is DbUpdateException update ? update.InnerException : completionError);
                Assert.Contains(busy.SqliteErrorCode, new[] { 5, 6 });
                completionDb.ChangeTracker.Clear();
                await CompleteAsync().ConfigureAwait(true);
            }

            completionDb.ChangeTracker.Clear();
            var stored = await completion.GetAsync("Codex", SessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
            var turn = Assert.Single(Assert.IsType<UnifiedSessionLogDto>(stored).Turns!);
            Assert.Equal("completed", turn.Status);
            Assert.Equal("Concurrent completed response", turn.Response);
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
                File.Delete(path + suffix);
        }
    }

    private static UnifiedSessionLogDto Session(UnifiedRequestEntryDto turn) => new()
    {
        SourceType = "Codex", SessionId = SessionId, Title = "Concurrent snapshot regression", Turns = [turn]
    };

    private sealed class SaveInterleaving(Func<Task> beforeSave) : SaveChangesInterceptor
    {
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
        {
            await beforeSave().ConfigureAwait(false);
            return result;
        }
    }

    private async Task<UnifiedRequestEntryDto> ReadTurnAsync()
    {
        _db.ChangeTracker.Clear();
        var stored = await _service.GetAsync("Codex", SessionId, TestContext.Current.CancellationToken).ConfigureAwait(true);
        return Assert.Single(Assert.IsType<UnifiedSessionLogDto>(stored).Turns!);
    }

    private static UnifiedRequestEntryDto CreateTurn(string status, string response) => new()
    {
        RequestId = RequestId, Timestamp = "2026-10-01T19:00:00Z", QueryTitle = "Preserve terminal history",
        QueryText = "Validate the write", Interpretation = "Verify durable persistence", Response = response,
        Status = status, PlanFile = "None", TodoId = "None", Tags = ["receipt"], ContextList = ["src/receipt.cs"],
        Actions = [new UnifiedActionDto { Order = 1, Type = "test", Status = "completed", Description = "Verified action" }],
        ProcessingDialog = [new ProcessingDialogItemDto { Role = "assistant", Content = "Verified dialog", Category = "decision" }],
        DesignDecisions = ["Preserve durable terminal history"], RequirementsDiscovered = ["FR-MCP-SESSIONLIFE-003"],
        FilesModified = ["src/receipt.cs"], Blockers = ["Synthetic blocker"], TokenCount = 42
    };
}
