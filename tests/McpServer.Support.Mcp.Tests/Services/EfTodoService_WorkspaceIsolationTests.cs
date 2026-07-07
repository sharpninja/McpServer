using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-TODO-008 Phase 3 acceptance: <see cref="EfTodoService"/>
/// MUST honor the active workspace resolved by <c>WorkspaceAuthMiddleware</c>
/// (TR-MCP-MT-003). Reads, updates, and deletes issued under one workspace
/// MUST NOT observe or mutate rows owned by another workspace, and the same
/// canonical TODO id MAY coexist across two workspaces under the composite
/// <c>(WorkspaceId, Id)</c> primary key.
/// </summary>
/// <remarks>
/// The <c>PLAN-BITNETINTEGRATION-001</c> case is the live-workspace collision
/// observed between <c>bitnet-b1.58-sharp</c> and <c>TruckMate</c>: both carry
/// a plan-TODO with that id, so the implementation must accept them as two
/// logically independent rows keyed on <c>(WorkspaceId, Id)</c>.
/// </remarks>
public sealed class EfTodoService_WorkspaceIsolationTests : IDisposable
{
    private const string SharedId = "PLAN-BITNETINTEGRATION-001";

    private readonly MutableWorkspace _workspace = new();
    private readonly ServiceProvider _serviceProvider;
    private readonly SqliteConnection _connection;
    private readonly string _tempRoot;
    private readonly string _workspaceA;
    private readonly string _workspaceB;
    private readonly EfTodoService _sut;

