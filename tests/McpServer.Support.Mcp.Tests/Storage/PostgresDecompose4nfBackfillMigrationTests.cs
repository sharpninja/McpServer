using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Npgsql;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Storage;

/// <summary>
/// PostgreSQL twin of <see cref="Decompose4nfBackfillMigrationTests"/>: seeds every decomposed
/// JSON list column at the pre-slice schema and migrates to head (and back down) through the real
/// PostgreSQL provider migration assembly, proving the jsonb backfill and reconstruction SQL.
/// Requires a reachable server: set <c>MCP_TEST_POSTGRES_CONNECTION</c> to a server-level
/// connection string (a scratch database is created and dropped per test); the tests skip when
/// the variable is absent.
/// </summary>
public sealed class PostgresDecompose4nfBackfillMigrationTests : IDisposable
{
    private const string PreSliceMigration = "20260628194746_RepairTriageCreatedTodoWorkspace";
    private const string WorkspacePath = "F:\\GitHub\\McpServer";
    private readonly string? _serverConnectionString;
    private readonly string _databaseName = $"mcp_backfill_{Guid.NewGuid():N}";
    private DbContextOptions<McpDbContext>? _options;
    private string? _databaseConnectionString;

    /// <summary>Resolves the gated server connection from the environment.</summary>
    public PostgresDecompose4nfBackfillMigrationTests()
    {
        _serverConnectionString = Environment.GetEnvironmentVariable("MCP_TEST_POSTGRES_CONNECTION");
    }

