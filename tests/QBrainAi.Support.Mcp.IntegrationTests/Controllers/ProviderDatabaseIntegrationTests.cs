using System.Globalization;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.EntityFrameworkCore;

namespace QBrainAi.Support.Mcp.IntegrationTests.Controllers;

/// <summary>
/// TEST-MCP-101, TEST-MCP-102: Verifies that the provider factory can apply migrations and persist data on
/// clean SQLite, PostgreSQL and SQL Server databases; the SQL Server cases run on a private server the harness
/// starts (LocalDB where installed, otherwise a temporary SQL Server 2025 Developer instance; PLAN-SESSIONTRIAGEFIX-001 B11).
/// These tests exercise the actual runtime DbContext wiring without editing production code.
/// </summary>
[Trait("Category", "Integration")]
public sealed class ProviderDatabaseIntegrationTests
{
    /// <summary>
    /// Verifies that the SQLite provider can migrate and persist data against a clean on-disk database file.
    /// </summary>
    [Fact]
    public async Task Sqlite_CleanDatabase_AppliesMigrationsAndPersistsEntity()
    {
        await using var workspace = ProviderIntegrationTestSupport.CreateWorkspace();
        var databasePath = workspace.GetDatabasePath("sqlite-provider-clean.db");
        var factory = ProviderIntegrationTestSupport.CreateFactory(
            workspace,
            new Dictionary<string, string?>
            {
                ["Mcp:DatabaseProvider"] = "sqlite",
                ["Mcp:DataSource"] = databasePath,
            });

        try
        {
            using var client = factory.CreateClient();
            _ = client;

            await ProviderIntegrationTestSupport.AssertDatabaseRoundTripAsync(factory, "Sqlite", string.Empty).ConfigureAwait(true);
            await ProviderIntegrationTestSupport.AssertHandoffIngestionStorageAsync(factory).ConfigureAwait(true);

            Assert.True(File.Exists(databasePath), "The SQLite provider should materialize a clean on-disk database file.");
        }
        finally
        {
            factory.Dispose();
        }
    }

    /// <summary>
    /// TEST-MCP-102 AC-002 (PLAN-SESSIONTRIAGEFIX-001 B11): verifies that the SQL Server provider can migrate and persist on a clean, uniquely
    /// named database of a private server the harness starts and removes (LocalDB where installed, otherwise a temporary SQL Server 2025
    /// Developer instance); the database is dropped when the case ends.
    /// </summary>
    [Fact]
    public async Task SqlServer_CleanDatabase_AppliesMigrationsAndPersistsEntity()
    {
        await using var workspace = ProviderIntegrationTestSupport.CreateWorkspace();
        await using var server = await SqlServerSandbox.CreateAsync().ConfigureAwait(true);
        var databaseName = $"mcp_provider_{Guid.NewGuid():N}";
        var factory = ProviderIntegrationTestSupport.CreateFactory(
            workspace,
            new Dictionary<string, string?>
            {
                ["Mcp:DatabaseProvider"] = "sqlserver",
                ["Mcp:SqlServerConnectionString"] = server.DatabaseConnectionString(databaseName),
            });

        try
        {
            using var client = factory.CreateClient();
            _ = client;

            await ProviderIntegrationTestSupport.AssertDatabaseRoundTripAsync(factory, "SqlServer", string.Empty).ConfigureAwait(true);
            await ProviderIntegrationTestSupport.AssertHandoffIngestionStorageAsync(factory).ConfigureAwait(true);
        }
        finally
        {
            factory.Dispose();
        }
    }

