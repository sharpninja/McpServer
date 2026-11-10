using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// SQL Server migration binding over a uniquely owned database. Integration: opens the lane's
/// provider connection. FR-MCP-TESTRESOURCE-001 ac-5 preserves first and later EF failures.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-TESTRESOURCE-001 ac-5; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its file from QBrainAi.Support.Mcp.Tests (Storage/AuditPayloadProviderMigrationTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); the collection-membership binding of SqlServerLocalDbCollectionTests that names it moved here too; SqlServerLocalDbCollection, SqlServerTestConnection and SqlServerMigrationFailureReporter are linked into this assembly; only the namespace changed.</remarks>
[Trait("Category", "Integration")]
[Collection(SqlServerLocalDbCollection.Name)]
public sealed class SqlServerAuditPayloadMigrationTests : IDisposable
{
    private readonly string _serverConnectionString = SqlServerTestConnection.Server;
    private readonly string _databaseName = $"mcp_audit_{Guid.NewGuid():N}";
    private readonly SqlServerMigrationFailureReporter _failures = new();
    private bool _databaseCreated;

    /// <summary>Applies real SQL Server migrations and proves audit payload and permission behavior.</summary>
    [Fact]
    public async Task Migration_PersistsCompressedPayloads_AndDeniesAuditMutation()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var admin = new SqlConnection(_serverConnectionString))
        {
            await admin.OpenAsync(cancellationToken);
            await using var create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE [{_databaseName}];";
            await create.ExecuteNonQueryAsync(cancellationToken);
            _databaseCreated = true;
        }

        var connectionString = new SqlConnectionStringBuilder(_serverConnectionString)
        {
            InitialCatalog = _databaseName,
        }.ToString();
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlServer(connectionString, sql =>
            {
                sql.MigrationsAssembly("QBrainAi.Storage.SqlServerMigrations");
                sql.CommandTimeout(120);
            })
            .LogTo(SqlServerMigrationFailureReporter.ShouldCapture, _failures.Observe)
            .Options;

        await using (var db = new McpDbContext(options))
        {
            await _failures.RunAsync(() => db.Database.MigrateAsync(cancellationToken));
            await AuditPayloadProviderAssertions.AssertRoundTripAsync(db, cancellationToken);
        }

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);
        await using var permissions = connection.CreateCommand();
        permissions.CommandText = """
            SELECT COUNT(*)
            FROM sys.database_permissions AS p
            JOIN sys.database_principals AS r ON r.principal_id = p.grantee_principal_id
            JOIN sys.objects AS o ON o.object_id = p.major_id
            WHERE r.name = N'mcp_runtime'
              AND p.state_desc = 'DENY'
              AND (
                  (o.name IN (N'DataAuditLogs', N'TodoAuditHistory')
                   AND p.permission_name IN ('UPDATE', 'DELETE'))
                  OR (o.name = N'__EFMigrationsHistory'
                      AND p.permission_name IN ('INSERT', 'UPDATE', 'DELETE'))
              );
            """;
        Assert.Equal(7, Convert.ToInt32(await permissions.ExecuteScalarAsync(cancellationToken)));
    }

    /// <summary>Drops only the database this test created.</summary>
    public void Dispose()
    {
        if (!_databaseCreated)
            return;
        using var admin = new SqlConnection(_serverConnectionString);
        admin.Open();
        using var drop = admin.CreateCommand();
        drop.CommandText =
            $"ALTER DATABASE [{_databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{_databaseName}];";
        drop.ExecuteNonQuery();
    }
}

/// <summary>
/// PostgreSQL twin of the SQL Server audit payload migration binding: real PostgreSQL migrations over a uniquely owned database persist compressed audit payloads. Integration: opens the ephemeral PostgreSQL server of the lane (TR-MCP-USECASE-018 AC-006).
/// </summary>
/// <remarks>2026-11-07, TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its file from QBrainAi.Support.Mcp.Tests (Storage/AuditPayloadProviderMigrationTests.cs) into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S8, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); the class had no documentation, so this summary and entry were added; EphemeralPostgresFixture is linked into this assembly; only the namespace changed.</remarks>
[Trait("Category", "Integration")]
public sealed class PostgreSqlAuditPayloadMigrationTests : IClassFixture<EphemeralPostgresFixture>, IDisposable
{
    private readonly string _serverConnectionString;
    private readonly string _databaseName = $"mcp_audit_{Guid.NewGuid():N}";
    private bool _databaseCreated;

    public PostgreSqlAuditPayloadMigrationTests(EphemeralPostgresFixture fixture)
        => _serverConnectionString = fixture.ServerConnectionString;

