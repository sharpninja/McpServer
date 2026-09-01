using System.Diagnostics;
using System.Reflection;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;
using PostgreSqlHeaderMigration = McpServer.Support.Mcp.Storage.PostgreSqlMigrations.Migrations.AddSessionLogTagsAndAgentSessionHeaders;
using SqlServerHeaderMigration = McpServer.Support.Mcp.Storage.SqlServerMigrations.Migrations.AddSessionLogTagsAndAgentSessionHeaders;
using SqliteHeaderMigration = McpServer.Support.Mcp.Storage.SqliteMigrations.Migrations.AddSessionLogTagsAndAgentSessionHeaders;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-TRIAGESCHEMA-001 / TR-MCP-TRIAGESCHEMA-001: Sqlite provider migration
/// <c>20260818205751_AddSessionLogTagsAndAgentSessionHeaders</c> adds the four SessionLogs
/// agent-header columns on a legacy table via <see cref="DatabaseFacade.MigrateAsync"/>,
/// without <c>ScratchSqliteSchema.RepairLegacySessionLogHeaderColumnsAsync</c>.
/// SqlServer and Postgres compiled <c>Up()</c> must emit guarded SQL (no unguarded
/// <see cref="CreateTableOperation"/>). SqlServer captured SQL is executed on disposable LocalDB.
/// Postgres captured SQL is executed on an ephemeral local cluster (no Skip).
/// </summary>
public sealed class SessionLogAgentSessionHeaderMigrationTests : IDisposable
{
    /// <summary>Sqlite migration immediately before the header-column apply vehicle.</summary>
    public const string PrecedingMigration = "20260818142008_AddProductsStorage";

    /// <summary>Sqlite provider migration that must add AgentSession header columns.</summary>
    public const string TargetMigration = "20260818205751_AddSessionLogTagsAndAgentSessionHeaders";

