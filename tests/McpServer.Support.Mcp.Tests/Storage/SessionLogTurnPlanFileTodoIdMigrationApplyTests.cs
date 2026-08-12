using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-004 / TEST-MCP-SESSIONLOG-006:
/// Apply the real SQLite migration that adds SessionLogTurns.PlanFile and TodoId.
/// Does not use EnsureCreated as the storage gate.
/// </summary>
public sealed class SessionLogTurnPlanFileTodoIdMigrationApplyTests : IDisposable
{
    /// <summary>Migration immediately before AddSessionLogTurnPlanFileAndTodoId on Sqlite.</summary>
    private const string PrecedingMigration = "20260808102524_AddUseCaseDiagramGraph";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Opens an isolated in-memory Sqlite database for real Migrate().</summary>
    public SessionLogTurnPlanFileTodoIdMigrationApplyTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly("McpServer.Storage.SqliteMigrations"))
            .Options;
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

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

    /// <summary>All three provider migration sources add both columns with default None.</summary>
    [Fact]
    public void ProviderMigrations_AddBothColumnsWithNoneDefault()
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
            var files = Directory.GetFiles(root, "*AddSessionLogTurnPlanFileAndTodoId.cs")
                .Where(f => !f.Contains("Designer", StringComparison.OrdinalIgnoreCase))
                .ToArray();
            Assert.NotEmpty(files);
            foreach (var file in files)
            {
                var text = File.ReadAllText(file);
                Assert.Contains("name: \"PlanFile\"", text, StringComparison.Ordinal);
                Assert.Contains("name: \"TodoId\"", text, StringComparison.Ordinal);
                Assert.Contains("defaultValue: \"None\"", text, StringComparison.Ordinal);
            }
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
