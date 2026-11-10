using System.Diagnostics;
using System.Reflection;
using QBrainAi.Support.Mcp.Models;
using QBrainAi.Support.Mcp.Notifications;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
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
using PostgreSqlHeaderMigration = QBrainAi.Support.Mcp.Storage.PostgreSqlMigrations.Migrations.AddSessionLogTagsAndAgentSessionHeaders;
using SqlServerHeaderMigration = QBrainAi.Support.Mcp.Storage.SqlServerMigrations.Migrations.AddSessionLogTagsAndAgentSessionHeaders;
using SqliteHeaderMigration = QBrainAi.Support.Mcp.Storage.SqliteMigrations.Migrations.AddSessionLogTagsAndAgentSessionHeaders;

namespace QBrainAi.Support.Mcp.EnvironmentIntegrationTests.Storage;

/// <summary>
/// TEST-MCP-TRIAGESCHEMA-001 / TR-MCP-TRIAGESCHEMA-001: Sqlite provider migration
/// <c>20260818205751_AddSessionLogTagsAndAgentSessionHeaders</c> adds the four SessionLogs
/// agent-header columns on a legacy table via <see cref="DatabaseFacade.MigrateAsync"/>,
/// without <c>ScratchSqliteSchema.RepairLegacySessionLogHeaderColumnsAsync</c>.
/// SqlServer and Postgres compiled <c>Up()</c> must emit guarded SQL (no unguarded
/// <see cref="CreateTableOperation"/>). SqlServer captured SQL is executed on disposable LocalDB.
/// Postgres captured SQL is executed on an ephemeral local cluster (no Skip).
/// Its SQL Server case opens LocalDB, so the class runs in <see cref="SqlServerLocalDbCollection"/> (FR-MCP-TESTRESOURCE-001 ac-7).
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGESCHEMA-001; TR-MCP-TRIAGESCHEMA-001; FR-MCP-TESTRESOURCE-001 ac-7; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the integration half of this class, split from QBrainAi.Support.Mcp.Tests (Storage/SessionLogAgentSessionHeaderMigrationTests.cs), where its unit cases stay in the class of the same name, into the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests, so the unit-owned project holds only unit cases (slice S7, hostile validation finding R14-F4; operator 2026-11-06: "...Environment.IntegrationTests (Recommended)"); its 2 Category=Integration cases and the helpers only they use (ExecutePostgresBatch, AssertPostgresLegacyApply, PostgresTableExists, PostgresColumnExists, EnsureLocalDbRunning, ExecuteSqlServerBatch, AssertSqlServerLegacyApply, SqlServerTableExists and SqlServerColumnExists) moved unchanged with the class attributes; the state and helpers both halves use come from the abstract base class SessionLogAgentSessionHeaderMigrationTestsBase in Storage/SessionLogAgentSessionHeaderMigrationTestsBase.cs of QBrainAi.Support.Mcp.Tests, linked into this project (DRY); only the namespace changed.</remarks>
[Collection(SqlServerLocalDbCollection.Name)]
public sealed class SessionLogAgentSessionHeaderMigrationTests : SessionLogAgentSessionHeaderMigrationTestsBase, IDisposable
{
    /// <summary>
    /// TR-MCP-TRIAGESCHEMA-001: captured SqlServer Up() SQL applies on a disposable LocalDB
    /// SessionLogs table that is missing the four header columns and SessionLogTags, then
    /// reapplies without error (idempotent). Does not run the full migration chain.
    /// Integration: opens the lane-owned SQL override or serialized LocalDB through the shared connection policy
    /// (FR-MCP-TESTRESOURCE-001 ac-8, TR-MCP-USECASE-018 AC-006).
    /// Each captured-SQL batch retains its first failure through the shared operation reporter
    /// (FR-MCP-TESTRESOURCE-001 ac-5); this path has no EF migration-lock release.
    /// </summary>
    [Fact]
    [Trait("Category", "Integration")]
    public void SqlServerUpSql_LegacySessionLogs_AddsHeaderColumnsAndTagsIdempotently()
    {
        var sql = CaptureUpSql(new SqlServerHeaderMigration(), "Microsoft.EntityFrameworkCore.SqlServer");
        AssertSqlServerGuardedHeaderAndTagsSql(sql);
        var failures = new SqlServerMigrationFailureReporter();

        if (string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(SqlServerTestConnection.OverrideVariable)))
            EnsureLocalDbRunning();
        var serverCs = SqlServerTestConnection.Server;

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

            failures.Run(() => ExecuteSqlServerBatch(db, sql));
            AssertSqlServerLegacyApply(db);

            failures.Run(() => ExecuteSqlServerBatch(db, sql));
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
    /// <remarks>Integration: applies the captured migration SQL to an actual isolated PostgreSQL database (TR-MCP-USECASE-018 AC-006).</remarks>
    [Fact]
    [Trait("Category", "Integration")]
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
}
