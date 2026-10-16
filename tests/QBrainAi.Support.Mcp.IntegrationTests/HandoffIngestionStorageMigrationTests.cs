using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace QBrainAi.Support.Mcp.IntegrationTests;

/// <summary>
/// TEST-HANDOFF-007: focused new-migration gate for AddHandoffIngestionStorage.
/// Applies clean-head, round-trips handoff rows, then downgrades to the immediately
/// preceding migration and re-upgrades on each supported provider.
/// </summary>
[Trait("Category", "Integration")]
public sealed class HandoffIngestionStorageMigrationTests
{
    /// <summary>SQLite: apply head, round-trip, downgrade to previous, re-upgrade.</summary>
    [Fact]
    public async Task Sqlite_HandoffMigration_DowngradeAndReupgrade()
    {
        var path = Path.Combine(Path.GetTempPath(), $"handoff-mig-{Guid.NewGuid():N}.db");
        var connection = new Microsoft.Data.Sqlite.SqliteConnection($"Data Source={path}");
        await connection.OpenAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        try
        {
            var options = new DbContextOptionsBuilder<McpDbContext>()
                .UseSqlite(connection, sqlite => sqlite.MigrationsAssembly("QBrainAi.Storage.SqliteMigrations"))
                .Options;
            await AssertProviderCycleAsync(
                options,
                "20260812173052_AddSessionLogTurnPlanFileAndTodoId",
                "20260816183137_AddHandoffIngestionStorage").ConfigureAwait(true);
        }
        finally
        {
            await connection.DisposeAsync().ConfigureAwait(true);
            Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    /// <summary>
    /// SQL Server private server (TEST-MCP-102 AC-002; PLAN-SESSIONTRIAGEFIX-001 B11: LocalDB where installed, otherwise a temporary SQL Server
    /// 2025 Developer instance): apply to the handoff migration, downgrade, re-upgrade, then head and round-trip.
    /// </summary>
    [Fact]
    public async Task SqlServer_HandoffMigration_DowngradeAndReupgrade()
    {
        await using var server = await SqlServerSandbox.CreateAsync().ConfigureAwait(true);
        var databaseName = $"mcp_handoff_mig_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlServer(
                server.DatabaseConnectionString(databaseName),
                sql => sql.MigrationsAssembly("QBrainAi.Storage.SqlServerMigrations"))
            .Options;
        await AssertProviderCycleAsync(
            options,
            "20260812173131_AddSessionLogTurnPlanFileAndTodoId",
            "20260816183150_AddHandoffIngestionStorage").ConfigureAwait(true);
    }

    /// <summary>PostgreSQL ephemeral/external cluster: apply head, round-trip, downgrade, re-upgrade.</summary>
    [Fact]
    public async Task PostgreSql_HandoffMigration_DowngradeAndReupgrade()
    {
        await using var postgres = new EphemeralPostgresSandbox();
        var databaseName = $"mcp_handoff_mig_{Guid.NewGuid():N}";
        postgres.CreateDatabase(databaseName);
        try
        {
            var options = new DbContextOptionsBuilder<McpDbContext>()
                .UseNpgsql(
                    postgres.GetDatabaseConnectionString(databaseName),
                    npgsql => npgsql.MigrationsAssembly("QBrainAi.Storage.PostgreSqlMigrations"))
                .Options;
            await AssertProviderCycleAsync(
                options,
                "20260812173136_AddSessionLogTurnPlanFileAndTodoId",
                "20260816183202_AddHandoffIngestionStorage").ConfigureAwait(true);
        }
        finally
        {
            postgres.DropDatabase(databaseName);
        }
    }

    /// <summary>
    /// TEST-HANDOFF-007 AC-001 mock gate (FR-HANDOFF-006; PLAN-SESSIONTRIAGEFIX-001 B12, triage-report-61c2a2b2): the handoff cycle assertion
    /// accepts applied-migration lists where the database stopped at AddHandoffIngestionStorage, the downgrade removed it and the re-upgrade
    /// restored it, and rejects a downgrade that left it applied, a re-upgrade that did not restore it, and a cycle whose starting point already
    /// held the forward-only VersionedAuditPayloads migration (a downgrade from there would have to cross it). Data: hand-built migration-id
    /// lists; no database.
    /// </summary>
    [Fact]
    public void HandoffCycleAssertion_AcceptsReversibleCycleAndRejectsMissingOrForwardOnlyCrossing()
    {
        const string preceding = "20260812173052_AddSessionLogTurnPlanFileAndTodoId";
        const string handoff = "20260816183137_AddHandoffIngestionStorage";
        const string forwardOnly = "20260929140000_VersionedAuditPayloads";
        string[] up = ["20260701192324_StoreDateTimeOffsetAsUtcDateTime", preceding, handoff];
        string[] down = ["20260701192324_StoreDateTimeOffsetAsUtcDateTime", preceding];
        AssertHandoffCycle(up, down, up, handoff);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertHandoffCycle(up, up, up, handoff));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertHandoffCycle(up, down, down, handoff));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertHandoffCycle([.. up, forwardOnly], down, up, handoff));
    }

    // TEST-HANDOFF-007 AC-001: the cycle starts at the handoff migration itself (never past the forward-only VersionedAuditPayloads), the
    // downgrade removes it, and the re-upgrade restores it.
    private static void AssertHandoffCycle(IReadOnlyList<string> afterUp, IReadOnlyList<string> afterDown, IReadOnlyList<string> afterReupgrade, string handoffMigration)
    {
        Assert.Equal(handoffMigration, afterUp[^1]);
        Assert.DoesNotContain(afterUp, name => name.EndsWith("_VersionedAuditPayloads", StringComparison.Ordinal));
        Assert.DoesNotContain(handoffMigration, afterDown);
        Assert.Contains(handoffMigration, afterReupgrade);
    }

    /// <summary>
    /// TEST-HANDOFF-007 AC-003 (FR-HANDOFF-006; PLAN-SESSIONTRIAGEFIX-001 B12, triage-report-61c2a2b2): 20260929140000_VersionedAuditPayloads'
    /// Down raises NotSupportedException on SQLite, PostgreSQL and SQL Server, the forward-only boundary that migration declares, which is why the
    /// handoff cycle stops at the handoff migration. Data: the three providers' migration classes; no database.
    /// </summary>
    [Fact]
    public void VersionedAuditPayloads_DownIsForwardOnly_OnEveryProvider()
    {
        Migration[] migrations =
        [
            new QBrainAi.Support.Mcp.Storage.SqliteMigrations.Migrations.VersionedAuditPayloads(),
            new QBrainAi.Support.Mcp.Storage.PostgreSqlMigrations.Migrations.VersionedAuditPayloads(),
            new QBrainAi.Support.Mcp.Storage.SqlServerMigrations.Migrations.VersionedAuditPayloads(),
        ];
        foreach (var migration in migrations)
            Assert.Throws<NotSupportedException>(() => migration.DownOperations);
    }

    // TEST-HANDOFF-007 AC-001 and AC-002 (PLAN-SESSIONTRIAGEFIX-001 B12): the reversible cycle runs on a database migrated only up to the handoff
    // migration, so it never crosses the forward-only 20260929140000_VersionedAuditPayloads; the database then goes to head, where handoff rows
    // round-trip.
    private static async Task AssertProviderCycleAsync(
        DbContextOptions<McpDbContext> options,
        string precedingMigration,
        string handoffMigration)
    {
        var afterUp = await MigrateToAsync(options, handoffMigration).ConfigureAwait(true);
        var afterDown = await MigrateToAsync(options, precedingMigration).ConfigureAwait(true);
        var afterReupgrade = await MigrateToAsync(options, handoffMigration).ConfigureAwait(true);
        AssertHandoffCycle(afterUp, afterDown, afterReupgrade, handoffMigration);

        using var db = new McpDbContext(options);
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(3));
        await db.Database.MigrateAsync().ConfigureAwait(true);
        var applied = await db.Database.GetAppliedMigrationsAsync().ConfigureAwait(true);
        Assert.Contains(applied, name => name == handoffMigration);
        await RoundTripAsync(db).ConfigureAwait(true);
    }

    // Migrates one fresh context to the named migration and returns the applied migration ids in order.
    private static async Task<IReadOnlyList<string>> MigrateToAsync(DbContextOptions<McpDbContext> options, string targetMigration)
    {
        using var db = new McpDbContext(options);
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(3));
        await db.GetService<IMigrator>().MigrateAsync(targetMigration).ConfigureAwait(true);
        return (await db.Database.GetAppliedMigrationsAsync().ConfigureAwait(true)).ToList();
    }

    private static async Task RoundTripAsync(McpDbContext db)
    {
        const string workspaceId = "F:\\handoff-migration-test";
        db.OverrideWorkspaceId(workspaceId);
        if (!await db.Workspaces.AnyAsync(item => item.WorkspaceId == workspaceId).ConfigureAwait(true))
        {
            db.Workspaces.Add(new WorkspaceEntity
            {
                WorkspaceId = workspaceId,
                WorkspacePath = workspaceId,
                Name = "handoff-migration-test",
                TodoPath = "docs/todo.yaml",
                IsEnabled = true,
            });
            await db.SaveChangesAsync().ConfigureAwait(true);
        }

        var runId = $"handoff-run-{Guid.NewGuid():N}";
        db.HandoffIngestionRuns.Add(new HandoffIngestionRunEntity
        {
            RunId = runId,
            WorkspaceId = workspaceId,
            SourceKind = "Content",
            SourceLocator = "content",
            ContentSha256 = new string('b', 64),
            ExtractedAtUtc = DateTimeOffset.UtcNow,
            PromptVersion = "handoff-todo-draft/v1",
            Mode = "DraftOnly",
            ReviewState = "None",
            ReplayIdentity = Guid.NewGuid().ToString("N").PadRight(64, '0')[..64],
            ProcessingState = "Terminal",
            Succeeded = true,
        });
        db.HandoffDiagnostics.Add(new HandoffDiagnosticEntity
        {
            WorkspaceId = workspaceId,
            RunId = runId,
            Code = "migration_roundtrip",
            Severity = "Info",
            Message = "ok",
            Ordinal = 0,
        });
        await db.SaveChangesAsync().ConfigureAwait(true);
        db.ChangeTracker.Clear();
        Assert.NotNull(await db.HandoffIngestionRuns.SingleAsync(item => item.RunId == runId).ConfigureAwait(true));
        Assert.NotNull(await db.HandoffDiagnostics.SingleAsync(item => item.RunId == runId).ConfigureAwait(true));
    }
}
