using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-USECASE-006 / TR-MCP-USECASE-001: Apply real EF migrations for Use Case support.
/// Proves empty-DB apply and upgrade from the migration before AddUseCaseSupport.
/// Does not use EnsureCreated as the storage gate.
/// </summary>
public sealed class UseCaseMigrationApplyTests : IDisposable
{
    /// <summary>Migration immediately before AddUseCaseSupport on Sqlite.</summary>
    private const string PrecedingMigration = "20260720170000_RenameQuadBrainRolesToCreativityLogic";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Opens an isolated in-memory Sqlite database for real Migrate().</summary>
    public UseCaseMigrationApplyTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly("McpServer.Storage.SqliteMigrations"))
            .Options;
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    /// <summary>Full migration chain from empty DB creates UseCases and related tables.</summary>
    [Fact]
    public void Migrate_FromEmpty_CreatesUseCaseTables()
    {
        using var db = new McpDbContext(_options);
        db.Database.Migrate();

        Assert.True(TableExists(db, "UseCases"));
        Assert.True(TableExists(db, "Actors"));
        Assert.True(TableExists(db, "UseCaseFrLinks"));
        Assert.True(ColumnExists(db, "UseCases", "VersionNumber"));
        Assert.True(ColumnExists(db, "UseCases", "ApprovalStatus"));
        Assert.True(ColumnExists(db, "UseCases", "ProductKey"));
        Assert.True(ColumnExists(db, "UseCases", "DiagramGraphJson"));
        Assert.True(ColumnExists(db, "UseCaseFrLinks", "FrId"));
    }

    /// <summary>
    /// Upgrade path: migrate to predecessor, ensure SessionLogs agent columns exist (production-shaped),
    /// then apply remaining migrations including AddUseCaseSupport without failing.
    /// </summary>
    [Fact]
    public void Migrate_FromPredecessor_WithSessionLogsAgentColumns_CreatesUseCaseTables()
    {
        using (var db = new McpDbContext(_options))
        {
            db.GetService<IMigrator>().Migrate(PrecedingMigration);
            EnsureColumn(db, "SessionLogs", "AgentExecutablePath");
            EnsureColumn(db, "SessionLogs", "AgentExecutableVersion");
            EnsureColumn(db, "SessionLogs", "AgentSessionId");
            EnsureColumn(db, "SessionLogs", "AgentSessionTranscriptFile");
        }

        using (var db = new McpDbContext(_options))
        {
            db.Database.Migrate();
            Assert.True(TableExists(db, "UseCases"));
            Assert.True(TableExists(db, "UseCaseFrLinks"));
            Assert.True(ColumnExists(db, "SessionLogs", "AgentExecutablePath"));
            Assert.True(ColumnExists(db, "UseCases", "VersionNumber"));
        }
    }

    /// <summary>AddUseCaseSupport migration sources must not alter SessionLogs.</summary>
    [Fact]
    public void AddUseCaseSupportMigration_Source_HasNoSessionLogsTableOps()
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
            var files = Directory.GetFiles(root, "*AddUseCaseSupport.cs")
                .Where(f => !f.Contains("Designer", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.NotEmpty(files);
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                Assert.DoesNotContain("table: \"SessionLogs\"", text, StringComparison.Ordinal);
            }
        }
    }

    private static void EnsureColumn(McpDbContext db, string table, string column)
    {
        if (ColumnExists(db, table, column))
            return;
        // Fixed identifiers only (not user input); ExecuteSqlRaw used with explicit allow-list.
        var allowedTables = new HashSet<string>(StringComparer.Ordinal) { "SessionLogs" };
        var allowedColumns = new HashSet<string>(StringComparer.Ordinal)
        {
            "AgentExecutablePath", "AgentExecutableVersion", "AgentSessionId", "AgentSessionTranscriptFile",
        };
        if (!allowedTables.Contains(table) || !allowedColumns.Contains(column))
            throw new InvalidOperationException($"Refusing to alter {table}.{column}");
#pragma warning disable EF1002 // Identifiers constrained to allow-list above
        db.Database.ExecuteSqlRaw($"ALTER TABLE {table} ADD COLUMN {column} TEXT NULL;");
#pragma warning restore EF1002
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
        return Convert.ToInt32(cmd.ExecuteScalar()) > 0;
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