    /// <summary>
    /// Verifies that the PostgreSQL provider can migrate a clean database to current head and
    /// persist HandoffIngestionRuns/HandoffDiagnostics. Uses the repository ephemeral cluster
    /// or MCP_TEST_POSTGRES_CONNECTION. No skip.
    /// </summary>
    [Fact]
    public async Task PostgreSql_CleanDatabase_AppliesHandoffMigrationAndPersistsEntity()
    {
        await using var workspace = ProviderIntegrationTestSupport.CreateWorkspace();
        await using var postgres = new EphemeralPostgresSandbox();
        var databaseName = $"mcp_handoff_{Guid.NewGuid():N}";
        postgres.CreateDatabase(databaseName);
        var factory = ProviderIntegrationTestSupport.CreateFactory(
            workspace,
            new Dictionary<string, string?>
            {
                ["Mcp:DatabaseProvider"] = "postgresql",
                ["Mcp:PostgresConnectionString"] = postgres.GetDatabaseConnectionString(databaseName),
            });

        try
        {
            using var client = factory.CreateClient();
            _ = client;

            await ProviderIntegrationTestSupport.AssertDatabaseRoundTripAsync(factory, "Npgsql", string.Empty).ConfigureAwait(true);
            await ProviderIntegrationTestSupport.AssertHandoffIngestionStorageAsync(factory).ConfigureAwait(true);
        }
        finally
        {
            factory.Dispose();
            postgres.DropDatabase(databaseName);
        }
    }

