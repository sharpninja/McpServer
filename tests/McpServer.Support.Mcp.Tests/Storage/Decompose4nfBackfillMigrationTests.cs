using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// Strict-4NF decomposition data migrations: validates that the Decompose4nf* migrations
/// backfill pre-existing JSON list column data into the new child tables (and that the
/// DateTimeOffset-to-UTC migration normalizes legacy offset text) by seeding a database at the
/// pre-slice schema and migrating to head with the real SQLite provider migration assembly.
/// </summary>
public sealed class Decompose4nfBackfillMigrationTests : IDisposable
{
    private const string PreSliceMigration = "20260628194717_RepairTriageCreatedTodoWorkspace";
    private const string WorkspacePath = "F:\\GitHub\\McpServer";
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Creates an isolated SQLite database using the real provider migration assembly.</summary>
    public Decompose4nfBackfillMigrationTests()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection, sqlite => sqlite.MigrationsAssembly("McpServer.Storage.SqliteMigrations"))
            .Options;
    }

    /// <summary>
    /// Seeds every decomposed JSON list column at the pre-slice schema, migrates to head, and
    /// asserts each list landed as ordered 4NF child rows with the source columns dropped.
    /// </summary>
    [Fact]
    public void Migrate_Decompose4nf_BackfillsJsonListColumnsIntoChildTables()
    {
        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PreSliceMigration);
            SeedPreSliceData(db);
        }

        using (var db = CreateContext())
        {
            db.Database.Migrate();

            // SessionLogCommit.FilesChangedJson -> SessionLogCommitFiles (ordered)
            var files = db.SessionLogCommitFiles
                .IgnoreQueryFilters()
                .OrderBy(f => f.Ordinal)
                .Select(f => f.Path)
                .ToList();
            Assert.Equal(["src/A.cs", "docs/B.md"], files);

            // TriageReport 4 lists -> TriageReportListItems (discriminated, ordered)
            var listItems = db.TriageReportListItems
                .IgnoreQueryFilters()
                .Where(i => i.ReportId == "triage-report-backfill")
                .ToList();
            Assert.Equal(
                ["src/One.cs", "src/Two.cs"],
                listItems.Where(i => i.ListType == "AffectedPath").OrderBy(i => i.Ordinal).Select(i => i.Value).ToList());
            Assert.Equal(
                ["Ns.Type.Method"],
                listItems.Where(i => i.ListType == "AffectedSymbol").Select(i => i.Value).ToList());
            Assert.Equal(
                ["run the failing test"],
                listItems.Where(i => i.ListType == "ReproductionHint").Select(i => i.Value).ToList());
            Assert.Equal(
                ["bug", "triage"],
                listItems.Where(i => i.ListType == "Tag").OrderBy(i => i.Ordinal).Select(i => i.Value).ToList());

            // Workspace 4 Banned* lists -> WorkspaceBannedItems (categorized, ordered)
            var banned = db.WorkspaceBannedItems
                .IgnoreQueryFilters()
                .Where(b => b.WorkspaceId == WorkspacePath)
                .ToList();
            Assert.Equal(
                ["GPL-3.0", "AGPL-3.0"],
                banned.Where(b => b.Category == "License").OrderBy(b => b.Ordinal).Select(b => b.Value).ToList());
            Assert.Equal(["CN"], banned.Where(b => b.Category == "Country").Select(b => b.Value).ToList());
            Assert.Equal(["EvilCorp"], banned.Where(b => b.Category == "Organization").Select(b => b.Value).ToList());
            Assert.Equal(["mallory"], banned.Where(b => b.Category == "Individual").Select(b => b.Value).ToList());

            // Requirement.AcceptanceCriteriaJson -> RequirementAcceptanceCriteria (all fields, ordered)
            var criteria = db.RequirementAcceptanceCriteria
                .IgnoreQueryFilters()
                .Where(c => c.RequirementId == "FR-MCP-4NF-001")
                .OrderBy(c => c.Ordinal)
                .ToList();
            Assert.Equal(2, criteria.Count);
            Assert.Equal("ac-1", criteria[0].CriterionId);
            Assert.Equal("line one\nline two", criteria[0].Text);
            Assert.False(criteria[0].IsSatisfied);
            Assert.Null(criteria[0].Evidence);
            Assert.Equal("ac-2", criteria[1].CriterionId);
            Assert.Equal("second criterion", criteria[1].Text);
            Assert.True(criteria[1].IsSatisfied);
            Assert.Equal("verified by test", criteria[1].Evidence);

            // TodoItem 5 lists + implementation tasks -> TodoItemListItems / TodoItemTasks
            var todoLists = db.TodoItemListItems
                .IgnoreQueryFilters()
                .Where(i => i.TodoId == "MVP-4NF-001")
                .ToList();
            Assert.Equal(
                ["desc line 1", "desc line 2"],
                todoLists.Where(i => i.ListType == "Description").OrderBy(i => i.Ordinal).Select(i => i.Value).ToList());
            Assert.Equal(
                ["tech detail"],
                todoLists.Where(i => i.ListType == "TechnicalDetail").Select(i => i.Value).ToList());
            Assert.Equal(
                ["MVP-4NF-000"],
                todoLists.Where(i => i.ListType == "DependsOn").Select(i => i.Value).ToList());
            Assert.Equal(
                ["FR-MCP-4NF-001"],
                todoLists.Where(i => i.ListType == "FunctionalRequirement").Select(i => i.Value).ToList());
            Assert.Equal(
                ["TR-MCP-4NF-001"],
                todoLists.Where(i => i.ListType == "TechnicalRequirement").Select(i => i.Value).ToList());

            var todoTasks = db.TodoItemTasks
                .IgnoreQueryFilters()
                .Where(t => t.TodoId == "MVP-4NF-001")
                .OrderBy(t => t.Ordinal)
                .ToList();
            Assert.Equal(2, todoTasks.Count);
            Assert.Equal("first task", todoTasks[0].Task);
            Assert.True(todoTasks[0].Done);
            Assert.Equal("second task", todoTasks[1].Task);
            Assert.False(todoTasks[1].Done);

            // TodoDocumentMetadata Notes + Completed -> TodoDocumentNotes / TodoCompletedGroups / TodoCompletedItems
            var notes = db.TodoDocumentNotes
                .IgnoreQueryFilters()
                .Where(n => n.WorkspaceId == WorkspacePath)
                .OrderBy(n => n.Ordinal)
                .Select(n => n.Value)
                .ToList();
            Assert.Equal(["first note", "second note"], notes);

            var groups = db.TodoCompletedGroups
                .IgnoreQueryFilters()
                .Where(g => g.WorkspaceId == WorkspacePath)
                .OrderBy(g => g.Ordinal)
                .ToList();
            Assert.Equal(2, groups.Count);
            Assert.Equal("2026-06-01", groups[0].Date);
            Assert.Equal("2026-06-15", groups[1].Date);
            var g0Items = db.TodoCompletedItems
                .IgnoreQueryFilters()
                .Where(i => i.GroupId == groups[0].Id)
                .OrderBy(i => i.Ordinal)
                .ToList();
            Assert.Equal(2, g0Items.Count);
            Assert.Equal("DONE-001", g0Items[0].ItemId);
            Assert.Equal("feature", g0Items[0].Qualifier);
            Assert.Equal("shipped the thing", g0Items[0].Summary);
            Assert.Equal("DONE-002", g0Items[1].ItemId);
            var g1Items = db.TodoCompletedItems
                .IgnoreQueryFilters()
                .Where(i => i.GroupId == groups[1].Id)
                .ToList();
            Assert.Single(g1Items);
            Assert.Equal("DONE-003", g1Items[0].ItemId);
            Assert.Null(g1Items[0].Qualifier);

            // Source JSON columns are gone from the rebuilt tables.
            Assert.False(ColumnExists("SessionLogCommits", "FilesChangedJson"));
            Assert.False(ColumnExists("TriageReports", "AffectedPathsJson"));
            Assert.False(ColumnExists("Workspaces", "BannedLicensesJson"));
            Assert.False(ColumnExists("Requirements", "AcceptanceCriteriaJson"));
            Assert.False(ColumnExists("TodoItems", "DescriptionJson"));
            Assert.False(ColumnExists("TodoItems", "ImplementationTasksJson"));
            Assert.False(ColumnExists("TodoDocumentMetadata", "NotesJson"));
            Assert.False(ColumnExists("TodoDocumentMetadata", "CompletedJson"));
        }
    }

    /// <summary>
    /// TR-MCP-DB-DTO-001: legacy DateTimeOffset text (with offset suffix) is normalized in place
    /// to offset-less UTC text by the StoreDateTimeOffsetAsUtcDateTime migration.
    /// </summary>
    [Fact]
    public void Migrate_StoreDateTimeOffsetAsUtcDateTime_NormalizesLegacyOffsetText()
    {
        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PreSliceMigration);
            SeedPreSliceData(db);
        }

        using (var db = CreateContext())
        {
            db.Database.Migrate();

            using var command = _connection.CreateCommand();
            command.CommandText = "SELECT \"FirstReportAtUtc\" FROM \"TriageGroups\" WHERE \"GroupId\" = 'triage-group-backfill';";
            var text = (string)command.ExecuteScalar()!;
            Assert.DoesNotContain("+00:00", text);
            Assert.StartsWith("2026-06-28 10:00:00", text); // seeded 12:00 +02:00 -> 10:00 UTC
        }
    }

    /// <summary>Releases the in-memory database connection.</summary>
    public void Dispose()
    {
        _connection.Dispose();
    }

    private McpDbContext CreateContext() => new(_options);

    private static void SeedPreSliceData(McpDbContext db)
    {
        // Seeded with an explicit +02:00 offset so the UTC normalization is observable.
        var now = new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.FromHours(2));

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "Workspaces" (
                "WorkspaceId", "WorkspacePath", "Name", "TodoPath", "IsPrimary", "IsEnabled",
                "DateTimeCreated", "DateTimeModified", "CurrentRequirementLayerKey",
                "BannedLicensesJson", "BannedCountriesOfOriginJson", "BannedOrganizationsJson", "BannedIndividualsJson",
                "IsDeleted"
            )
            VALUES ({0}, {0}, {1}, {2}, 1, 1, {3}, {3}, {4}, {5}, {6}, {7}, {8}, 0);
            """,
            WorkspacePath,
            "McpServer",
            "docs/Project/TODO.yaml",
            now,
            "layer-1",
            """["GPL-3.0","AGPL-3.0"]""",
            """["CN"]""",
            """["EvilCorp"]""",
            """["mallory"]""");

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "SessionLogs" ("WorkspaceId", "SourceType", "SessionId", "EntryCount", "IsDeleted")
            VALUES ({0}, 'Claude', 'Claude-20260628T120000Z-backfill', 1, 0);
            """,
            WorkspacePath);
        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "SessionLogTurns" ("WorkspaceId", "SessionLogId", "IsDeleted")
            VALUES ({0}, (SELECT "Id" FROM "SessionLogs" LIMIT 1), 0);
            """,
            WorkspacePath);
        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "SessionLogCommits" ("WorkspaceId", "SessionLogTurnId", "Ordinal", "Sha", "FilesChangedJson", "IsDeleted")
            VALUES ({0}, (SELECT "Id" FROM "SessionLogTurns" LIMIT 1), 0, 'abc123', {1}, 0);
            """,
            WorkspacePath,
            """["src/A.cs","docs/B.md"]""");

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "TriageGroups" (
                "GroupId", "WorkspaceId", "GroupKey", "EffectiveWorkspacePath", "Title", "Summary",
                "Status", "ReportCount", "FirstReportAtUtc", "LastReportAtUtc", "QuietDeadlineUtc",
                "IsMcpServerRelated", "IsDeleted"
            )
            VALUES ({0}, {1}, {2}, {1}, {3}, {4}, {5}, 1, {6}, {6}, {6}, 1, 0);
            """,
            "triage-group-backfill",
            WorkspacePath,
            "triage-key-backfill",
            "Backfill group",
            "Backfill the triage report lists.",
            "completed",
            now);
        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "TriageReports" (
                "ReportId", "WorkspaceId", "GroupId", "OriginalWorkspacePath", "EffectiveWorkspacePath",
                "Title", "Summary", "Fingerprint", "Status", "CreatedUtc",
                "AffectedPathsJson", "AffectedSymbolsJson", "ReproductionHintsJson", "TagsJson",
                "IsDeleted"
            )
            VALUES ({0}, {1}, {2}, {1}, {1}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, 0);
            """,
            "triage-report-backfill",
            WorkspacePath,
            "triage-group-backfill",
            "Backfill report",
            "Report carrying JSON lists.",
            "fingerprint-backfill",
            "grouped",
            now,
            """["src/One.cs","src/Two.cs"]""",
            """["Ns.Type.Method"]""",
            """["run the failing test"]""",
            """["bug","triage"]""");

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "TodoItems" (
                "WorkspaceId", "Id", "Title", "Section", "Priority", "Done", "ItemKind",
                "SectionOrder", "ItemOrder",
                "DescriptionJson", "TechnicalDetailsJson", "DependsOnJson",
                "FunctionalRequirementsJson", "TechnicalRequirementsJson", "ImplementationTasksJson",
                "IsDeleted"
            )
            VALUES ({0}, {1}, {2}, 'Backlog', 'medium', 0, 'standard', 0, 0, {3}, {4}, {5}, {6}, {7}, {8}, 0);
            """,
            WorkspacePath,
            "MVP-4NF-001",
            "Backfill todo",
            """["desc line 1","desc line 2"]""",
            """["tech detail"]""",
            """["MVP-4NF-000"]""",
            """["FR-MCP-4NF-001"]""",
            """["TR-MCP-4NF-001"]""",
            """[{"task":"first task","done":true},{"task":"second task","done":false}]""");

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "TodoDocumentMetadata" ("WorkspaceId", "SingletonId", "NotesJson", "CompletedJson", "IsDeleted")
            VALUES ({0}, 1, {1}, {2}, 0);
            """,
            WorkspacePath,
            """["first note","second note"]""",
            """[{"date":"2026-06-01","items":[{"id":"DONE-001","qualifier":"feature","summary":"shipped the thing"},{"id":"DONE-002","qualifier":"fix","summary":"fixed the bug"}]},{"date":"2026-06-15","items":[{"id":"DONE-003","qualifier":null,"summary":"cleanup"}]}]""");

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "Requirements" (
                "WorkspaceId", "Kind", "Id", "Title", "Body", "Priority", "Status",
                "AcceptanceCriteriaJson", "ScopeStartLayerKey", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted"
            )
            VALUES ({0}, 'fr', {1}, {2}, {3}, 'medium', 'pending', {4}, 'layer-1', {5}, {5}, 0);
            """,
            WorkspacePath,
            "FR-MCP-4NF-001",
            "Backfill requirement",
            "Requirement carrying acceptance criteria JSON.",
            """[{"id":"ac-1","text":"line one\nline two","isSatisfied":false,"evidence":null},{"id":"ac-2","text":"second criterion","isSatisfied":true,"evidence":"verified by test"}]""",
            "2026-06-28T12:00:00.0000000+00:00");
    }

    private bool ColumnExists(string tableName, string columnName)
    {
        using var command = _connection.CreateCommand();
        command.CommandText = $"SELECT COUNT(*) FROM pragma_table_info('{tableName}') WHERE name = $col;";
        command.Parameters.AddWithValue("$col", columnName);
        return Convert.ToInt32(command.ExecuteScalar()) > 0;
    }
}
