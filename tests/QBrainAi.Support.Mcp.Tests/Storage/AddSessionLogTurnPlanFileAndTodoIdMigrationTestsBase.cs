using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TR-MCP-SESSIONLOG-006-004; TEST-MCP-SESSIONLOG-006: the per-test state and helpers that both halves of AddSessionLogTurnPlanFileAndTodoIdMigrationTests use (the unit half in QBrainAi.Support.Mcp.Tests, the integration half in QBrainAi.Support.Mcp.EnvironmentIntegrationTests), defined once in a base class both halves derive from and compiled into both projects.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-SESSIONLOG-006-004; TEST-MCP-SESSIONLOG-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: extracted unchanged from AddSessionLogTurnPlanFileAndTodoIdMigrationTests (_connection, _options, .ctor and Dispose) when that class split into its unit half and its integration half (slice S7, hostile validation finding R14-F4), so both halves use one definition instead of a copy (DRY); only the accessibility changed, from private to internal, and the constructor takes the base class name and protected accessibility; xUnit still creates one instance of a half per case, so each case gets fresh state as before.</remarks>
public abstract class AddSessionLogTurnPlanFileAndTodoIdMigrationTestsBase : IDisposable
{
    internal readonly SqliteConnection _connection;
    internal readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Opens an isolated in-memory Sqlite database for real Migrate().</summary>
    protected AddSessionLogTurnPlanFileAndTodoIdMigrationTestsBase()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly("QBrainAi.Storage.SqliteMigrations"))
            .Options;
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();
}
