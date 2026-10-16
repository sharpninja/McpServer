// SPDX-License-Identifier: Apache-2.0
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace QBrainAi.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-SUPPORT-024 AC-005 (FR-SUPPORT-016 ac-6, TR-SUPPORT-CORE-016 ac-4; PLAN-SESSIONTRIAGEFIX-001 A7 follow-up,
/// triage-report-905c3ac1b6af4d57b9c24c2383642379): concurrent dialog appends to one session-log turn, each on its own context, leave every
/// appended item stored once with a distinct ordinal on SQLite, PostgreSQL and SQL Server; an attempt that loses a conflict is refused with an
/// error McpErrorClassifier reports as retryable and is retried, and any other error fails the case. Temporary instances only: a file SQLite
/// database, a scratch database on the ephemeral PostgreSQL cluster, and a private SQL Server (LocalDB or a temporary SQL Server 2025
/// Developer instance). Data: one session whose turn holds dialog item seed-0, and six writers each appending writer-N.
/// </summary>
[Trait("Category", "Integration")]
public sealed class SessionLogConcurrentTurnWriteIntegrationTests
{
    private const int Writers = 6;
    private const int Attempts = 20;
    private const string Source = "Codex";
    private const string SessionId = "Codex-20261016T120000Z-concurrent";
    private const string RequestId = "req-20261016T120000Z-concurrent";

    /// <summary>TEST-SUPPORT-024 AC-005 on SQLite: a file database migrated to head.</summary>
    [Fact]
    public async Task Sqlite_ConcurrentDialogAppends_StoreDistinctOrdinals()
    {
        var path = Path.Combine(Path.GetTempPath(), $"turnwrite-provider-{Guid.NewGuid():N}.db");
        try
        {
            await AssertConcurrentAppendsAsync(builder => builder.UseSqlite(
                $"Data Source={path};Pooling=False",
                sqlite => sqlite.MigrationsAssembly("QBrainAi.Storage.SqliteMigrations"))).ConfigureAwait(true);
        }
        finally
        {
            foreach (var suffix in new[] { string.Empty, "-wal", "-shm", "-journal" })
                File.Delete(path + suffix);
        }
    }

    /// <summary>TEST-SUPPORT-024 AC-005 on PostgreSQL: a scratch database on the ephemeral cluster, dropped afterwards.</summary>
    [Fact]
    public async Task PostgreSql_ConcurrentDialogAppends_StoreDistinctOrdinals()
    {
        await using var postgres = new EphemeralPostgresSandbox();
        var databaseName = $"mcp_turnwrite_{Guid.NewGuid():N}";
        postgres.CreateDatabase(databaseName);
        try
        {
            await AssertConcurrentAppendsAsync(builder => builder.UseNpgsql(
                postgres.GetDatabaseConnectionString(databaseName),
                npgsql => npgsql.MigrationsAssembly("QBrainAi.Storage.PostgreSqlMigrations"))).ConfigureAwait(true);
        }
        finally
        {
            postgres.DropDatabase(databaseName);
        }
    }

    /// <summary>TEST-SUPPORT-024 AC-005 on SQL Server: a uniquely named database of the case's private server.</summary>
    [Fact]
    public async Task SqlServer_ConcurrentDialogAppends_StoreDistinctOrdinals()
    {
        await using var server = await SqlServerSandbox.CreateAsync().ConfigureAwait(true);
        var connectionString = new SqlConnectionStringBuilder(server.DatabaseConnectionString($"mcp_turnwrite_{Guid.NewGuid():N}")) { Pooling = false }.ToString();
        await AssertConcurrentAppendsAsync(builder => builder.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly("QBrainAi.Storage.SqlServerMigrations"))).ConfigureAwait(true);
    }

    // TEST-SUPPORT-024 AC-005: the writers append concurrently; a refused attempt must be retryable and is retried with a short backoff; at
    // the end every item is stored once and every ordinal of the turn's dialog is distinct.
    private static async Task AssertConcurrentAppendsAsync(Func<DbContextOptionsBuilder<McpDbContext>, DbContextOptionsBuilder<McpDbContext>> configure)
    {
        var ct = TestContext.Current.CancellationToken;
        var options = configure(new DbContextOptionsBuilder<McpDbContext>()).Options;
        var workspace = new WorkspaceContext { WorkspacePath = "concurrent-turnwrite-provider-workspace" };
        await using (var setupDb = new McpDbContext(options, workspace))
        {
            setupDb.Database.SetCommandTimeout(TimeSpan.FromMinutes(3));
            await setupDb.Database.MigrateAsync(ct).ConfigureAwait(true);
            var setup = new SessionLogService(setupDb, NullLogger<SessionLogService>.Instance, workspaceContext: workspace);
            await setup.SubmitAsync(SeedSession(), cancellationToken: ct).ConfigureAwait(true);
        }

        var start = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        var writers = Enumerable.Range(0, Writers).Select(index => Task.Run(async () =>
        {
            await start.Task.ConfigureAwait(false);
            for (var attempt = 1; ; attempt++)
            {
                await using var db = new McpDbContext(options, workspace);
                var service = new SessionLogService(db, NullLogger<SessionLogService>.Instance, workspaceContext: workspace);
                try
                {
                    await service.AppendProcessingDialogAsync(Source, SessionId, RequestId, [Dialog($"writer-{index}")], ct).ConfigureAwait(false);
                    return;
                }
                catch (Exception ex) when (attempt < Attempts && McpErrorClassifier.Classify(ex).Retryable)
                {
                    await Task.Delay(TimeSpan.FromMilliseconds((15 * attempt) + (7 * index)), ct).ConfigureAwait(false);
                }
            }
        }, ct)).ToArray();
        start.SetResult();
        await Task.WhenAll(writers).ConfigureAwait(true);

        await using var readDb = new McpDbContext(options, workspace);
        var items = await readDb.SessionLogTurns
            .Where(turn => turn.RequestId == RequestId)
            .SelectMany(turn => turn.ProcessingDialog)
            .Select(item => new { item.Ordinal, item.Content })
            .ToListAsync(ct)
            .ConfigureAwait(true);
        Assert.Equal(items.Count, items.Select(item => item.Ordinal).Distinct().Count());
        var expected = new[] { "seed-0" }.Concat(Enumerable.Range(0, Writers).Select(index => $"writer-{index}"));
        Assert.Equal(expected.Order(StringComparer.Ordinal), items.Select(item => item.Content).Order(StringComparer.Ordinal));
    }

    private static UnifiedSessionLogDto SeedSession() => new()
    {
        SourceType = Source,
        SessionId = SessionId,
        Title = "Concurrent turn writes",
        Turns =
        [
            new UnifiedRequestEntryDto
            {
                RequestId = RequestId, Timestamp = "2026-10-16T12:00:00Z", QueryTitle = "Concurrent turn writes", QueryText = "Keep ordinals unique",
                Status = "in_progress", PlanFile = "None", TodoId = "None", ProcessingDialog = [Dialog("seed-0")],
            },
        ],
    };

    private static ProcessingDialogItemDto Dialog(string content)
        => new() { Role = "assistant", Content = content, Category = "note", Timestamp = "2026-10-16T12:00:00Z" };
}