    /// <summary>
    /// FR-MCP-AUDIT-001; TR-MCP-AUDIT-002; TEST-MCP-AUDIT-001: the real PostgreSQL migrations apply to a new database,
    /// including 20260929140000_VersionedAuditPayloads, and through AuditPayloadProviderAssertions a legacy audit row
    /// reads back its legacy text with no compressed payload while a new row round trips its previous snapshot, current
    /// snapshot, diff and metadata JSON with all four compressed payloads stored and the current snapshot payload
    /// shorter than its JSON. Data: the server of EphemeralPostgresFixture, a uniquely named database the case creates
    /// and Dispose drops, the QBrainAi.Storage.PostgreSqlMigrations assembly, and the legacy and compressed audit rows
    /// of AuditPayloadProviderAssertions.
    /// </summary>
    /// <remarks>2026-11-07, FR-MCP-AUDIT-001; TR-MCP-AUDIT-002; TEST-MCP-AUDIT-001: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the test code is unchanged.</remarks>
    [Fact]
    public async Task Migration_PersistsCompressedPayloads()
    {
        var cancellationToken = TestContext.Current.CancellationToken;
        await using (var admin = new NpgsqlConnection(_serverConnectionString))
        {
            await admin.OpenAsync(cancellationToken);
            await using var create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE \"{_databaseName}\";";
            await create.ExecuteNonQueryAsync(cancellationToken);
            _databaseCreated = true;
        }

        var connectionString = new NpgsqlConnectionStringBuilder(_serverConnectionString)
        {
            Database = _databaseName,
        }.ToString();
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseNpgsql(connectionString, pg =>
                pg.MigrationsAssembly("QBrainAi.Storage.PostgreSqlMigrations"))
            .Options;

        await using var db = new McpDbContext(options);
        await db.Database.MigrateAsync(cancellationToken);
        await AuditPayloadProviderAssertions.AssertRoundTripAsync(db, cancellationToken);
    }

    public void Dispose()
    {
        if (!_databaseCreated)
            return;
        using var admin = new NpgsqlConnection(_serverConnectionString);
        admin.Open();
        using (var terminate = admin.CreateCommand())
        {
            terminate.CommandText =
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid();";
            terminate.ExecuteNonQuery();
        }
        using var drop = admin.CreateCommand();
        drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\";";
        drop.ExecuteNonQuery();
    }
}

/// <summary>
/// The audit payload round trip that both provider migration bindings in this file assert after migrating.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-TESTRESOURCE-001 ac-5; TR-MCP-USECASE-018 AC-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: moved whole with its file from QBrainAi.Support.Mcp.Tests into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests together with the two bindings that use it (slice S8, hostile validation finding R14-F4); the class had no documentation, so this summary and entry were added; only the namespace changed.</remarks>
internal static class AuditPayloadProviderAssertions
{
    private const string MigrationId = "20260929140000_VersionedAuditPayloads";
    private const string WorkspaceId = "Q:\\__mcp_unit_test__\\AuditPayloads";

    public static async Task AssertRoundTripAsync(McpDbContext db, CancellationToken cancellationToken)
    {
        Assert.Contains(MigrationId, await db.Database.GetAppliedMigrationsAsync(cancellationToken));

        var now = DateTimeOffset.UtcNow;
        db.Workspaces.Add(new WorkspaceEntity
        {
            WorkspaceId = WorkspaceId,
            WorkspacePath = WorkspaceId,
            Name = "audit payload test",
            DateTimeCreated = now,
            DateTimeModified = now,
        });
        await db.SaveChangesAsync(cancellationToken);

        var legacy = NewAudit("legacy");
        legacy.PayloadEncodingVersion = null;
        legacy.CurrentSnapshotJsonLegacy = "{\"old\":true}";
        var compressed = NewAudit("compressed");
        compressed.PreviousSnapshotJson = "{\"before\":\"Å\"}";
        compressed.CurrentSnapshotJson = "{\"name\":\"東京\",\"data\":\"" + new string('x', 2000) + "\"}";
        compressed.DiffJson = "{\"change\":true}";
        compressed.MetadataJson = "{\"actor\":\"東京\"}";
        db.DataAuditLogs.AddRange(legacy, compressed);
        await db.SaveChangesAsync(cancellationToken);
        db.ChangeTracker.Clear();

        var oldRow = await db.DataAuditLogs.AsNoTracking().SingleAsync(
            row => row.EntityKey == "legacy", cancellationToken);
        Assert.Equal("{\"old\":true}", oldRow.CurrentSnapshotJson);
        Assert.Null(oldRow.CurrentSnapshotPayload);

        var newRow = await db.DataAuditLogs.AsNoTracking().SingleAsync(
            row => row.EntityKey == "compressed", cancellationToken);
        Assert.Equal("{\"before\":\"Å\"}", newRow.PreviousSnapshotJson);
        Assert.Equal(compressed.CurrentSnapshotJson, newRow.CurrentSnapshotJson);
        Assert.Equal("{\"change\":true}", newRow.DiffJson);
        Assert.Equal("{\"actor\":\"東京\"}", newRow.MetadataJson);
        Assert.Null(newRow.CurrentSnapshotJsonLegacy);
        Assert.NotNull(newRow.PreviousSnapshotPayload);
        Assert.NotNull(newRow.CurrentSnapshotPayload);
        Assert.NotNull(newRow.DiffPayload);
        Assert.NotNull(newRow.MetadataPayload);
        Assert.True(newRow.CurrentSnapshotPayload!.Length < newRow.CurrentSnapshotJson!.Length);
    }

    private static DataAuditLogEntity NewAudit(string key) => new()
    {
        WorkspaceId = WorkspaceId,
        EntityKind = "test",
        EntityKey = key,
        Action = "create",
        Actor = "test",
        SourceType = "test",
        OccurredAtUtc = DateTimeOffset.UtcNow,
    };
}
