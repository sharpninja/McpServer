using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-TRIAGE-004: validates that removing obsolete TODO anchors preserves
/// triage-created TODO visibility by backfilling canonical TODO items.
/// </summary>
public sealed class RemoveTodoRecordsMigrationTests : IDisposable
{
    private const string PreviousMigration = "20260628102336_AddTriageRunAgentStreams";
    private const string WorkspacePath = "F:\\GitHub\\McpServer";
    private const string StaleAnchorWorkspacePath = "C:\\ProgramData\\McpServer";
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Creates an isolated SQLite database using the real provider migration assembly.</summary>
    public RemoveTodoRecordsMigrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly("McpServer.Storage.SqliteMigrations"))
            .Options;
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-004: anchor-only BUG TODOs are converted into canonical
    /// TodoItems before the TodoRecords table is dropped.
    /// </summary>
    [Fact]
    public void Migrate_RemoveTodoRecords_BackfillsAnchorOnlyTriageTodos()
    {
        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PreviousMigration);
            SeedAnchorOnlyTriageTodo(db);
        }

        using (var db = CreateContext())
        {
            db.Database.Migrate();

            var todo = db.TodoItems
                .IgnoreQueryFilters()
                .Single(item => item.WorkspaceId == WorkspacePath && item.Id == "BUG-TRIAGE-123");

            Assert.Equal("Backfilled triage bug", todo.Title);
            Assert.Equal("Backlog", todo.Section);
            Assert.Equal("medium", todo.Priority);
            Assert.False(todo.Done);
            Assert.Empty(db.TodoItems
                .IgnoreQueryFilters()
                .Where(item => item.WorkspaceId == StaleAnchorWorkspacePath && item.Id == "BUG-TRIAGE-123"));
            Assert.Equal(0, CountTables("TodoRecords"));
        }
    }

    /// <summary>
    /// TEST-MCP-TRIAGE-004: already-upgraded databases with BUG TODOs in a stale
    /// service workspace get a non-destructive canonical copy in the triage workspace.
    /// </summary>
    [Fact]
    public void Migrate_RepairTriageCreatedTodoWorkspace_CopiesWrongWorkspaceTodoToTriageWorkspace()
    {
        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate("20260628122208_RemoveTodoRecords");
            SeedWrongWorkspaceTriageTodo(db);
        }

        using (var db = CreateContext())
        {
            db.Database.Migrate();

            var todo = db.TodoItems
                .IgnoreQueryFilters()
                .Single(item => item.WorkspaceId == WorkspacePath && item.Id == "BUG-TRIAGE-124");

            Assert.Equal("Existing stale workspace title", todo.Title);
            Assert.Equal("Backlog", todo.Section);
            Assert.Equal("high", todo.Priority);
            Assert.False(todo.Done);
            Assert.True(db.TodoItems
                .IgnoreQueryFilters()
                .Any(item => item.WorkspaceId == StaleAnchorWorkspacePath && item.Id == "BUG-TRIAGE-124"));
        }
    }

    /// <summary>Releases the in-memory database connection.</summary>
    public void Dispose()
    {
        _connection.Dispose();
    }

    private McpDbContext CreateContext() => new(_options);

    private void SeedAnchorOnlyTriageTodo(McpDbContext db)
    {
        var now = new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.Zero);
        SeedWorkspace(db, StaleAnchorWorkspacePath, "Deployed service workspace", now);
        SeedWorkspace(db, WorkspacePath, "McpServer", now);

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "TriageGroups" (
                "GroupId",
                "WorkspaceId",
                "GroupKey",
                "EffectiveWorkspacePath",
                "Title",
                "Summary",
                "Status",
                "ReportCount",
                "FirstReportAtUtc",
                "LastReportAtUtc",
                "QuietDeadlineUtc",
                "IsMcpServerRelated",
                "CreatedTodoId",
                "IsDeleted"
            )
            VALUES ({0}, {1}, {2}, {1}, {3}, {4}, {5}, 1, {6}, {6}, {6}, 1, {7}, 0);
            """,
            "triage-group-backfill",
            WorkspacePath,
            "triage-key-backfill",
            "Backfilled triage bug",
            "Backfill the stale anchor into TodoItems.",
            "completed",
            now,
            "BUG-TRIAGE-123");

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "TodoRecords" (
                "WorkspaceId",
                "TodoId",
                "CreatedAtUtc",
                "UpdatedAtUtc",
                "IsDeleted"
            )
            VALUES ({0}, {1}, {2}, {2}, 0);
            """,
            StaleAnchorWorkspacePath,
            "BUG-TRIAGE-123",
            now);
    }

    private static void SeedWrongWorkspaceTriageTodo(McpDbContext db)
    {
        var now = new DateTimeOffset(2026, 6, 28, 12, 30, 0, TimeSpan.Zero);
        SeedWorkspace(db, StaleAnchorWorkspacePath, "Deployed service workspace", now);
        SeedWorkspace(db, WorkspacePath, "McpServer", now);

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "TriageGroups" (
                "GroupId",
                "WorkspaceId",
                "GroupKey",
                "EffectiveWorkspacePath",
                "Title",
                "Summary",
                "Status",
                "ReportCount",
                "FirstReportAtUtc",
                "LastReportAtUtc",
                "QuietDeadlineUtc",
                "IsMcpServerRelated",
                "CreatedTodoId",
                "IsDeleted"
            )
            VALUES ({0}, {1}, {2}, {1}, {3}, {4}, {5}, 1, {6}, {6}, {6}, 1, {7}, 0);
            """,
            "triage-group-repair",
            WorkspacePath,
            "triage-key-repair",
            "Repair triage bug",
            "Repair wrong-workspace TODO copy.",
            "completed",
            now,
            "BUG-TRIAGE-124");

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "TodoItems" (
                "WorkspaceId",
                "Id",
                "Title",
                "Section",
                "Priority",
                "Done",
                "ItemKind",
                "SectionOrder",
                "ItemOrder",
                "IsDeleted"
            )
            VALUES ({0}, {1}, {2}, {3}, {4}, 0, {5}, 0, 0, 0);
            """,
            StaleAnchorWorkspacePath,
            "BUG-TRIAGE-124",
            "Existing stale workspace title",
            "Backlog",
            "high",
            "standard");
    }

    private static void SeedWorkspace(McpDbContext db, string workspacePath, string name, DateTimeOffset now)
    {
        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "Workspaces" (
                "WorkspaceId",
                "WorkspacePath",
                "Name",
                "TodoPath",
                "IsPrimary",
                "IsEnabled",
                "DateTimeCreated",
                "DateTimeModified",
                "CurrentRequirementLayerKey",
                "IsDeleted"
            )
            VALUES ({0}, {0}, {1}, {2}, 1, 1, {3}, {3}, {4}, 0);
            """,
            workspacePath,
            name,
            "docs/Project/TODO.yaml",
            now,
            "layer-1");
    }

    private int CountTables(string tableName)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
