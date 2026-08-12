using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// AC-TR-MCP-SESSIONLOG-006-004 / TEST-MCP-SESSIONLOG-006:
/// Provider migrations add and drop SessionLogTurns.PlanFile and TodoId.
/// </summary>
public sealed class AddSessionLogTurnPlanFileAndTodoIdMigrationTests : IDisposable
{
    private const string Predecessor = "20260808102524_AddUseCaseDiagramGraph";
    private const string Target = "20260812173052_AddSessionLogTurnPlanFileAndTodoId";

    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Opens an isolated in-memory Sqlite database for real Migrate().</summary>
    public AddSessionLogTurnPlanFileAndTodoIdMigrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly("McpServer.Storage.SqliteMigrations"))
            .Options;
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

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

    /// <summary>AC-TR-MCP-SESSIONLOG-006-004: SQL Server migration source adds both columns with None default.</summary>
    [Fact]
    public void SqlServerMigration_UpAddsColumns_DownDropsThem()
    {
        AssertProviderMigrationContainsUpAndDown("McpServer.Storage.SqlServerMigrations");
    }

    /// <summary>AC-TR-MCP-SESSIONLOG-006-004: PostgreSQL migration source adds both columns with None default.</summary>
    [Fact]
    public void PostgreSqlMigration_UpAddsColumns_DownDropsThem()
    {
        AssertProviderMigrationContainsUpAndDown("McpServer.Storage.PostgreSqlMigrations");
    }

    private static void AssertProviderMigrationContainsUpAndDown(string project)
    {
        var repoSrc = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "..", "src"));
        var root = Path.Combine(repoSrc, project, "Migrations");
        var files = Directory.GetFiles(root, "*AddSessionLogTurnPlanFileAndTodoId.cs")
            .Where(f => !f.Contains("Designer", StringComparison.OrdinalIgnoreCase))
            .ToArray();
        Assert.NotEmpty(files);
        var text = File.ReadAllText(files[0]);
        Assert.Contains("name: \"PlanFile\"", text, StringComparison.Ordinal);
        Assert.Contains("name: \"TodoId\"", text, StringComparison.Ordinal);
        Assert.Contains("defaultValue: \"None\"", text, StringComparison.Ordinal);
        Assert.Contains("DropColumn", text, StringComparison.Ordinal);
        Assert.Contains("DropIndex", text, StringComparison.Ordinal);
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
