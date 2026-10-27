using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Storage;

[Trait("Category", "Integration")]
public sealed class SqlServerAuditPayloadMigrationTests : IDisposable
{
    private readonly string _serverConnectionString =
        Environment.GetEnvironmentVariable("MCP_TEST_SQLSERVER_CONNECTION")
        ?? "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True";
    private readonly string _databaseName = $"mcp_audit_{Guid.NewGuid():N}";
    private bool _databaseCreated;

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
            .Options;

        await using (var db = new McpDbContext(options))
        {
            await db.Database.MigrateAsync(cancellationToken);
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

[Trait("Category", "Integration")]
public sealed class PostgreSqlAuditPayloadMigrationTests : IClassFixture<EphemeralPostgresFixture>, IDisposable
{
    private readonly string _serverConnectionString;
    private readonly string _databaseName = $"mcp_audit_{Guid.NewGuid():N}";
    private bool _databaseCreated;

    public PostgreSqlAuditPayloadMigrationTests(EphemeralPostgresFixture fixture)
        => _serverConnectionString = fixture.ServerConnectionString;

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