    /// <summary>
    /// TEST-MCP-102 AC-001 mock gate (PLAN-SESSIONTRIAGEFIX-001 B11, triage-report-ead2fdaa): the sandbox plan assertion accepts a private
    /// LocalDB instance and a temporary SQL Server 2025 Developer instance (license accepted, port on 127.0.0.1, image never pulled, removed
    /// afterwards), and rejects a shared server, no server, a skip, a server left behind, a port on every interface, a pulled image, another
    /// SQL Server version, another edition and an unaccepted license. Data: hand-built plans; no server.
    /// </summary>
    [Fact]
    public void SqlServerSandboxPlanAssertion_AcceptsLocalDbOrTemporaryInstanceAndRejectsSharedOrNone()
    {
        var localDb = new SqlServerSandboxPlan(SqlServerSandboxKind.LocalDb, null, null, false, null, false, true);
        var temporary = new SqlServerSandboxPlan(
            SqlServerSandboxKind.TemporaryInstance, "mcr.microsoft.com/mssql/server:2025-latest", "Developer", true, "127.0.0.1", false, true);
        SqlServerSandboxPlan.AssertAllowed(localDb);
        SqlServerSandboxPlan.AssertAllowed(temporary);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => SqlServerSandboxPlan.AssertAllowed(temporary with { Kind = SqlServerSandboxKind.Shared }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => SqlServerSandboxPlan.AssertAllowed(localDb with { Kind = SqlServerSandboxKind.None }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => SqlServerSandboxPlan.AssertAllowed(localDb with { Kind = SqlServerSandboxKind.Skip }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => SqlServerSandboxPlan.AssertAllowed(localDb with { RemovedAfterCase = false }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => SqlServerSandboxPlan.AssertAllowed(temporary with { RemovedAfterCase = false }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => SqlServerSandboxPlan.AssertAllowed(temporary with { HostAddress = "0.0.0.0" }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => SqlServerSandboxPlan.AssertAllowed(temporary with { PullImage = true }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => SqlServerSandboxPlan.AssertAllowed(temporary with { Image = "mcr.microsoft.com/mssql/server:2022-latest" }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => SqlServerSandboxPlan.AssertAllowed(temporary with { ProductId = "Express" }));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => SqlServerSandboxPlan.AssertAllowed(temporary with { AcceptEula = false }));
    }

    /// <summary>
    /// TEST-MCP-102 AC-003 mock gate (TR-MCP-DB-DTO-001 AC-002; PLAN-SESSIONTRIAGEFIX-001 B8, triage-report-71d801ad): the soft-delete
    /// round-trip assertion accepts a datetime2 column whose read-back equals the written instant at offset zero, and rejects a datetimeoffset
    /// column, a shifted instant, a non-zero offset and a missing value. Data: hand-built column types and instants; no server.
    /// </summary>
    [Fact]
    public void RecoveryRunDeletedAtAssertion_AcceptsDatetime2UtcRoundTripAndRejectsOffsetColumnOrShiftedInstant()
    {
        var written = new DateTimeOffset(2026, 10, 15, 21, 30, 0, TimeSpan.Zero).AddTicks(1234567);
        AssertUtcDeletedAtRoundTrip("datetime2", written, written);
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertUtcDeletedAtRoundTrip("datetimeoffset", written, written));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertUtcDeletedAtRoundTrip("datetime2", written, written.AddHours(1)));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertUtcDeletedAtRoundTrip("datetime2", written, written.ToOffset(TimeSpan.FromHours(-4))));
        Assert.ThrowsAny<Xunit.Sdk.XunitException>(() => AssertUtcDeletedAtRoundTrip("datetime2", written, null));
    }

    /// <summary>
    /// TEST-MCP-102 AC-003 and TR-MCP-DB-DTO-001 AC-002 (PLAN-SESSIONTRIAGEFIX-001 B8, triage-report-71d801ad): on a clean SQL Server database
    /// migrated to head, RequirementsRecoveryRuns.DeletedAtUtc is datetime2 and a soft-deleted recovery run reads back the DeletedAtUtc
    /// McpDbContext wrote, as the same UTC instant. Data: one recovery run on a uniquely named database of the case's private server.
    /// </summary>
    [Fact]
    public async Task SqlServer_RequirementsRecoveryRun_SoftDelete_RoundTripsUtcDeletedAt()
    {
        const string workspaceId = "F:\\recovery-run-test";
        const string idempotencyKey = "b8-recovery-run";
        await using var server = await SqlServerSandbox.CreateAsync().ConfigureAwait(true);
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlServer(
                server.DatabaseConnectionString($"mcp_recovery_{Guid.NewGuid():N}"),
                sql => sql.MigrationsAssembly("QBrainAi.Storage.SqlServerMigrations"))
            .Options;
        using var db = new McpDbContext(options);
        db.Database.SetCommandTimeout(TimeSpan.FromMinutes(3));
        await db.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        db.OverrideWorkspaceId(workspaceId);
        if (!await db.Workspaces.AnyAsync(item => item.WorkspaceId == workspaceId, TestContext.Current.CancellationToken).ConfigureAwait(true))
        {
            db.Workspaces.Add(new WorkspaceEntity
            {
                WorkspaceId = workspaceId,
                WorkspacePath = workspaceId,
                Name = "recovery-run-test",
                TodoPath = "docs/todo.yaml",
                IsEnabled = true,
            });
            await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        var run = new RequirementsRecoveryRunEntity
        {
            WorkspaceId = workspaceId,
            IdempotencyKey = idempotencyKey,
            PayloadHash = new string('c', 64),
            ResultJson = "{}",
            CreatedAtUtc = DateTimeOffset.UtcNow.ToString("O", CultureInfo.InvariantCulture),
        };
        db.RequirementsRecoveryRuns.Add(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);

        db.RequirementsRecoveryRuns.Remove(run);
        await db.SaveChangesAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var written = Assert.IsType<DateTimeOffset>(db.Entry(run).Property("DeletedAtUtc").CurrentValue);
        db.ChangeTracker.Clear();

        var columnType = await db.Database
            .SqlQuery<string>($"SELECT DATA_TYPE AS [Value] FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = 'RequirementsRecoveryRuns' AND COLUMN_NAME = 'DeletedAtUtc'")
            .SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        var readBack = await db.RequirementsRecoveryRuns.IgnoreQueryFilters()
            .Where(item => item.WorkspaceId == workspaceId && item.IdempotencyKey == idempotencyKey)
            .Select(item => EF.Property<DateTimeOffset?>(item, "DeletedAtUtc"))
            .SingleAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
        AssertUtcDeletedAtRoundTrip(columnType, written, readBack);
    }

    // TEST-MCP-102 AC-003 and TR-MCP-DB-DTO-001 AC-002: the column the SQL Server migrations leave is datetime2, and a soft-deleted row reads
    // back the instant McpDbContext wrote, as UTC.
    private static void AssertUtcDeletedAtRoundTrip(string columnType, DateTimeOffset written, DateTimeOffset? readBack)
    {
        Assert.Equal("datetime2", columnType, ignoreCase: true);
        var value = Assert.NotNull(readBack);
        Assert.Equal(TimeSpan.Zero, value.Offset);
        Assert.Equal(written.UtcTicks, value.UtcTicks);
    }
}