    private static readonly string[] HeaderColumns =
    [
        "AgentSessionId",
        "AgentSessionTranscriptFile",
        "AgentExecutablePath",
        "AgentExecutableVersion",
    ];

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Opens an isolated in-memory Sqlite database for real MigrateAsync.</summary>
    public SessionLogAgentSessionHeaderMigrationTests()
    {
        SessionLogSchemaGuard.ResetCache();
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly("McpServer.Storage.SqliteMigrations"))
            .Options;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SessionLogSchemaGuard.ResetCache();
        _connection.Dispose();
    }

    /// <summary>
    /// TR-MCP-TRIAGESCHEMA-001: MigrateAsync on SessionLogs missing the four header columns
    /// (with or without SessionLogTags) applies 20260818205751, then sessionlog query
    /// succeeds with and without a text filter.
    /// </summary>
    /// <param name="sessionLogTagsAlreadyExist">When true, SessionLogTags exists before apply.</param>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task SqliteMigrateAsync_LegacySessionLogsMissingHeaderColumns_AddsColumnsAndQuerySucceeds(
        bool sessionLogTagsAlreadyExist)
    {
        const string workspace = @"E:\tests\triageschema-legacy-sqlite";

        using (var db = new McpDbContext(_options, new WorkspaceContext { WorkspacePath = workspace }))
        {
            await db.GetService<IMigrator>().MigrateAsync(PrecedingMigration, TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.True(TableExists(db, "SessionLogs"));
            foreach (var column in HeaderColumns)
                Assert.False(ColumnExists(db, "SessionLogs", column), $"{column} must be missing on the legacy table.");
            Assert.False(TableExists(db, "SessionLogTags"));

            if (sessionLogTagsAlreadyExist)
                CreateLegacySessionLogTags(db);
        }

        using (var db = new McpDbContext(_options, new WorkspaceContext { WorkspacePath = workspace }))
        {
            await db.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var applied = await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Contains(applied, name => name == TargetMigration);
            foreach (var column in HeaderColumns)
                Assert.True(ColumnExists(db, "SessionLogs", column), $"{column} must be added by {TargetMigration}.");
            Assert.True(TableExists(db, "SessionLogTags"));

            db.OverrideWorkspaceId(workspace);
            SessionLogSchemaGuard.ResetCache();
            Assert.True(SessionLogSchemaGuard.Probe(db));

            var sut = new SessionLogService(
                db,
                NullLogger<SessionLogService>.Instance,
                Substitute.For<IChangeEventBus>(),
                new WorkspaceContext { WorkspacePath = workspace });
            var unfiltered = await sut.QueryAsync(
                new SessionLogQueryRequest { Limit = 1 },
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.NotNull(unfiltered);

            var filtered = await sut.QueryAsync(
                new SessionLogQueryRequest { Limit = 1, Text = "does-not-match" },
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.NotNull(filtered);
        }
    }

    /// <summary>
    /// TR-MCP-TRIAGESCHEMA-001: MigrateAsync succeeds when SessionLogs already has the four
    /// header columns and SessionLogTags already exists (hotfix / RepairLegacy shape).
    /// </summary>
    [Fact]
    public async Task SqliteMigrateAsync_SessionLogsAlreadyHasHeaderColumnsAndTags_Succeeds()
    {
        const string workspace = @"E:\tests\triageschema-legacy-sqlite-present";

        using (var db = new McpDbContext(_options, new WorkspaceContext { WorkspacePath = workspace }))
        {
            await db.GetService<IMigrator>().MigrateAsync(PrecedingMigration, TestContext.Current.CancellationToken).ConfigureAwait(true);
            foreach (var column in HeaderColumns)
                AddHeaderColumn(db, column);
            CreateLegacySessionLogTags(db);
            foreach (var column in HeaderColumns)
                Assert.True(ColumnExists(db, "SessionLogs", column));
            Assert.True(TableExists(db, "SessionLogTags"));
        }

        using (var db = new McpDbContext(_options, new WorkspaceContext { WorkspacePath = workspace }))
        {
            await db.Database.MigrateAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            var applied = await db.Database.GetAppliedMigrationsAsync(TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.Contains(applied, name => name == TargetMigration);
            foreach (var column in HeaderColumns)
                Assert.True(ColumnExists(db, "SessionLogs", column));

            db.OverrideWorkspaceId(workspace);
            SessionLogSchemaGuard.ResetCache();
            var sut = new SessionLogService(
                db,
                NullLogger<SessionLogService>.Instance,
                Substitute.For<IChangeEventBus>(),
                new WorkspaceContext { WorkspacePath = workspace });
            var result = await sut.QueryAsync(
                new SessionLogQueryRequest { Limit = 1 },
                TestContext.Current.CancellationToken).ConfigureAwait(true);
            Assert.NotNull(result);
        }
    }

    /// <summary>Sqlite 20260818205751 compiled Up() probes pragma_table_info and skips ADD when the column exists.</summary>
    [Fact]
    public void SqliteUp_MigrationBuilderSqlGuardsHeaderColumnsAndSessionLogTagsCreate()
    {
        var sql = CaptureUpSql(new SqliteHeaderMigration(), "Microsoft.EntityFrameworkCore.Sqlite");
        Assert.Contains("pragma_table_info", sql, StringComparison.OrdinalIgnoreCase);
        foreach (var column in HeaderColumns)
        {
            Assert.Contains($"mcp_add_sessionlog_text_column_if_missing('{column}')", sql, StringComparison.Ordinal);
            Assert.Contains($"name = '{column}'", sql, StringComparison.Ordinal);
        }

        Assert.Contains(@"CREATE TABLE IF NOT EXISTS ""SessionLogTags""", sql, StringComparison.Ordinal);
    }

    /// <summary>
    /// SqlServer 20260818205807 compiled Up() guards each header column with COL_LENGTH and does not
    /// use unguarded CreateTable for SessionLogTags.
    /// </summary>
    [Fact]
    public void SqlServerUp_MigrationBuilderSqlGuardsHeaderColumnsAndSessionLogTagsCreate()
    {
        var sql = CaptureUpSql(new SqlServerHeaderMigration(), "Microsoft.EntityFrameworkCore.SqlServer");
        AssertSqlServerGuardedHeaderAndTagsSql(sql);
    }

    /// <summary>
    /// Postgres 20260818205822 compiled Up() uses ADD COLUMN IF NOT EXISTS for each header column
    /// and CREATE TABLE IF NOT EXISTS for SessionLogTags.
    /// </summary>
    [Fact]
    public void PostgreSqlUp_MigrationBuilderSqlGuardsHeaderColumnsAndSessionLogTagsCreate()
    {
        var sql = CaptureUpSql(new PostgreSqlHeaderMigration(), "Npgsql.EntityFrameworkCore.PostgreSQL");
        AssertPostgreSqlGuardedHeaderAndTagsSql(sql);
    }

    /// <summary>
    /// TR-MCP-TRIAGESCHEMA-001: captured SqlServer Up() SQL applies on a disposable LocalDB
    /// SessionLogs table that is missing the four header columns and SessionLogTags, then
    /// reapplies without error (idempotent). Does not run the full migration chain.
    /// </summary>
    [Fact]
    public void SqlServerUpSql_LegacySessionLogs_AddsHeaderColumnsAndTagsIdempotently()
    {
        var sql = CaptureUpSql(new SqlServerHeaderMigration(), "Microsoft.EntityFrameworkCore.SqlServer");
        AssertSqlServerGuardedHeaderAndTagsSql(sql);

        var serverCs = Environment.GetEnvironmentVariable("MCP_TEST_SQLSERVER_CONNECTION");
        if (string.IsNullOrWhiteSpace(serverCs))
        {
            EnsureLocalDbRunning();
            serverCs = "Server=(localdb)\\MSSQLLocalDB;Integrated Security=True;TrustServerCertificate=True;Connect Timeout=8";
        }

        var databaseName = $"mcp_triageschema_{Guid.NewGuid():N}";
        using var admin = new SqlConnection(serverCs);
        try
        {
            admin.Open();
        }
        catch (SqlException ex)
        {
            throw new InvalidOperationException(
                "SQL Server is not reachable for the SessionLogs header-column apply gate. " +
                "Run InstallTestDependencies to provision LocalDB, or set MCP_TEST_SQLSERVER_CONNECTION. " +
                $"({ex.Message})",
                ex);
        }

        using (var create = admin.CreateCommand())
        {
            create.CommandTimeout = 15;
            create.CommandText = $"CREATE DATABASE [{databaseName}];";
            create.ExecuteNonQuery();
        }

        try
        {
            var dbCs = new SqlConnectionStringBuilder(serverCs)
            {
                InitialCatalog = databaseName,
                ConnectTimeout = 8,
            }.ToString();
            using var db = new SqlConnection(dbCs);
            db.Open();
            using (var setup = db.CreateCommand())
            {
                setup.CommandTimeout = 15;
                setup.CommandText = """
                    CREATE TABLE [Workspaces] (
                        [WorkspaceId] nvarchar(1024) NOT NULL,
                        CONSTRAINT [PK_Workspaces] PRIMARY KEY ([WorkspaceId])
                    );
                    CREATE TABLE [SessionLogs] (
                        [Id] bigint NOT NULL IDENTITY,
                        [WorkspaceId] nvarchar(1024) NOT NULL,
                        CONSTRAINT [PK_SessionLogs] PRIMARY KEY ([Id])
                    );
                    """;
                setup.ExecuteNonQuery();
            }

            ExecuteSqlServerBatch(db, sql);
            AssertSqlServerLegacyApply(db);

            ExecuteSqlServerBatch(db, sql);
            AssertSqlServerLegacyApply(db);
        }
        finally
        {
            try
            {
                using var drop = admin.CreateCommand();
                drop.CommandTimeout = 15;
                drop.CommandText =
                    $"IF DB_ID('{databaseName}') IS NOT NULL BEGIN ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{databaseName}]; END";
                drop.ExecuteNonQuery();
            }
            catch (SqlException)
            {
                // Best-effort cleanup; scratch databases are uniquely named.
            }
        }
    }

    /// <summary>
    /// TR-MCP-TRIAGESCHEMA-001: captured Postgres Up() SQL applies on a disposable local
    /// SessionLogs table that is missing the four header columns and SessionLogTags, then
    /// reapplies without error (idempotent). Uses <see cref="EphemeralPostgresFixture"/>
    /// (Program Files PostgreSQL or MCP_TEST_POSTGRES_CONNECTION). Does not Skip.
    /// </summary>
    [Fact]
    public void PostgreSqlUpSql_LegacySessionLogs_AddsHeaderColumnsAndTagsIdempotently()
    {
        var sql = CaptureUpSql(new PostgreSqlHeaderMigration(), "Npgsql.EntityFrameworkCore.PostgreSQL");
        AssertPostgreSqlGuardedHeaderAndTagsSql(sql);

        using var fixture = new EphemeralPostgresFixture();
        var databaseName = $"mcp_triageschema_{Guid.NewGuid():N}";
        using var admin = new NpgsqlConnection(fixture.ServerConnectionString);
        try
        {
            admin.Open();
        }
        catch (NpgsqlException ex)
        {
            throw new InvalidOperationException(
                "PostgreSQL is not reachable for the SessionLogs header-column apply gate. " +
                "Run InstallTestDependencies or set MCP_TEST_POSTGRES_CONNECTION. " +
                $"({ex.Message})",
                ex);
        }

        using (var create = admin.CreateCommand())
        {
            create.CommandTimeout = 30;
            create.CommandText = $"CREATE DATABASE \"{databaseName}\";";
            create.ExecuteNonQuery();
        }

        try
        {
            var dbCs = new NpgsqlConnectionStringBuilder(fixture.ServerConnectionString)
            {
                Database = databaseName,
                Timeout = 8,
            }.ToString();
            using var db = new NpgsqlConnection(dbCs);
            db.Open();
            using (var setup = db.CreateCommand())
            {
                setup.CommandTimeout = 15;
                setup.CommandText = """
                    CREATE TABLE "Workspaces" (
                        "WorkspaceId" character varying(1024) NOT NULL,
                        CONSTRAINT "PK_Workspaces" PRIMARY KEY ("WorkspaceId")
                    );
                    CREATE TABLE "SessionLogs" (
                        "Id" bigint GENERATED BY DEFAULT AS IDENTITY,
                        "WorkspaceId" character varying(1024) NOT NULL,
                        CONSTRAINT "PK_SessionLogs" PRIMARY KEY ("Id")
                    );
                    """;
                setup.ExecuteNonQuery();
            }

            ExecutePostgresBatch(db, sql);
            AssertPostgresLegacyApply(db);

            ExecutePostgresBatch(db, sql);
            AssertPostgresLegacyApply(db);
        }
        finally
        {
            try
            {
                using var terminate = admin.CreateCommand();
                terminate.CommandTimeout = 15;
                terminate.CommandText =
                    $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{databaseName}' AND pid <> pg_backend_pid();";
                terminate.ExecuteNonQuery();
                using var drop = admin.CreateCommand();
                drop.CommandTimeout = 15;
                drop.CommandText = $"DROP DATABASE IF EXISTS \"{databaseName}\";";
                drop.ExecuteNonQuery();
            }
            catch (NpgsqlException)
            {
                // Best-effort cleanup; scratch databases are uniquely named.
            }
        }
    }

    private static string CaptureUpSql(Migration migration, string activeProvider)
    {
        ArgumentNullException.ThrowIfNull(migration);
        var builder = new MigrationBuilder(activeProvider);
        var up = migration.GetType().GetMethod(
            "Up",
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? throw new InvalidOperationException($"Up() missing on {migration.GetType().FullName}");
        try
        {
            up.Invoke(migration, new object[] { builder });
        }
        catch (TargetInvocationException ex) when (ex.InnerException is not null)
        {
            throw ex.InnerException;
        }

        Assert.DoesNotContain(builder.Operations, static o => o is CreateTableOperation);
        Assert.DoesNotContain(builder.Operations, static o => o is AddColumnOperation);
        var sqlOps = builder.Operations.OfType<SqlOperation>().ToList();
        Assert.NotEmpty(sqlOps);
        return string.Join(Environment.NewLine, sqlOps.Select(static o => o.Sql));
    }

    private static void AssertSqlServerGuardedHeaderAndTagsSql(string sql)
    {
        foreach (var column in HeaderColumns)
        {
            Assert.Contains($"COL_LENGTH(N'SessionLogs', N'{column}') IS NULL", sql, StringComparison.Ordinal);
            Assert.Contains($"ALTER TABLE [SessionLogs] ADD [{column}]", sql, StringComparison.Ordinal);
        }

        Assert.Contains("OBJECT_ID(N'SessionLogTags'", sql, StringComparison.Ordinal);
        Assert.Contains("CREATE TABLE [SessionLogTags]", sql, StringComparison.Ordinal);
        Assert.Contains("IF OBJECT_ID(N'SessionLogTags', N'U') IS NULL", sql, StringComparison.Ordinal);
    }

    private static void AssertPostgreSqlGuardedHeaderAndTagsSql(string sql)
    {
        foreach (var column in HeaderColumns)
            Assert.Contains($@"ADD COLUMN IF NOT EXISTS ""{column}""", sql, StringComparison.Ordinal);

        Assert.Contains(@"CREATE TABLE IF NOT EXISTS ""SessionLogTags""", sql, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "CREATE TABLE \"SessionLogTags\"",
            sql.Replace(@"CREATE TABLE IF NOT EXISTS ""SessionLogTags""", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal);
    }

    private static void ExecutePostgresBatch(NpgsqlConnection db, string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandTimeout = 15;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static void AssertPostgresLegacyApply(NpgsqlConnection db)
    {
        foreach (var column in HeaderColumns)
            Assert.True(PostgresColumnExists(db, "SessionLogs", column), $"{column} must exist after Postgres Up() SQL.");
        Assert.True(PostgresTableExists(db, "SessionLogTags"), "SessionLogTags must exist after Postgres Up() SQL.");
    }

    private static bool PostgresTableExists(NpgsqlConnection db, string table)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandTimeout = 8;
        cmd.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.tables
                WHERE table_schema = 'public' AND table_name = @n
            )
            """;
        cmd.Parameters.AddWithValue("n", table);
        return Convert.ToBoolean(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static bool PostgresColumnExists(NpgsqlConnection db, string table, string column)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandTimeout = 8;
        cmd.CommandText = """
            SELECT EXISTS (
                SELECT 1
                FROM information_schema.columns
                WHERE table_schema = 'public' AND table_name = @t AND column_name = @c
            )
            """;
        cmd.Parameters.AddWithValue("t", table);
        cmd.Parameters.AddWithValue("c", column);
        return Convert.ToBoolean(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
    }

    private static void EnsureLocalDbRunning()
    {
        using var process = Process.Start(new ProcessStartInfo
        {
            FileName = "sqllocaldb",
            Arguments = "start MSSQLLocalDB",
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            CreateNoWindow = true,
        });
        if (process is null)
            throw new InvalidOperationException("sqllocaldb start failed to launch.");
        if (!process.WaitForExit(15000))
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch (InvalidOperationException)
            {
            }

            throw new InvalidOperationException("sqllocaldb start MSSQLLocalDB timed out after 15s.");
        }

        if (process.ExitCode != 0)
        {
            var stderr = process.StandardError.ReadToEnd();
            throw new InvalidOperationException($"sqllocaldb start MSSQLLocalDB exited {process.ExitCode}: {stderr}");
        }
    }

    private static void ExecuteSqlServerBatch(SqlConnection db, string sql)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandTimeout = 15;
        cmd.CommandText = sql;
        cmd.ExecuteNonQuery();
    }

    private static void AssertSqlServerLegacyApply(SqlConnection db)
    {
        foreach (var column in HeaderColumns)
            Assert.True(SqlServerColumnExists(db, "SessionLogs", column), $"{column} must exist after SqlServer Up() SQL.");
        Assert.True(SqlServerTableExists(db, "SessionLogTags"), "SessionLogTags must exist after SqlServer Up() SQL.");
    }

    private static bool SqlServerTableExists(SqlConnection db, string table)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandTimeout = 8;
        cmd.CommandText = "SELECT CASE WHEN OBJECT_ID(@n, N'U') IS NULL THEN 0 ELSE 1 END";
        cmd.Parameters.AddWithValue("@n", table);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) == 1;
    }

    private static bool SqlServerColumnExists(SqlConnection db, string table, string column)
    {
        using var cmd = db.CreateCommand();
        cmd.CommandTimeout = 8;
        cmd.CommandText = "SELECT CASE WHEN COL_LENGTH(@t, @c) IS NULL THEN 0 ELSE 1 END";
        cmd.Parameters.AddWithValue("@t", table);
        cmd.Parameters.AddWithValue("@c", column);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) == 1;
    }

    private static void AddHeaderColumn(McpDbContext db, string column)
    {
        var allowed = new HashSet<string>(StringComparer.Ordinal)
        {
            "AgentSessionId", "AgentSessionTranscriptFile", "AgentExecutablePath", "AgentExecutableVersion",
        };
        if (!allowed.Contains(column))
            throw new InvalidOperationException($"Refusing to alter SessionLogs.{column}");
#pragma warning disable EF1002
        db.Database.ExecuteSqlRaw($"ALTER TABLE SessionLogs ADD COLUMN {column} TEXT NULL;");
#pragma warning restore EF1002
    }

    private static void CreateLegacySessionLogTags(McpDbContext db)
    {
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        if (cmd.Connection!.State != System.Data.ConnectionState.Open)
            cmd.Connection.Open();
        cmd.CommandText = """
            CREATE TABLE SessionLogTags (
                Id INTEGER NOT NULL CONSTRAINT PK_SessionLogTags PRIMARY KEY AUTOINCREMENT,
                WorkspaceId TEXT NOT NULL,
                SessionLogId INTEGER NOT NULL,
                Tag TEXT NOT NULL,
                DeleteReason TEXT NULL,
                DeletedAtUtc TEXT NULL,
                DeletedBy TEXT NULL,
                IsDeleted INTEGER NOT NULL DEFAULT 0
            );
            CREATE UNIQUE INDEX IX_SessionLogTags_SessionLogId_Tag ON SessionLogTags (SessionLogId, Tag);
            CREATE INDEX IX_SessionLogTags_WorkspaceId ON SessionLogTags (WorkspaceId);
            """;
        cmd.ExecuteNonQuery();
    }

    private static bool TableExists(McpDbContext db, string table)
    {
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        if (cmd.Connection!.State != System.Data.ConnectionState.Open)
            cmd.Connection.Open();
        cmd.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type='table' AND name=$n";
        var p = cmd.CreateParameter();
        p.ParameterName = "$n";
        p.Value = table;
        cmd.Parameters.Add(p);
        return Convert.ToInt32(cmd.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture) > 0;
    }

    private static bool ColumnExists(McpDbContext db, string table, string column)
    {
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        if (cmd.Connection!.State != System.Data.ConnectionState.Open)
            cmd.Connection.Open();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
