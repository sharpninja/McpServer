using QBrainAi.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace QBrainAi.Support.Mcp.Tests.Storage;

/// <summary>
/// TEST-MCP-TRIAGE-004: validates that removing obsolete TODO anchors preserves
/// triage-created TODO visibility by backfilling canonical TODO items.
/// </summary>
public sealed class RemoveTodoRecordsMigrationTests : IDisposable
{
    private const string PreviousMigration = "20260628102336_AddTriageRunAgentStreams";
    private const string WorkspacePath = "Q:\\__mcp_unit_test__\\QBrainAi";
    private const string StaleAnchorWorkspacePath = "C:\\ProgramData\\QBrainAi";
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Creates an isolated SQLite database using the real provider migration assembly.</summary>
    public RemoveTodoRecordsMigrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly("QBrainAi.Storage.SqliteMigrations"))
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

            HistoricalTodoMigrationContract.AssertAnchor(ReadRawTodoRows(), CountTables("TodoRecords"));
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

            HistoricalTodoMigrationContract.AssertRepair(ReadRawTodoRows());
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
        SeedWorkspace(db, WorkspacePath, "QBrainAi", now);

        // Historical schema: the column keeps its 1.x name until 20261005170000_RenameTriageIsMcpServerRelatedColumn.
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
        SeedWorkspace(db, WorkspacePath, "QBrainAi", now);

        // Historical schema: the column keeps its 1.x name until 20261005170000_RenameTriageIsMcpServerRelatedColumn.
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

    /// <summary>
    /// TEST-MCP-TRIAGE-004: independent historical rows must pass the exact migration
    /// assertion runner and all 21 damaged or destructive row controls must reject.
    /// </summary>
    [Fact]
    public void HistoricalAssertions_IndependentRows_RejectDamagedMigrationResults()
    {
        Assert.Equal(new[] { 2, 21 }, HistoricalTodoMigrationContract.VerifyMockGate());
    }

    private IReadOnlyList<HistoricalTodoMigrationContract.Row> ReadRawTodoRows()
    {
        // Historical raw SQL seeds intentionally bypass today's workspace key converter.
        // Observe the same raw storage boundary instead of converting query parameters.
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT WorkspaceId, Id, Title, Section, Priority, Done FROM TodoItems;";
        using var reader = command.ExecuteReader();
        var rows = new List<HistoricalTodoMigrationContract.Row>();
        while (reader.Read())
        {
            rows.Add(new HistoricalTodoMigrationContract.Row(
                reader.GetString(0), reader.GetString(1), reader.GetString(2),
                reader.GetString(3), reader.GetString(4), reader.GetBoolean(5)));
        }

        return rows;
    }

    private int CountTables(string tableName)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $name;";
        command.Parameters.AddWithValue("$name", tableName);
        return Convert.ToInt32(command.ExecuteScalar());
    }
}
