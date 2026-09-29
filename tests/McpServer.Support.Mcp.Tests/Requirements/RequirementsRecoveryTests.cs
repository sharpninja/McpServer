using System.Data;
using System.Diagnostics;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Requirements;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Requirements;

/// <summary>
/// TEST-MCP-REQRECOVERY-001: atomic requirements recovery on SQLite.
/// Dry-run writes nothing. Apply is one serializable commit. A second item failure or a
/// failed SaveChanges leaves zero requirement rows and zero run rows.
/// </summary>
public sealed class RequirementsRecoveryTests
{
    private const string WorkspacePath = "/tmp/mcp-req-recovery";

    /// <summary>Dry-run reports create and update and does not store a run. GET is 404.</summary>
    [Fact]
    public async Task PlanAsync_DoesNotWrite_AndGetIsNotFound()
    {
        await using var connection = OpenConnection();
        var (sut, db) = Build(connection);
        using (db)
        {
            db.Database.EnsureCreated();
            await SeedAsync(db, "FR-MCP-REQRECOVERY-001", "original").ConfigureAwait(true);

            var plan = await sut.PlanAsync(Request("plan-001", Item("fr", "FR-MCP-REQRECOVERY-001", "next"), Item("test", "TEST-MCP-REQRECOVERY-001", "prove")), TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal("planned", plan.Status);
            Assert.False(plan.Applied);
            Assert.Contains(plan.Items, item => item.Id == "FR-MCP-REQRECOVERY-001" && item.Action == "update");
            Assert.Contains(plan.Items, item => item.Id == "TEST-MCP-REQRECOVERY-001" && item.Action == "create");
            Assert.Equal("original", await TitleAsync(db, "fr", "FR-MCP-REQRECOVERY-001").ConfigureAwait(true));
            Assert.Equal(0, Count(connection, "SELECT COUNT(*) FROM RequirementsRecoveryRuns"));
            var missing = await Assert.ThrowsAsync<RequirementsRecoveryNotFoundException>(() =>
                sut.GetAsync("plan-001", TestContext.Current.CancellationToken)).ConfigureAwait(true);
            Assert.Equal(404, McpErrorClassifier.Classify(missing).StatusCode);
        }
    }

    /// <summary>Apply creates and updates in one commit, then replay does not write again.</summary>
    [Fact]
    public async Task ApplyAsync_CommitsBothRows_AndReplayDoesNotRewrite()
    {
        await using var connection = OpenConnection();
        var capture = new IsolationCaptureInterceptor();
        var (sut, db) = Build(connection, capture);
        using (db)
        {
            db.Database.EnsureCreated();
            await SeedAsync(db, "FR-MCP-REQRECOVERY-001", "original").ConfigureAwait(true);
            var request = Request("apply-001", Item("fr", "FR-MCP-REQRECOVERY-001", "updated"), Item("tr", "TR-MCP-REQRECOVERY-001", "technical"));

            var applied = await sut.ApplyAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);

            Assert.Equal(IsolationLevel.Serializable, capture.Isolation);
            Assert.Equal("applied", applied.Status);
            Assert.True(applied.Applied);
            Assert.False(applied.Replay);
            Assert.Equal(2, Count(connection, "SELECT COUNT(*) FROM Requirements"));
            Assert.Equal(1, Count(connection, "SELECT COUNT(*) FROM RequirementsRecoveryRuns"));
            Assert.Equal("updated", await TitleAsync(db, "fr", "FR-MCP-REQRECOVERY-001").ConfigureAwait(true));

            Execute(connection, "UPDATE Requirements SET Title = 'mutated' WHERE Id = 'FR-MCP-REQRECOVERY-001'");
            var replay = await sut.ApplyAsync(request, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(replay.Replay);
            Assert.Equal("mutated", Scalar(connection, "SELECT Title FROM Requirements WHERE Id = 'FR-MCP-REQRECOVERY-001'"));
            Assert.Equal(1, Count(connection, "SELECT COUNT(*) FROM RequirementsRecoveryRuns"));

            var stored = await sut.GetAsync("apply-001", TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Equal("apply-001", stored.IdempotencyKey);
            Assert.False(stored.Replay);
            Assert.Equal(applied.PayloadHash, stored.PayloadHash);
        }
    }

    /// <summary>A different payload for a stored key is 409 and leaves the original rows.</summary>
    [Fact]
    public async Task ApplyAsync_DifferentPayload_IsConflictWithoutMutation()
    {
        await using var connection = OpenConnection();
        var (sut, db) = Build(connection);
        using (db)
        {
            db.Database.EnsureCreated();
            await sut.ApplyAsync(Request("apply-002", Item("fr", "FR-MCP-REQRECOVERY-001", "kept")), TestContext.Current.CancellationToken).ConfigureAwait(true);

            var conflict = await Assert.ThrowsAsync<RequirementsRecoveryConflictException>(() =>
                sut.ApplyAsync(Request("apply-002", Item("fr", "FR-MCP-REQRECOVERY-001", "changed")), TestContext.Current.CancellationToken)).ConfigureAwait(true);

            Assert.Equal(409, McpErrorClassifier.Classify(conflict).StatusCode);
            Assert.Equal("kept", await TitleAsync(db, "fr", "FR-MCP-REQRECOVERY-001").ConfigureAwait(true));
            Assert.Equal(1, Count(connection, "SELECT COUNT(*) FROM Requirements"));
            Assert.Equal(1, Count(connection, "SELECT COUNT(*) FROM RequirementsRecoveryRuns"));
        }
    }

    /// <summary>An invalid second item is 400 and writes neither requirements nor a run.</summary>
    [Fact]
    public async Task ApplyAsync_InvalidSecondItem_WritesNothing()
    {
        await using var connection = OpenConnection();
        var (sut, db) = Build(connection);
        using (db)
        {
            db.Database.EnsureCreated();
            var ex = await Assert.ThrowsAsync<ArgumentException>(() => sut.ApplyAsync(
                Request("bad-001", Item("fr", "FR-MCP-REQRECOVERY-001", "first"), Item("tr", "TR-MCP-001", "invalid")),
                TestContext.Current.CancellationToken)).ConfigureAwait(true);

            Assert.Equal(400, McpErrorClassifier.Classify(ex).StatusCode);
            Assert.Equal(0, Count(connection, "SELECT COUNT(*) FROM Requirements"));
            Assert.Equal(0, Count(connection, "SELECT COUNT(*) FROM RequirementsRecoveryRuns"));
        }
    }

    /// <summary>A SaveChanges failure rolls back. No requirement row and no run row remain.</summary>
    [Fact]
    public async Task ApplyAsync_SaveFailure_RollsBackEveryRow()
    {
        await using var connection = OpenConnection();
        var (sut, db) = Build(connection, new ThrowingSaveInterceptor());
        using (db)
        {
            db.Database.EnsureCreated();
            await Assert.ThrowsAsync<InvalidOperationException>(() => sut.ApplyAsync(
                Request("boom-001", Item("fr", "FR-MCP-REQRECOVERY-001", "first"), Item("test", "TEST-MCP-REQRECOVERY-001", "second")),
                TestContext.Current.CancellationToken)).ConfigureAwait(true);

            Assert.Equal(0, Count(connection, "SELECT COUNT(*) FROM Requirements"));
            Assert.Equal(0, Count(connection, "SELECT COUNT(*) FROM RequirementsRecoveryRuns"));
        }
    }

    /// <summary>A hung SaveChanges is retryable HTTP 503 and persists nothing.</summary>
    [Fact]
    public async Task ApplyAsync_HungSave_IsRetryable503()
    {
        await using var connection = OpenConnection();
        var (sut, db) = Build(connection, new DelayedSaveInterceptor(TimeSpan.FromSeconds(4)), budget: TimeSpan.FromSeconds(1));
        using (db)
        {
            db.Database.EnsureCreated();
            var clock = Stopwatch.StartNew();
            var ex = await Assert.ThrowsAsync<StorageCommandBudgetExceededException>(() => sut.ApplyAsync(
                Request("slow-001", Item("fr", "FR-MCP-REQRECOVERY-001", "slow")),
                TestContext.Current.CancellationToken)).ConfigureAwait(true);
            clock.Stop();

            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(3), $"Recovery save budget took {clock.Elapsed}.");
            var classified = McpErrorClassifier.Classify(ex);
            Assert.Equal(503, classified.StatusCode);
            Assert.Equal(McpErrorClassifier.BackendUnavailable, classified.Code);
            Assert.True(classified.Retryable);
            Assert.Equal(0, Count(connection, "SELECT COUNT(*) FROM Requirements"));
            Assert.Equal(0, Count(connection, "SELECT COUNT(*) FROM RequirementsRecoveryRuns"));
        }
    }

    /// <summary>The model key is the workspace plus idempotency key.</summary>
    [Fact]
    public void Model_PrimaryKey_IsWorkspaceAndIdempotencyKey()
    {
        using var connection = OpenConnection();
        var (_, db) = Build(connection);
        using (db)
        {
            var entity = db.Model.FindEntityType(typeof(RequirementsRecoveryRunEntity));
            Assert.NotNull(entity);
            var names = entity!.FindPrimaryKey()!.Properties.Select(property => property.Name).ToArray();
            Assert.Equal(["WorkspaceId", "IdempotencyKey"], names);
        }
    }

    private static async Task SeedAsync(McpDbContext db, string id, string title)
    {
        db.Requirements.Add(new RequirementEntity
        {
            WorkspaceId = WorkspacePath,
            Kind = "fr",
            Id = id,
            Title = title,
            Body = "seed",
            Priority = "medium",
            Status = "pending",
            ScopeStartLayerKey = "layer-1",
            CreatedAtUtc = "2026-09-29T00:00:00Z",
            UpdatedAtUtc = "2026-09-29T00:00:00Z",
        });
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(false);
    }

    private static async Task<string> TitleAsync(McpDbContext db, string kind, string id)
    {
        var row = await db.Requirements.SingleAsync(requirement => requirement.Kind == kind && requirement.Id == id, TestContext.Current.CancellationToken).ConfigureAwait(false);
        return row.Title;
    }

    private static RequirementsRecoveryRequest Request(string key, params RequirementsRecoveryItemRequest[] items)
        => new() { IdempotencyKey = key, Items = items.ToList() };

    private static RequirementsRecoveryItemRequest Item(string kind, string id, string title)
        => new() { Kind = kind, Id = id, Title = title, Body = title + " body" };

    private static (RequirementsRecoveryService Sut, McpDbContext Db) Build(
        SqliteConnection connection,
        IInterceptor? interceptor = null,
        TimeSpan? budget = null)
    {
        var builder = new DbContextOptionsBuilder<McpDbContext>().UseSqlite(connection);
        if (interceptor is not null)
            builder.AddInterceptors(interceptor);
        var workspace = new WorkspaceContext { WorkspacePath = WorkspacePath };
        var db = new McpDbContext(builder.Options, workspace);
        db.OverrideWorkspaceId(WorkspacePath);
        return (new RequirementsRecoveryService(db, workspace, budget), db);
    }

    private static SqliteConnection OpenConnection()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();
        return connection;
    }

