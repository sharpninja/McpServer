using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-PRODUCT-005 / TR-MCP-PRODUCT-MODEL-001: Products tables appear after Migrate().
/// Phase 1 red until the AddProductsStorage migration exists.
/// </summary>
/// <remarks>2026-11-07, TEST-MCP-PRODUCT-005; TR-MCP-PRODUCT-MODEL-001; FR-MCP-BUILD-001; TR-MCP-NUKE-019 AC-7: the unit half of this class: its 1 Category=Integration case (AddProductsStorageMigration_Source_ExistsForAllProviders_AndHasNoSessionLogsTableOps) moved unchanged to the class of the same name in the integration-owned QBrainAi.Support.Mcp.EnvironmentIntegrationTests (slice S7, hostile validation finding R14-F4), so this unit-owned project holds only unit cases; the state and helpers both halves use (_connection, _options, .ctor and Dispose) moved once into the abstract base class ProductMigrationApplyTestsBase in Storage/ProductMigrationApplyTestsBase.cs beside this file, which both halves derive from and the integration project links (DRY); the unit cases and the class attributes are unchanged.</remarks>
public sealed class ProductMigrationApplyTests : ProductMigrationApplyTestsBase, IDisposable
{
    /// <summary>Full migrate from empty creates Products and ProductWorkspaceMemberships.</summary>
    [Fact]
    public void Migrate_FromEmpty_CreatesProductTables()
    {
        using var db = new McpDbContext(_options);
        db.Database.Migrate();

        Assert.True(TableExists(db, "Products"));
        Assert.True(TableExists(db, "ProductWorkspaceMemberships"));
        Assert.True(ColumnExists(db, "Products", "Key"));
        Assert.True(ColumnExists(db, "Products", "OwnerWorkspaceId"));
    }

    /// <summary>
    /// TEST-MCP-PRODUCT-005: Upgrade from the current tip with SessionLogs agent columns present
    /// still creates Products and does not drop agent columns.
    /// </summary>
    [Fact]
    public void Migrate_FromPredecessor_WithSessionLogsAgentColumns_CreatesProductTables()
    {
        using (var db = new McpDbContext(_options))
        {
            db.GetService<IMigrator>().Migrate("20260816183137_AddHandoffIngestionStorage");
            EnsureColumn(db, "SessionLogs", "AgentExecutablePath");
            EnsureColumn(db, "SessionLogs", "AgentExecutableVersion");
            EnsureColumn(db, "SessionLogs", "AgentSessionId");
            EnsureColumn(db, "SessionLogs", "AgentSessionTranscriptFile");
        }

        using (var db = new McpDbContext(_options))
        {
            db.Database.Migrate();
            Assert.True(TableExists(db, "Products"));
            Assert.True(TableExists(db, "ProductWorkspaceMemberships"));
            Assert.True(ColumnExists(db, "SessionLogs", "AgentExecutablePath"));
        }
    }

    private static void EnsureColumn(McpDbContext db, string table, string column)
    {
        if (ColumnExists(db, table, column))
            return;

        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = $"ALTER TABLE \"{table}\" ADD COLUMN \"{column}\" TEXT NULL";
        cmd.ExecuteNonQuery();
    }

    private static bool TableExists(McpDbContext db, string table)
    {
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = "SELECT 1 FROM sqlite_master WHERE type='table' AND name=$name";
        var p = cmd.CreateParameter();
        p.ParameterName = "$name";
        p.Value = table;
        cmd.Parameters.Add(p);
        return cmd.ExecuteScalar() is not null;
    }

    private static bool ColumnExists(McpDbContext db, string table, string column)
    {
        using var cmd = db.Database.GetDbConnection().CreateCommand();
        cmd.CommandText = $"PRAGMA table_info('{table}')";
        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            if (string.Equals(reader.GetString(1), column, StringComparison.Ordinal))
                return true;
        }

        return false;
    }
}