    /// <summary>Builds an isolated EF TODO stack with switchable workspace context.</summary>
    public EfTodoService_WorkspaceIsolationTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"ef_todo_workspace_{Guid.NewGuid():N}");
        _workspaceA = Path.Combine(_tempRoot, "workspace-a");
        _workspaceB = Path.Combine(_tempRoot, "workspace-b");
        Directory.CreateDirectory(Path.Combine(_workspaceA, "docs", "Project"));
        Directory.CreateDirectory(Path.Combine(_workspaceB, "docs", "Project"));

        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        services.AddScoped(_ => new WorkspaceContext { WorkspacePath = _workspace.CurrentWorkspacePath });
        services.AddDbContext<McpDbContext>(opts => opts.UseSqlite(_connection));

        _serviceProvider = services.BuildServiceProvider();
        using (var scope = _serviceProvider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            ctx.Database.EnsureCreated();
        }

        _sut = new EfTodoService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new IngestionOptions
            {
                RepoRoot = _workspaceA,
                TodoFilePath = Path.Combine("docs", "Project", "TODO.yaml"),
            }),
            Microsoft.Extensions.Options.Options.Create(new TodoStorageOptions { Provider = TodoStorageOptions.DatabaseProvider }),
            Substitute.For<IWriteAuditLog>(),
            NullLogger<EfTodoService>.Instance,
            Substitute.For<IChangeEventBus>());
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sut.Dispose();
        _serviceProvider.Dispose();
        _connection.Dispose();
        try
        {
            if (Directory.Exists(_tempRoot))
                Directory.Delete(_tempRoot, true);
        }
        catch
        {
            // Best-effort cleanup for Windows file locking during test teardown.
        }
    }

    /// <summary>
    /// <c>GET</c>-shape queries scoped to workspace A MUST return only A's
    /// rows even when B owns rows with overlapping or disjoint ids.
    /// </summary>
    [Fact]
    public async Task QueryAsync_WorkspaceA_DoesNotReturnWorkspaceBRows()
    {
        UseWorkspaceA();
        await CreateRequiredAsync(SharedId, "workspace A shared").ConfigureAwait(true);
        await CreateRequiredAsync("TODO-A-001", "workspace A only").ConfigureAwait(true);

        UseWorkspaceB();
        await CreateRequiredAsync(SharedId, "workspace B shared").ConfigureAwait(true);
        await CreateRequiredAsync("TODO-B-001", "workspace B only").ConfigureAwait(true);

        UseWorkspaceA();
        var result = await _sut.QueryAsync(new TodoQueryRequest(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(2, result.TotalCount);
        Assert.Contains(result.Items, item => item.Id == SharedId && item.Title == "workspace A shared");
        Assert.Contains(result.Items, item => item.Id == "TODO-A-001");
        Assert.DoesNotContain(result.Items, item => item.Title == "workspace B shared" || item.Id == "TODO-B-001");
    }

    /// <summary>
    /// <c>DELETE</c> issued under workspace A MUST NOT remove a row with the
    /// same canonical id that lives in workspace B.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_WorkspaceA_LeavesMatchingIdInWorkspaceBIntact()
    {
        UseWorkspaceA();
        await CreateRequiredAsync(SharedId, "workspace A shared").ConfigureAwait(true);

        UseWorkspaceB();
        await CreateRequiredAsync(SharedId, "workspace B shared").ConfigureAwait(true);

        UseWorkspaceA();
        var deleted = await _sut.DeleteAsync(SharedId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(deleted.Success, deleted.Error);
        Assert.Null(await _sut.GetByIdAsync(SharedId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true));

        UseWorkspaceB();
        var remaining = await _sut.GetByIdAsync(SharedId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.NotNull(remaining);
        Assert.Equal("workspace B shared", remaining!.Title);
    }

    /// <summary>
    /// The duplicate-id check in <c>CreateAsync</c> MUST be scoped to the
    /// active workspace so the same id can be created in two workspaces.
    /// </summary>
    [Fact]
    public async Task CreateAsync_SameIdInTwoWorkspaces_BothSucceed_PlanBitNetIntegrationCase()
    {
        UseWorkspaceA();
        var first = await _sut.CreateAsync(CreateRequest(SharedId, "bitnet plan"), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(first.Success, first.Error);

        UseWorkspaceB();
        var second = await _sut.CreateAsync(CreateRequest(SharedId, "truckmate plan"), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(second.Success, second.Error);

        using var scope = _serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var rows = await ctx.TodoItems
            .IgnoreQueryFilters()
            .Where(item => item.Id == SharedId)
            .OrderBy(item => item.WorkspaceId)
            .ToListAsync(cancellationToken: TestContext.Current.CancellationToken)
            .ConfigureAwait(true);

        Assert.Equal(2, rows.Count);
        Assert.Contains(rows, row => row.WorkspaceId == _workspaceA && row.Title == "bitnet plan");
        Assert.Contains(rows, row => row.WorkspaceId == _workspaceB && row.Title == "truckmate plan");
    }

    /// <summary>
    /// Audit history lookups scoped to workspace A MUST NOT surface audit
    /// rows recorded under workspace B, even when the underlying
    /// <c>(TodoId, Version)</c> pair collides.
    /// </summary>
    [Fact]
    public async Task GetAuditAsync_WorkspaceA_DoesNotLeakWorkspaceBAuditRows()
    {
        UseWorkspaceA();
        await CreateRequiredAsync(SharedId, "workspace A initial").ConfigureAwait(true);
        var updateA = await _sut.UpdateAsync(SharedId, new TodoUpdateRequest { Title = "workspace A updated" }, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.True(updateA.Success, updateA.Error);

        UseWorkspaceB();
        await CreateRequiredAsync(SharedId, "workspace B initial").ConfigureAwait(true);

        UseWorkspaceA();
        var auditA = await _sut.GetAuditAsync(SharedId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(2, auditA.TotalCount);
        Assert.All(auditA.Entries, entry => Assert.DoesNotContain("workspace B", entry.Snapshot?.Title, StringComparison.OrdinalIgnoreCase));
        Assert.Contains(auditA.Entries, entry => entry.Version == 1 && entry.Action == "created");
        Assert.Contains(auditA.Entries, entry => entry.Version == 2 && entry.Action == "updated");

        UseWorkspaceB();
        var auditB = await _sut.GetAuditAsync(SharedId, cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Single(auditB.Entries);
        Assert.Equal(1, auditB.Entries[0].Version);
        Assert.Equal("workspace B initial", auditB.Entries[0].Snapshot?.Title);
    }

    /// <summary>
    /// <see cref="LegacyTodoSqliteMigrator"/> MUST stamp imported rows with the
    /// active workspace's path; those rows MUST remain invisible from other
    /// workspace scopes.
    /// </summary>
    [Fact]
    public async Task LegacyMigrator_StampsWorkspaceIdOnImportedRows()
    {
        var legacyPath = Path.Combine(_tempRoot, "legacy-mcp.db");
        SeedLegacy(legacyPath);

        UseWorkspaceA();
        var outcome = await CreateMigrator(legacyPath).RunAsync(CancellationToken.None).ConfigureAwait(true);
        Assert.Equal(LegacyTodoSqliteMigrator.MigrationOutcome.Migrated, outcome);

        UseWorkspaceA();
        var imported = await _sut.QueryAsync(new TodoQueryRequest(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Single(imported.Items);
        Assert.Equal("LEGACY-001", imported.Items[0].Id);

        UseWorkspaceB();
        var otherWorkspace = await _sut.QueryAsync(new TodoQueryRequest(), cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        Assert.Empty(otherWorkspace.Items);

        using var scope = _serviceProvider.CreateScope();
        var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var item = await ctx.TodoItems.IgnoreQueryFilters().SingleAsync(i => i.Id == "LEGACY-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var audit = await ctx.TodoAuditHistory.IgnoreQueryFilters().SingleAsync(i => i.TodoId == "LEGACY-001", cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);
        var metadata = await ctx.TodoDocumentMetadata.IgnoreQueryFilters().SingleAsync(cancellationToken: TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.Equal(_workspaceA, item.WorkspaceId);
        Assert.Equal(_workspaceA, audit.WorkspaceId);
        Assert.Equal(_workspaceA, metadata.WorkspaceId);
    }

    private void UseWorkspaceA() => _workspace.CurrentWorkspacePath = _workspaceA;

    private void UseWorkspaceB() => _workspace.CurrentWorkspacePath = _workspaceB;

    private async Task CreateRequiredAsync(string id, string title)
    {
        var result = await _sut.CreateAsync(CreateRequest(id, title)).ConfigureAwait(true);
        Assert.True(result.Success, result.Error);
    }

    private static TodoCreateRequest CreateRequest(string id, string title) => new()
    {
        Id = id,
        Title = title,
        Section = "workspace-isolation",
        Priority = "high",
    };

    private LegacyTodoSqliteMigrator CreateMigrator(string legacyPath)
    {
        var config = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["DataFolder"] = _tempRoot,
            })
            .Build();

        return new LegacyTodoSqliteMigrator(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            Microsoft.Extensions.Options.Options.Create(new TodoStorageOptions
            {
                Provider = TodoStorageOptions.DatabaseProvider,
                MigrateFromLegacySqlite = true,
                SqliteDataSource = legacyPath,
            }),
            config,
            NullLogger<LegacyTodoSqliteMigrator>.Instance);
    }

    private static void SeedLegacy(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = @"
            CREATE TABLE todo_items (
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
            CREATE TABLE todo_item_history (
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
            CREATE TABLE todo_document_metadata (
                singleton_id INTEGER PRIMARY KEY CHECK (singleton_id = 1),
                notes_json TEXT NULL,
                completed_json TEXT NULL,
                code_review_reference TEXT NULL,
                last_imported_from_yaml_utc TEXT NULL,
                last_projected_to_yaml_utc TEXT NULL,
                last_projection_failure_utc TEXT NULL,
                last_projection_failure_message TEXT NULL
            );
            INSERT INTO todo_items(id, title, section, priority, done, item_kind, section_order, item_order)
            VALUES ('LEGACY-001', 'legacy item', 'legacy', 'high', 0, 'standard', 1, 1);
            INSERT INTO todo_item_history(todo_id, version, action, recorded_at_utc, snapshot_json, source)
            VALUES ('LEGACY-001', 1, 'created', '2026-04-20T00:00:00Z', '{""id"":""LEGACY-001"",""title"":""legacy item""}', 'legacy');
            INSERT INTO todo_document_metadata(singleton_id, notes_json)
            VALUES (1, '[""legacy note""]');";
        cmd.ExecuteNonQuery();
    }

    private sealed class MutableWorkspace
    {
        public string? CurrentWorkspacePath { get; set; }
    }
}
