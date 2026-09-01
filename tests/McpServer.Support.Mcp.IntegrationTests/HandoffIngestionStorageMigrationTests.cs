using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace McpServer.Support.Mcp.IntegrationTests;

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
                .UseSqlite(connection, sqlite => sqlite.MigrationsAssembly("McpServer.Storage.SqliteMigrations"))
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

    /// <summary>SQL Server LocalDB private instance: apply head, round-trip, downgrade, re-upgrade.</summary>
    [Fact]
    public async Task SqlServer_HandoffMigration_DowngradeAndReupgrade()
    {
        await using var localDb = await SqlLocalDbSandbox.CreateAsync().ConfigureAwait(true);
        var databaseName = $"mcp_handoff_mig_{Guid.NewGuid():N}";
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlServer(
                $"{localDb.ConnectionString}Database={databaseName};Command Timeout=180;",
                sql => sql.MigrationsAssembly("McpServer.Storage.SqlServerMigrations"))
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
                    npgsql => npgsql.MigrationsAssembly("McpServer.Storage.PostgreSqlMigrations"))
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

    private static async Task AssertProviderCycleAsync(
        DbContextOptions<McpDbContext> options,
        string precedingMigration,
        string handoffMigration)
    {
        using (var db = new McpDbContext(options))
        {
            db.Database.SetCommandTimeout(TimeSpan.FromMinutes(3));
            await db.Database.MigrateAsync().ConfigureAwait(true);
            var applied = await db.Database.GetAppliedMigrationsAsync().ConfigureAwait(true);
            Assert.Contains(applied, name => name == handoffMigration);
            await RoundTripAsync(db).ConfigureAwait(true);
        }

        using (var db = new McpDbContext(options))
        {
            db.Database.SetCommandTimeout(TimeSpan.FromMinutes(3));
            await db.GetService<IMigrator>().MigrateAsync(precedingMigration).ConfigureAwait(true);
            var applied = await db.Database.GetAppliedMigrationsAsync().ConfigureAwait(true);
            Assert.DoesNotContain(applied, name => name == handoffMigration);
        }

        using (var db = new McpDbContext(options))
        {
            db.Database.SetCommandTimeout(TimeSpan.FromMinutes(3));
            await db.Database.MigrateAsync().ConfigureAwait(true);
            var applied = await db.Database.GetAppliedMigrationsAsync().ConfigureAwait(true);
            Assert.Contains(applied, name => name == handoffMigration);
            await RoundTripAsync(db).ConfigureAwait(true);
        }
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