    private static long Count(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (long)(command.ExecuteScalar() ?? 0L);
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }

    private static string Scalar(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        return (string)(command.ExecuteScalar() ?? string.Empty);
    }

    /// <summary>Records the isolation level of the active transaction during SaveChanges.</summary>
    private sealed class IsolationCaptureInterceptor : SaveChangesInterceptor
    {
        public IsolationLevel? Isolation { get; private set; }

        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            Isolation = eventData.Context?.Database.CurrentTransaction?.GetDbTransaction().IsolationLevel;
            return base.SavingChangesAsync(eventData, result, cancellationToken);
        }
    }

    /// <summary>Fails SaveChanges before the provider writes.</summary>
    private sealed class ThrowingSaveInterceptor : SaveChangesInterceptor
    {
        /// <inheritdoc />
        public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
            => throw new InvalidOperationException("simulated recovery save failure");
    }

    /// <summary>Delays SaveChanges until the caller cancels or the delay elapses.</summary>
    private sealed class DelayedSaveInterceptor : SaveChangesInterceptor
    {
        private readonly TimeSpan _delay;

        public DelayedSaveInterceptor(TimeSpan delay) => _delay = delay;

        /// <inheritdoc />
        public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
            DbContextEventData eventData,
            InterceptionResult<int> result,
            CancellationToken cancellationToken = default)
        {
            await Task.Delay(_delay, cancellationToken).ConfigureAwait(false);
            return await base.SavingChangesAsync(eventData, result, cancellationToken).ConfigureAwait(false);
        }
    }
}