    /// <summary>
    /// Seeds every decomposed JSON list column at the pre-slice schema, migrates to head, and
    /// asserts each list landed as ordered 4NF child rows with the source columns dropped.
    /// </summary>
    [Fact]
    public void Migrate_Decompose4nf_BackfillsJsonListColumnsIntoChildTables()
    {
        EnsureScratchDatabase();

        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PreSliceMigration);
            SeedPreSliceData(db);
        }

        using (var db = CreateContext())
        {
            db.Database.Migrate();

            var files = db.SessionLogCommitFiles
                .IgnoreQueryFilters()
                .OrderBy(f => f.Ordinal)
                .Select(f => f.Path)
                .ToList();
            Assert.Equal(["src/A.cs", "docs/B.md"], files);

            var todoLists = db.TodoItemListItems
                .IgnoreQueryFilters()
                .Where(i => i.TodoId == "MVP-4NF-001")
                .ToList();
            Assert.Equal(
                ["desc line 1", "desc line 2"],
                todoLists.Where(i => i.ListType == "Description").OrderBy(i => i.Ordinal).Select(i => i.Value).ToList());
            Assert.Equal(
                ["FR-MCP-4NF-001"],
                todoLists.Where(i => i.ListType == "FunctionalRequirement").Select(i => i.Value).ToList());
            var todoTasks = db.TodoItemTasks
                .IgnoreQueryFilters()
                .Where(t => t.TodoId == "MVP-4NF-001")
                .OrderBy(t => t.Ordinal)
                .ToList();
            Assert.Equal(2, todoTasks.Count);
            Assert.True(todoTasks[0].Done);
            Assert.False(todoTasks[1].Done);

            var criteria = db.RequirementAcceptanceCriteria
                .IgnoreQueryFilters()
                .Where(c => c.RequirementId == "FR-MCP-4NF-001")
                .OrderBy(c => c.Ordinal)
                .ToList();
            Assert.Equal(2, criteria.Count);
            Assert.Equal("line one\nline two", criteria[0].Text);
            Assert.True(criteria[1].IsSatisfied);
            Assert.Equal("verified by test", criteria[1].Evidence);

            var notes = db.TodoDocumentNotes
                .IgnoreQueryFilters()
                .OrderBy(n => n.Ordinal)
                .Select(n => n.Value)
                .ToList();
            Assert.Equal(["first note", "second note"], notes);
            var groups = db.TodoCompletedGroups
                .IgnoreQueryFilters()
                .Include(g => g.Items)
                .OrderBy(g => g.Ordinal)
                .ToList();
            Assert.Equal(2, groups.Count);
            Assert.Equal("2026-06-01", groups[0].Date);
            Assert.Equal(2, groups[0].Items.Count);
            Assert.Equal("DONE-001", groups[0].Items.OrderBy(i => i.Ordinal).First().ItemId);

            var banned = db.WorkspaceBannedItems
                .IgnoreQueryFilters()
                .Where(b => b.Category == "License")
                .OrderBy(b => b.Ordinal)
                .Select(b => b.Value)
                .ToList();
            Assert.Equal(["GPL-3.0", "AGPL-3.0"], banned);

            var triageTags = db.TriageReportListItems
                .IgnoreQueryFilters()
                .Where(i => i.ListType == "Tag")
                .OrderBy(i => i.Ordinal)
                .Select(i => i.Value)
                .ToList();
            Assert.Equal(["bug", "triage"], triageTags);

            var agentModels = db.AgentDefinitionModels
                .IgnoreQueryFilters()
                .Where(m => m.AgentDefinitionId == "test-agent")
                .OrderBy(m => m.Ordinal)
                .Select(m => m.Model)
                .ToList();
            Assert.Equal(["model-a", "model-b"], agentModels);
            var overrides = db.AgentWorkspaceListItems
                .IgnoreQueryFilters()
                .Where(i => i.ListType == "InstructionFileOverride")
                .OrderBy(i => i.Ordinal)
                .Select(i => i.Value)
                .ToList();
            Assert.Equal(["CLAUDE.md", "AGENTS.md"], overrides);

            Assert.False(ColumnExists("SessionLogCommits", "FilesChangedJson"));
            Assert.False(ColumnExists("TodoItems", "DescriptionJson"));
            Assert.False(ColumnExists("TodoDocumentMetadata", "NotesJson"));
            Assert.False(ColumnExists("AgentDefinitions", "DefaultModelsJson"));
            Assert.False(ColumnExists("Requirements", "AcceptanceCriteriaJson"));
        }
    }

    /// <summary>
    /// Down round-trip: after migrating to head, migrating back to the pre-slice schema
    /// reconstructs the decomposed JSON columns from the child rows.
    /// </summary>
    [Fact]
    public void Migrate_DownToPreSlice_ReconstructsJsonColumnsFromChildRows()
    {
        EnsureScratchDatabase();

        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PreSliceMigration);
            SeedPreSliceData(db);
        }

        using (var db = CreateContext())
        {
            db.Database.Migrate();
        }

        using (var db = CreateContext())
        {
            db.GetService<IMigrator>().Migrate(PreSliceMigration);
        }

        Assert.Equal(new[] { "src/A.cs", "docs/B.md" }, ReadJsonStringArray(
            "SELECT \"FilesChangedJson\" FROM \"SessionLogCommits\" LIMIT 1;"));
        Assert.Equal(new[] { "desc line 1", "desc line 2" }, ReadJsonStringArray(
            "SELECT \"DescriptionJson\" FROM \"TodoItems\" WHERE \"Id\" = 'MVP-4NF-001';"));
        Assert.Equal(new[] { "first note", "second note" }, ReadJsonStringArray(
            "SELECT \"NotesJson\" FROM \"TodoDocumentMetadata\" LIMIT 1;"));
        Assert.Equal(new[] { "model-a", "model-b" }, ReadJsonStringArray(
            "SELECT \"DefaultModelsJson\" FROM \"AgentDefinitions\" WHERE \"Id\" = 'test-agent';"));

        using var critDoc = System.Text.Json.JsonDocument.Parse(ReadScalarText(
            "SELECT \"AcceptanceCriteriaJson\" FROM \"Requirements\" WHERE \"Id\" = 'FR-MCP-4NF-001';"));
        var criteria = critDoc.RootElement;
        Assert.Equal(2, criteria.GetArrayLength());
        Assert.Equal("ac-1", criteria[0].GetProperty("id").GetString());
        Assert.Equal("verified by test", criteria[1].GetProperty("evidence").GetString());

        using var completedDoc = System.Text.Json.JsonDocument.Parse(ReadScalarText(
            "SELECT \"CompletedJson\" FROM \"TodoDocumentMetadata\" LIMIT 1;"));
        var completed = completedDoc.RootElement;
        Assert.Equal(2, completed.GetArrayLength());
        Assert.Equal("2026-06-01", completed[0].GetProperty("date").GetString());
        Assert.Equal(2, completed[0].GetProperty("items").GetArrayLength());
        Assert.Equal("shipped the thing", completed[0].GetProperty("items")[0].GetProperty("summary").GetString());
    }

    /// <summary>Drops the scratch database.</summary>
    public void Dispose()
    {
        if (_serverConnectionString is null || _options is null)
            return;
        try
        {
            using var admin = new NpgsqlConnection(_serverConnectionString);
            admin.Open();
            using var terminate = admin.CreateCommand();
            terminate.CommandText =
                $"SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '{_databaseName}' AND pid <> pg_backend_pid();";
            terminate.ExecuteNonQuery();
            using var drop = admin.CreateCommand();
            drop.CommandText = $"DROP DATABASE IF EXISTS \"{_databaseName}\";";
            drop.ExecuteNonQuery();
        }
        catch (NpgsqlException)
        {
            // Best-effort cleanup; scratch databases are uniquely named.
        }
    }

    private void EnsureScratchDatabase()
    {
        Assert.SkipWhen(
            string.IsNullOrWhiteSpace(_serverConnectionString),
            "MCP_TEST_POSTGRES_CONNECTION is not set; skipping PostgreSQL migration tests.");

        using (var admin = new NpgsqlConnection(_serverConnectionString))
        {
            admin.Open();
            using var create = admin.CreateCommand();
            create.CommandText = $"CREATE DATABASE \"{_databaseName}\";";
            create.ExecuteNonQuery();
        }

        var builder = new NpgsqlConnectionStringBuilder(_serverConnectionString) { Database = _databaseName };
        _databaseConnectionString = builder.ToString();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseNpgsql(_databaseConnectionString, npgsql => npgsql.MigrationsAssembly("McpServer.Storage.PostgreSqlMigrations"))
            .Options;
    }

    private McpDbContext CreateContext() => new(_options!);

    private static void SeedPreSliceData(McpDbContext db)
    {
        var now = new DateTimeOffset(2026, 6, 28, 12, 0, 0, TimeSpan.FromHours(2));

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "Workspaces" (
                "WorkspaceId", "WorkspacePath", "Name", "TodoPath", "IsPrimary", "IsEnabled",
                "DateTimeCreated", "DateTimeModified", "CurrentRequirementLayerKey",
                "BannedLicensesJson", "BannedCountriesOfOriginJson", "BannedOrganizationsJson", "BannedIndividualsJson",
                "IsDeleted"
            )
            VALUES ({0}, {0}, {1}, {2}, true, true, {3}, {3}, {4}, {5}, {6}, {7}, {8}, false);
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
            VALUES ({0}, 'Claude', 'Claude-20260628T120000Z-backfill', 1, false);
            """,
            WorkspacePath);
        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "SessionLogTurns" ("WorkspaceId", "SessionLogId", "IsDeleted")
            VALUES ({0}, (SELECT "Id" FROM "SessionLogs" LIMIT 1), false);
            """,
            WorkspacePath);
        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "SessionLogCommits" ("WorkspaceId", "SessionLogTurnId", "Ordinal", "Sha", "FilesChangedJson", "IsDeleted")
            VALUES ({0}, (SELECT "Id" FROM "SessionLogTurns" LIMIT 1), 0, 'abc123', {1}, false);
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
            VALUES ({0}, {1}, {2}, {1}, {3}, {4}, {5}, 1, {6}, {6}, {6}, true, false);
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
            VALUES ({0}, {1}, {2}, {1}, {1}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11}, false);
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
            VALUES ({0}, {1}, {2}, 'Backlog', 'medium', false, 'standard', 0, 0, {3}, {4}, {5}, {6}, {7}, {8}, false);
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
            VALUES ({0}, 1, {1}, {2}, false);
            """,
            WorkspacePath,
            """["first note","second note"]""",
            """[{"date":"2026-06-01","items":[{"id":"DONE-001","qualifier":"feature","summary":"shipped the thing"},{"id":"DONE-002","qualifier":"fix","summary":"fixed the bug"}]},{"date":"2026-06-15","items":[{"id":"DONE-003","qualifier":null,"summary":"cleanup"}]}]""");

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "AgentDefinitions" (
                "Id", "WorkspaceId", "DisplayName", "DefaultLaunchCommand", "DefaultInstructionFile",
                "DefaultModelsJson", "DefaultBranchStrategy", "DefaultSeedPrompt", "IsBuiltIn",
                "CreatedAt", "ModifiedAt", "IsDeleted"
            )
            VALUES ('test-agent', '', 'Test Agent', 'run', 'CLAUDE.md', {0}, 'feature/{{agent}}', '', false, {1}, {1}, false);
            """,
            """["model-a","model-b"]""",
            new DateTime(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc));
        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "AgentWorkspaces" (
                "WorkspaceId", "AgentDefinitionId", "WorkspacePath", "Enabled", "Banned",
                "AgentIsolation", "ModelsOverrideJson", "InstructionFilesOverrideJson",
                "MarkerAdditions", "RestartPolicy", "AddedAt", "IsDeleted"
            )
            VALUES ({0}, 'test-agent', {0}, true, false, 'worktree', {1}, {2}, '', 'never', {3}, false);
            """,
            WorkspacePath,
            """["override-model"]""",
            """["CLAUDE.md","AGENTS.md"]""",
            new DateTime(2026, 6, 28, 12, 0, 0, DateTimeKind.Utc));

        db.Database.ExecuteSqlRaw(
            """
            INSERT INTO "Requirements" (
                "WorkspaceId", "Kind", "Id", "Title", "Body", "Priority", "Status",
                "AcceptanceCriteriaJson", "ScopeStartLayerKey", "CreatedAtUtc", "UpdatedAtUtc", "IsDeleted"
            )
            VALUES ({0}, 'fr', {1}, {2}, {3}, 'medium', 'pending', {4}, 'layer-1', {5}, {5}, false);
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
        using var connection = new NpgsqlConnection(_databaseConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText =
            "SELECT COUNT(*) FROM information_schema.columns WHERE table_name = @t AND column_name = @c;";
        command.Parameters.AddWithValue("@t", tableName);
        command.Parameters.AddWithValue("@c", columnName);
        return Convert.ToInt64(command.ExecuteScalar()) > 0;
    }

    private string ReadScalarText(string sql)
    {
        using var connection = new NpgsqlConnection(_databaseConnectionString);
        connection.Open();
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        var value = command.ExecuteScalar();
        Assert.NotNull(value);
        Assert.IsNotType<DBNull>(value);
        return (string)value!;
    }

    private string[] ReadJsonStringArray(string sql)
    {
        using var doc = System.Text.Json.JsonDocument.Parse(ReadScalarText(sql));
        return doc.RootElement.EnumerateArray().Select(e => e.GetString()!).ToArray();
    }
}
