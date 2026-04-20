using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-TODO-007: behavioral tests for <see cref="LegacyTodoSqliteMigrator"/>.
/// </summary>
/// <remarks>
/// Every test owns a temp directory with a legacy-schema SQLite file. The target
/// database is an in-memory SQLite instance kept alive via a single long-lived
/// <see cref="SqliteConnection"/> so EF scopes created inside the migrator share
/// the same database (<see cref="EfTodoServiceTests"/> uses the same pattern).
/// </remarks>
public sealed class LegacyTodoSqliteMigratorTests : IDisposable
{
    private readonly string _tempRoot;
    private readonly string _legacyPath;
    private readonly SqliteConnection _targetConnection;
    private readonly ServiceProvider _serviceProvider;

    /// <summary>Builds a fresh legacy + target DB pair per test instance.</summary>
    public LegacyTodoSqliteMigratorTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"legacy_todo_mig_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempRoot);
        _legacyPath = Path.Combine(_tempRoot, "mcp.db");

        _targetConnection = new SqliteConnection("Data Source=:memory:");
        _targetConnection.Open();

        var services = new ServiceCollection();
        services.AddDbContext<McpDbContext>(opts => opts.UseSqlite(_targetConnection));
        _serviceProvider = services.BuildServiceProvider();

        using var scope = _serviceProvider.CreateScope();
        scope.ServiceProvider.GetRequiredService<McpDbContext>().Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _serviceProvider.Dispose();
        _targetConnection.Dispose();
        try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true); } catch { /* best-effort */ }
    }

    /// <summary>
    /// Full-copy happy path: legacy rows arrive in the target database with ids,
    /// versions, and metadata preserved.
    /// </summary>
    [Fact]
    public async Task Migrator_CopiesAllRowsPreservingIdsVersionsAndMetadata()
    {
        SeedLegacy(_legacyPath, seedRows: true);

        var migrator = CreateSut(migrateFlag: true);
        var outcome = await migrator.RunAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(LegacyTodoSqliteMigrator.MigrationOutcome.Migrated, outcome);

        using var scope = _serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var items = await ctx.TodoItems.AsNoTracking().OrderBy(i => i.Id).ToListAsync().ConfigureAwait(true);
        Assert.Equal(2, items.Count);
        Assert.Equal("LEG-ONE-001", items[0].Id);
        Assert.Equal("LEG-TWO-002", items[1].Id);

        var history = await ctx.TodoAuditHistory.AsNoTracking().OrderBy(h => h.TodoId).ThenBy(h => h.Version).ToListAsync().ConfigureAwait(true);
        Assert.Equal(3, history.Count);
        Assert.Equal(1, history[0].Version);
        Assert.Equal(2, history[1].Version);
        Assert.Equal("LEG-ONE-001", history[0].TodoId);

        var meta = await ctx.TodoDocumentMetadata.AsNoTracking().SingleAsync().ConfigureAwait(true);
        Assert.Equal("[\"note\"]", meta.NotesJson);
    }

    /// <summary>
    /// Idempotency: when the target already contains TODO rows, the migrator writes
    /// a marker and leaves target data untouched.
    /// </summary>
    [Fact]
    public async Task Migrator_IsIdempotent_WhenTargetTableNonempty()
    {
        SeedLegacy(_legacyPath, seedRows: true);
        await SeedTargetWithSingleRowAsync().ConfigureAwait(true);

        var migrator = CreateSut(migrateFlag: true);
        var outcome = await migrator.RunAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(LegacyTodoSqliteMigrator.MigrationOutcome.SkippedTargetPopulated, outcome);
        Assert.True(File.Exists(_legacyPath + LegacyTodoSqliteMigrator.MarkerFileSuffix));

        using var scope = _serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var items = await ctx.TodoItems.AsNoTracking().ToListAsync().ConfigureAwait(true);
        Assert.Single(items);
        Assert.Equal("TGT-PREEX-001", items[0].Id);
    }

    /// <summary>
    /// Legacy file absent: migrator must treat this as a clean install and no-op
    /// without creating a marker or touching the target database.
    /// </summary>
    [Fact]
    public async Task Migrator_IsNoop_WhenLegacyDbMissing()
    {
        Assert.False(File.Exists(_legacyPath));

        var migrator = CreateSut(migrateFlag: true);
        var outcome = await migrator.RunAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(LegacyTodoSqliteMigrator.MigrationOutcome.SkippedMissingFile, outcome);
        Assert.False(File.Exists(_legacyPath + LegacyTodoSqliteMigrator.MarkerFileSuffix));
    }

    /// <summary>
    /// Flag disabled: migrator must short-circuit even when a legacy file is present.
    /// </summary>
    [Fact]
    public async Task Migrator_IsNoop_WhenFlagFalse()
    {
        SeedLegacy(_legacyPath, seedRows: true);

        var migrator = CreateSut(migrateFlag: false);
        var outcome = await migrator.RunAsync(CancellationToken.None).ConfigureAwait(true);

        Assert.Equal(LegacyTodoSqliteMigrator.MigrationOutcome.SkippedByFlag, outcome);
        Assert.False(File.Exists(_legacyPath + LegacyTodoSqliteMigrator.MarkerFileSuffix));

        using var scope = _serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        Assert.False(await ctx.TodoItems.AnyAsync().ConfigureAwait(true));
    }

    /// <summary>
    /// Post-migration marker prevents re-runs: second invocation sees the marker
    /// and exits without touching either database.
    /// </summary>
    [Fact]
    public async Task Migrator_WritesMarkerFile_ToPreventRerun()
    {
        SeedLegacy(_legacyPath, seedRows: true);

        var first = await CreateSut(migrateFlag: true).RunAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(LegacyTodoSqliteMigrator.MigrationOutcome.Migrated, first);
        Assert.True(File.Exists(_legacyPath + LegacyTodoSqliteMigrator.MarkerFileSuffix));

        var second = await CreateSut(migrateFlag: true).RunAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(LegacyTodoSqliteMigrator.MigrationOutcome.SkippedMarkerPresent, second);
    }

    private LegacyTodoSqliteMigrator CreateSut(bool migrateFlag)
    {
        var storageOptions = Microsoft.Extensions.Options.Options.Create(new TodoStorageOptions
        {
            Provider = TodoStorageOptions.DatabaseProvider,
            MigrateFromLegacySqlite = migrateFlag,
            SqliteDataSource = _legacyPath,
        });
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataFolder"] = _tempRoot,
            })
            .Build();

        return new LegacyTodoSqliteMigrator(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            storageOptions,
            config,
            NullLogger<LegacyTodoSqliteMigrator>.Instance);
    }

    private async Task SeedTargetWithSingleRowAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        ctx.TodoItems.Add(new()
        {
            Id = "TGT-PREEX-001",
            Title = "preexisting",
            Section = "s",
            Priority = "low",
            Done = false,
        });
        await ctx.SaveChangesAsync().ConfigureAwait(true);
    }

    private static void SeedLegacy(string dbPath, bool seedRows)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                CREATE TABLE IF NOT EXISTS todo_items (
                    id TEXT NOT NULL PRIMARY KEY COLLATE NOCASE,
                    title TEXT NOT NULL,
                    section TEXT NOT NULL,
                    priority TEXT NOT NULL,
                    done INTEGER NOT NULL,
                    estimate TEXT NULL,
                    note TEXT NULL,
                    description_json TEXT NULL,
                    technical_details_json TEXT NULL,
                    implementation_tasks_json TEXT NULL,
                    completed_date TEXT NULL,
                    done_summary TEXT NULL,
                    remaining TEXT NULL,
                    priority_note TEXT NULL,
                    reference TEXT NULL,
                    depends_on_json TEXT NULL,
                    functional_requirements_json TEXT NULL,
                    technical_requirements_json TEXT NULL,
                    item_kind TEXT NOT NULL DEFAULT 'standard',
                    section_order INTEGER NOT NULL DEFAULT 0,
                    item_order INTEGER NOT NULL DEFAULT 0,
                    phase_label TEXT NULL
                );
                CREATE TABLE IF NOT EXISTS todo_item_history (
                    audit_id INTEGER PRIMARY KEY AUTOINCREMENT,
                    todo_id TEXT NOT NULL COLLATE NOCASE,
                    version INTEGER NOT NULL,
                    action TEXT NOT NULL,
                    recorded_at_utc TEXT NOT NULL,
                    snapshot_json TEXT NULL,
                    previous_snapshot_json TEXT NULL,
                    source TEXT NULL,
                    UNIQUE(todo_id, version)
                );
                CREATE TABLE IF NOT EXISTS todo_document_metadata (
                    singleton_id INTEGER PRIMARY KEY CHECK (singleton_id = 1),
                    notes_json TEXT NULL,
                    completed_json TEXT NULL,
                    code_review_reference TEXT NULL,
                    last_imported_from_yaml_utc TEXT NULL,
                    last_projected_to_yaml_utc TEXT NULL,
                    last_projection_failure_utc TEXT NULL,
                    last_projection_failure_message TEXT NULL
                );";
            cmd.ExecuteNonQuery();
        }

        if (!seedRows) return;

        using (var cmd = conn.CreateCommand())
        {
            cmd.CommandText = @"
                INSERT INTO todo_items(id, title, section, priority, done, item_kind, section_order, item_order)
                VALUES
                    ('LEG-ONE-001','legacy one','support','high',0,'standard',1,1),
                    ('LEG-TWO-002','legacy two','support','low' ,1,'standard',1,2);
                INSERT INTO todo_item_history(todo_id, version, action, recorded_at_utc, snapshot_json, source)
                VALUES
                    ('LEG-ONE-001', 1, 'created',  '2026-04-20T00:00:00Z', '{""id"":""LEG-ONE-001""}', 'import'),
                    ('LEG-ONE-001', 2, 'updated',  '2026-04-20T00:00:01Z', '{""id"":""LEG-ONE-001""}', 'api'),
                    ('LEG-TWO-002', 1, 'created',  '2026-04-20T00:00:02Z', '{""id"":""LEG-TWO-002""}', 'import');
                INSERT INTO todo_document_metadata(singleton_id, notes_json)
                VALUES (1, '[""note""]');";
            cmd.ExecuteNonQuery();
        }
    }
}
