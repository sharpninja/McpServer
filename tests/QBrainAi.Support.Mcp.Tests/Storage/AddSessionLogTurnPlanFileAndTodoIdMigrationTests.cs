using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-004 / TEST-MCP-SESSIONLOG-006:
/// Provider migrations add and drop SessionLogTurns.PlanFile and TodoId.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-SESSIONLOG-006-004; TEST-MCP-SESSIONLOG-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 2 Category=Integration cases (SqlServerMigration_UpAddsColumns_DownDropsThem and PostgreSqlMigration_UpAddsColumns_DownDropsThem) and the helpers only they use (AssertProviderMigrationContainsUpAndDown) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_connection, _options, .ctor and Dispose) moved once into the abstract base class AddSessionLogTurnPlanFileAndTodoIdMigrationTestsBase in Storage/AddSessionLogTurnPlanFileAndTodoIdMigrationTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class AddSessionLogTurnPlanFileAndTodoIdMigrationTests : AddSessionLogTurnPlanFileAndTodoIdMigrationTestsBase, IDisposable
{
    private const string Predecessor = "20260808102524_AddUseCaseDiagramGraph";
    private const string Target = "20260812173052_AddSessionLogTurnPlanFileAndTodoId";

    /// <summary>AC-TR-MCP-SESSIONLOG-006-004: SQLite Up adds both columns; Down drops them.</summary>
    [Fact]
    public void SqliteMigration_UpAddsColumns_DownDropsThem()
    {
        using (var db = new McpDbContext(_options))
        {
            db.GetService<IMigrator>().Migrate(Predecessor);
            Assert.False(ColumnExists(db, "SessionLogTurns", "PlanFile"));
            Assert.False(ColumnExists(db, "SessionLogTurns", "TodoId"));
        }

        using (var db = new McpDbContext(_options))
        {
            db.GetService<IMigrator>().Migrate(Target);
            Assert.True(ColumnExists(db, "SessionLogTurns", "PlanFile"));
            Assert.True(ColumnExists(db, "SessionLogTurns", "TodoId"));
        }

        using (var db = new McpDbContext(_options))
        {
            db.GetService<IMigrator>().Migrate(Predecessor);
            Assert.False(ColumnExists(db, "SessionLogTurns", "PlanFile"));
            Assert.False(ColumnExists(db, "SessionLogTurns", "TodoId"));
        }
    }

    private static bool ColumnExists(McpDbContext db, string table, string column)
    {
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        if (cmd.Connection!.State != System.Data.ConnectionState.Open)
            cmd.Connection.Open();
        cmd.CommandText = $"PRAGMA table_info({table})";
        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            if (string.Equals(r.GetString(1), column, StringComparison.OrdinalIgnoreCase))
                return true;
        }

        return false;
    }
}
