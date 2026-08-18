using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-PRODUCT-005 / TR-MCP-PRODUCT-MODEL-001: Products tables appear after Migrate().
/// Phase 1 red until the AddProductsStorage migration exists.
/// </summary>
public sealed class ProductMigrationApplyTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Opens an isolated in-memory Sqlite database for real Migrate().</summary>
    public ProductMigrationApplyTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly("McpServer.Storage.SqliteMigrations"))
            .Options;
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

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

    /// <summary>
    /// TEST-MCP-PRODUCT-005: AddProductsStorage migration sources exist for every provider
    /// and do not alter SessionLogs.
    /// </summary>
    [Fact]
    public void AddProductsStorageMigration_Source_ExistsForAllProviders_AndHasNoSessionLogsTableOps()
    {
        var repoSrc = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src"));
        var roots = new[]
        {
            Path.Combine(repoSrc, "McpServer.Storage.SqliteMigrations", "Migrations"),
            Path.Combine(repoSrc, "McpServer.Storage.SqlServerMigrations", "Migrations"),
            Path.Combine(repoSrc, "McpServer.Storage.PostgreSqlMigrations", "Migrations"),
        };

        foreach (var root in roots)
        {
            Assert.True(Directory.Exists(root), $"Missing migrations root {root}");
            var files = Directory.GetFiles(root, "*AddProductsStorage.cs")
                .Where(f => !f.Contains("Designer", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.NotEmpty(files);
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain("SessionLogs", text, StringComparison.Ordinal);
            }
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
