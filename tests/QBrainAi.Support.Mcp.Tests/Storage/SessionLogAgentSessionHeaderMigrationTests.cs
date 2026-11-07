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

namespace QBrainAi.Support.Mcp.Tests.Storage;

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
/// <remarks>2026-11-07, TEST-MCP-TRIAGESCHEMA-001; TR-MCP-TRIAGESCHEMA-001; FR-MCP-TESTRESOURCE-001 ac-7; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (SqlServerUpSql_LegacySessionLogs_AddsHeaderColumnsAndTagsIdempotently and PostgreSqlUpSql_LegacySessionLogs_AddsHeaderColumnsAndTagsIdempotently) and the helpers only they use (ExecutePostgresBatch, AssertPostgresLegacyApply, PostgresTableExists, PostgresColumnExists, EnsureLocalDbRunning, ExecuteSqlServerBatch, AssertSqlServerLegacyApply, SqlServerTableExists and SqlServerColumnExists) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (HeaderColumns, _connection, _options, .ctor, Dispose, CaptureUpSql, AssertSqlServerGuardedHeaderAndTagsSql and AssertPostgreSqlGuardedHeaderAndTagsSql) moved once into the abstract base class SessionLogAgentSessionHeaderMigrationTestsBase in Storage/SessionLogAgentSessionHeaderMigrationTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
[Collection(SqlServerLocalDbCollection.Name)]
public sealed class SessionLogAgentSessionHeaderMigrationTests : SessionLogAgentSessionHeaderMigrationTestsBase, IDisposable
{
    /// <summary>Sqlite migration immediately before the header-column apply vehicle.</summary>
    public const string PrecedingMigration = "20260818142008_AddProductsStorage";

    /// <summary>Sqlite provider migration that must add AgentSession header columns.</summary>
    public const string TargetMigration = "20260818205751_AddSessionLogTagsAndAgentSessionHeaders";

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
