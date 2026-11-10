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
/// TEST-MCP-TRIAGESCHEMA-001; TR-MCP-TRIAGESCHEMA-001; FR-MCP-TESTRESOURCE-001 ac-7: the per-test state and helpers that both halves of SessionLogAgentSessionHeaderMigrationTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-TRIAGESCHEMA-001; TR-MCP-TRIAGESCHEMA-001; FR-MCP-TESTRESOURCE-001 ac-7; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from SessionLogAgentSessionHeaderMigrationTests (HeaderColumns, _connection, _options, .ctor, Dispose, CaptureUpSql, AssertSqlServerGuardedHeaderAndTagsSql and AssertPostgreSqlGuardedHeaderAndTagsSql) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class SessionLogAgentSessionHeaderMigrationTestsBase : IDisposable
{
    internal static readonly string[] HeaderColumns =
    [
        "AgentSessionId",
        "AgentSessionTranscriptFile",
        "AgentExecutablePath",
        "AgentExecutableVersion",
    ];

    internal readonly SqliteConnection _connection;
    internal readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Opens an isolated in-memory Sqlite database for real MigrateAsync.</summary>
    protected SessionLogAgentSessionHeaderMigrationTestsBase()
    {
        SessionLogSchemaGuard.ResetCache();
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly("QBrainAi.Storage.SqliteMigrations"))
            .Options;
    }

    /// <inheritdoc />
    public void Dispose()
    {
        SessionLogSchemaGuard.ResetCache();
        _connection.Dispose();
    }

    internal static string CaptureUpSql(Migration migration, string activeProvider)
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

    internal static void AssertSqlServerGuardedHeaderAndTagsSql(string sql)
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

    internal static void AssertPostgreSqlGuardedHeaderAndTagsSql(string sql)
    {
        foreach (var column in HeaderColumns)
            Assert.Contains($@"ADD COLUMN IF NOT EXISTS ""{column}""", sql, StringComparison.Ordinal);

        Assert.Contains(@"CREATE TABLE IF NOT EXISTS ""SessionLogTags""", sql, StringComparison.Ordinal);
        Assert.DoesNotContain(
            "CREATE TABLE \"SessionLogTags\"",
            sql.Replace(@"CREATE TABLE IF NOT EXISTS ""SessionLogTags""", string.Empty, StringComparison.Ordinal),
            StringComparison.Ordinal);
    }
}