/// <summary>TEST-MCP-REQRECOVERY-001: REST status mapping for recovery.</summary>
public sealed class RequirementsRecoveryControllerTests
{
    /// <summary>dry-run, apply, invalid mode, conflict, not found, and budget exhaustion map to 200/400/409/404/503.</summary>
    [Fact]
    public async Task PostAndGet_MapStatusContract()
    {
        var fake = new FakeRecovery();
        var controller = new RequirementsRecoveryController(fake);
        var ok = await controller.PostAsync(new RequirementsRecoveryRequest { Mode = "dry-run", IdempotencyKey = "k" }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(200, Assert.IsType<OkObjectResult>(ok.Result).StatusCode);

        fake.Error = new RequirementsRecoveryConflictException("Requirements recovery idempotency key 'k' conflicts with a stored payload.");
        var conflict = await controller.PostAsync(new RequirementsRecoveryRequest { Mode = "apply", IdempotencyKey = "k" }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(409, StatusOf(conflict.Result));

        var invalid = await controller.PostAsync(new RequirementsRecoveryRequest { Mode = "nope" }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(400, StatusOf(invalid.Result));

        fake.Error = new RequirementsRecoveryNotFoundException("Requirements recovery run 'missing' was not found.");
        var missing = await controller.GetAsync("missing", TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(404, StatusOf(missing.Result));

        fake.Error = new StorageCommandBudgetExceededException(TimeSpan.FromSeconds(5));
        var unavailable = await controller.PostAsync(new RequirementsRecoveryRequest { Mode = "apply" }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Equal(503, StatusOf(unavailable.Result));
    }

    private static int? StatusOf(ActionResult? result) => result switch
    {
        ObjectResult objectResult => objectResult.StatusCode,
        StatusCodeResult status => status.StatusCode,
        _ => null,
    };

    private sealed class FakeRecovery : IRequirementsRecoveryService
    {
        public Exception? Error { get; set; }

        public Task<RequirementsRecoveryResult> PlanAsync(RequirementsRecoveryRequest request, CancellationToken cancellationToken = default)
            => Next();

        public Task<RequirementsRecoveryResult> ApplyAsync(RequirementsRecoveryRequest request, CancellationToken cancellationToken = default)
            => Next();

        public Task<RequirementsRecoveryResult> GetAsync(string idempotencyKey, CancellationToken cancellationToken = default)
            => Next();

        private Task<RequirementsRecoveryResult> Next()
        {
            if (Error is not null)
                return Task.FromException<RequirementsRecoveryResult>(Error);
            return Task.FromResult(new RequirementsRecoveryResult { Status = "planned", IdempotencyKey = "k" });
        }
    }
}
