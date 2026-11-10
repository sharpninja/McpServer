using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-004 / TEST-MCP-SESSIONLOG-006:
/// Apply the real SQLite migration that adds SessionLogTurns.PlanFile and TodoId.
/// Does not use EnsureCreated as the storage gate.
/// </summary>
/// <remarks>2026-11-07, TR-MCP-SESSIONLOG-006-004; TEST-MCP-SESSIONLOG-006; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (ProviderMigrations_AddBothColumnsWithNoneDefault) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_connection, _options, .ctor and Dispose) moved once into the abstract base class SessionLogTurnPlanFileTodoIdMigrationApplyTestsBase in Storage/SessionLogTurnPlanFileTodoIdMigrationApplyTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class SessionLogTurnPlanFileTodoIdMigrationApplyTests : SessionLogTurnPlanFileTodoIdMigrationApplyTestsBase, IDisposable
{
    /// <summary>Migration immediately before AddSessionLogTurnPlanFileAndTodoId on Sqlite.</summary>
    private const string PrecedingMigration = "20260808102524_AddUseCaseDiagramGraph";

    /// <summary>Full migration chain from empty DB creates PlanFile and TodoId columns.</summary>
    [Fact]
    public void Migrate_FromEmpty_AddsPlanFileAndTodoIdColumns()
    {
        using var db = new McpDbContext(_options);
        db.Database.Migrate();

        Assert.True(ColumnExists(db, "SessionLogTurns", "PlanFile"));
        Assert.True(ColumnExists(db, "SessionLogTurns", "TodoId"));
    }

    /// <summary>Upgrade from the predecessor migration adds the two columns.</summary>
    [Fact]
    public void Migrate_FromPredecessor_AddsPlanFileAndTodoIdColumns()
    {
        using (var db = new McpDbContext(_options))
        {
            db.GetService<IMigrator>().Migrate(PrecedingMigration);
            Assert.False(ColumnExists(db, "SessionLogTurns", "PlanFile"));
            Assert.False(ColumnExists(db, "SessionLogTurns", "TodoId"));
        }

        using (var db = new McpDbContext(_options))
        {
            db.Database.Migrate();
            Assert.True(ColumnExists(db, "SessionLogTurns", "PlanFile"));
            Assert.True(ColumnExists(db, "SessionLogTurns", "TodoId"));
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
