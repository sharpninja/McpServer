using McpServer.Support.Mcp.Ingestion;
using McpServer.Support.Mcp.Notifications;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using Xunit;

namespace McpServer.Support.Mcp.Tests.Services;

/// <summary>
/// TR-MCP-TODO-005 (provider-agnostic): Behavioral tests for <see cref="EfTodoService"/>.
/// Tests run against an in-memory Sqlite provider so the same <see cref="McpDbContext"/>
/// configuration exercises the production relational code path.
/// </summary>
/// <remarks>
/// Byrd Development Process: these tests define the contract EfTodoService must satisfy
/// in phase 3. The service's method bodies start as <see cref="NotImplementedException"/>;
/// each test flips to green once its corresponding method is ported from
/// <see cref="SqliteTodoService"/>. The test list covers the acceptance criteria from
/// <c>plan-todo-provider-agnostic-v1.0.md</c> phase 3.
/// </remarks>
public sealed class EfTodoServiceTests : IDisposable
{
    private readonly ServiceProvider _serviceProvider;
    private readonly SqliteConnection _connection;
    private readonly string _tempRoot;
    private readonly string _tempYamlPath;
    private readonly IWriteAuditLog _auditLog = Substitute.For<IWriteAuditLog>();
    private readonly IChangeEventBus _eventBus = Substitute.For<IChangeEventBus>();
    private readonly EfTodoService _sut;

    /// <summary>Builds an isolated in-memory Sqlite EF stack per test instance.</summary>
    public EfTodoServiceTests()
    {
        _tempRoot = Path.Combine(Path.GetTempPath(), $"ef_todo_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(_tempRoot, "docs", "Project"));
        _tempYamlPath = Path.Combine(_tempRoot, "docs", "Project", "TODO.yaml");

        // Keep a single open connection for the fixture lifetime so the in-memory database
        // survives across EF scopes (SQLite drops ":memory:" when the last connection closes).
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();

        var services = new ServiceCollection();
        // TR-MCP-TODO-008: WorkspaceContext pinned to the test repo root so the
        // global query filter installed in McpDbContext lets the fixture see its
        // own inserts. Without this, `!IsNullOrEmpty(_workspaceId) && e.WorkspaceId == _workspaceId`
        // is `false && ...` = false, suppressing every row.
        services.AddScoped(_ => new McpServer.Support.Mcp.Services.WorkspaceContext
        {
            WorkspacePath = _tempRoot,
        });
        services.AddDbContext<McpDbContext>(opts => opts.UseSqlite(_connection));

        _serviceProvider = services.BuildServiceProvider();
        using (var scope = _serviceProvider.CreateScope())
        {
            var ctx = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            ctx.Database.EnsureCreated();
        }

        var ingestionOptions = Microsoft.Extensions.Options.Options.Create(new IngestionOptions
        {
            RepoRoot = _tempRoot,
            TodoFilePath = _tempYamlPath,
        });
        var storageOptions = Microsoft.Extensions.Options.Options.Create(new TodoStorageOptions
        {
            Provider = TodoStorageOptions.DatabaseProvider,
        });

        _sut = new EfTodoService(
            _serviceProvider.GetRequiredService<IServiceScopeFactory>(),
            ingestionOptions,
            storageOptions,
            _auditLog,
            NullLogger<EfTodoService>.Instance,
            _eventBus);
    }

    /// <inheritdoc />
    public void Dispose()
    {
        _sut.Dispose();
        _serviceProvider.Dispose();
        _connection.Dispose();
        try { if (Directory.Exists(_tempRoot)) Directory.Delete(_tempRoot, true); } catch { /* best-effort */ }
    }

    /// <summary>
    /// Phase-3 acceptance: <see cref="EfTodoService.CreateAsync"/> persists a row
    /// through <see cref="McpDbContext"/>, round-trips via
    /// <see cref="EfTodoService.GetByIdAsync"/>, and publishes a <c>todo.created</c>
    /// change event.
    /// </summary>
    [Fact]
    public async Task CreateAsync_PersistsAndRoundTrips()
    {
        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-TODO-001",
            Title = "EF TODO",
            Section = "mvp-support",
            Priority = "high",
        }).ConfigureAwait(true);

        Assert.True(result.Success);
        var item = await _sut.GetByIdAsync("EF-TODO-001").ConfigureAwait(true);
        Assert.NotNull(item);
        Assert.Equal("EF TODO", item!.Title);
    }

    /// <summary>
    /// Phase-3 acceptance: <see cref="EfTodoService.UpdateAsync"/> is append-only
    /// in the audit table - one history row per version, monotonic
    /// <c>(TodoId, Version)</c>.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_AppendsAuditRow_MonotonicVersion()
    {
        await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-TODO-002",
            Title = "Initial",
            Section = "mvp-support",
            Priority = "low",
        }).ConfigureAwait(true);

        await _sut.UpdateAsync("EF-TODO-002", new TodoUpdateRequest { Title = "Second" }).ConfigureAwait(true);
        var audit = await _sut.GetAuditAsync("EF-TODO-002").ConfigureAwait(true);
        Assert.True(audit.TotalCount >= 2);
        Assert.Contains(audit.Entries, e => e.Action == "updated");
    }

    /// <summary>
    /// TEST-MCP-161: Transaction rollback compensation restores EF TODO state
    /// exactly enough to clear values that public update DTOs cannot clear.
    /// </summary>
    [Fact]
    public async Task RestoreAsync_AfterUpdate_RestoresCapturedEntityState()
    {
        var create = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-TXN-001",
            Title = "Before",
            Section = "Backlog",
            Priority = "high",
            FunctionalRequirements = ["FR-BEFORE-001"],
        }).ConfigureAwait(true);
        Assert.True(create.Success, create.Error);

        var snapshot = await _sut.CaptureForRestoreAsync("EF-TXN-001").ConfigureAwait(true);
        Assert.NotNull(snapshot);

        var update = await _sut.UpdateAsync("EF-TXN-001", new TodoUpdateRequest
        {
            Title = "After",
            Note = "note after",
            Remaining = "remaining after",
            FunctionalRequirements = ["FR-AFTER-001"],
        }).ConfigureAwait(true);
        Assert.True(update.Success, update.Error);

        var restore = await _sut.RestoreAsync(snapshot!).ConfigureAwait(true);
        Assert.True(restore.Success, restore.Error);

        var restored = await _sut.GetByIdAsync("EF-TXN-001").ConfigureAwait(true);
        Assert.NotNull(restored);
        Assert.Equal("Before", restored!.Title);
        Assert.Null(restored.Note);
        Assert.Null(restored.Remaining);
        Assert.Equal(["FR-BEFORE-001"], restored.FunctionalRequirements);

        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
        var links = await db.TodoRequirementLinks
            .OrderBy(row => row.RequirementId)
            .Select(row => row.RequirementId)
            .ToListAsync()
            .ConfigureAwait(true);
        Assert.Equal(["FR-BEFORE-001"], links);
    }

    /// <summary>
    /// TEST-MCP-161: Store-level compensation restores a soft-deleted EF TODO row.
    /// </summary>
    [Fact]
    public async Task RestoreAsync_AfterDelete_RestoresSoftDeletedTodo()
    {
        var create = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-TXN-002",
            Title = "Delete rollback",
            Section = "Backlog",
            Priority = "medium",
            FunctionalRequirements = ["FR-DELETE-BEFORE-001"],
        }).ConfigureAwait(true);
        Assert.True(create.Success, create.Error);

        var snapshot = await _sut.CaptureForRestoreAsync("EF-TXN-002").ConfigureAwait(true);
        Assert.NotNull(snapshot);

        var delete = await _sut.DeleteAsync("EF-TXN-002").ConfigureAwait(true);
        Assert.True(delete.Success, delete.Error);
        Assert.Null(await _sut.GetByIdAsync("EF-TXN-002").ConfigureAwait(true));

        var restore = await _sut.RestoreAsync(snapshot!).ConfigureAwait(true);
        Assert.True(restore.Success, restore.Error);

        var restored = await _sut.GetByIdAsync("EF-TXN-002").ConfigureAwait(true);
        Assert.NotNull(restored);
        Assert.Equal("Delete rollback", restored!.Title);
        Assert.Equal(["FR-DELETE-BEFORE-001"], restored.FunctionalRequirements);
    }

    /// <summary>
    /// Phase-3 acceptance: <see cref="EfTodoService.QueryAsync"/> honors priority
    /// and keyword filters applied through the relational layer.
    /// </summary>
    [Fact]
    public async Task QueryAsync_FiltersByPriorityAndKeyword()
    {
        var createA = await _sut.CreateAsync(new TodoCreateRequest { Id = "EF-ALPHA-001", Title = "alpha widget", Section = "s", Priority = "high" }).ConfigureAwait(true);
        Assert.True(createA.Success, createA.Error);
        var createB = await _sut.CreateAsync(new TodoCreateRequest { Id = "EF-BETA-002", Title = "beta widget", Section = "s", Priority = "low" }).ConfigureAwait(true);
        Assert.True(createB.Success, createB.Error);

        var highs = await _sut.QueryAsync(new TodoQueryRequest { Priority = "high" }).ConfigureAwait(true);
        Assert.Single(highs.Items);
        Assert.Equal("EF-ALPHA-001", highs.Items[0].Id);

        var betas = await _sut.QueryAsync(new TodoQueryRequest { Keyword = "beta" }).ConfigureAwait(true);
        Assert.Single(betas.Items);
        Assert.Equal("EF-BETA-002", betas.Items[0].Id);
    }

    /// <summary>
    /// Phase-3 acceptance: <see cref="EfTodoService.DeleteAsync"/> removes the row
    /// and appends a <c>deleted</c> audit entry, making the TODO unfindable.
    /// </summary>
    [Fact]
    public async Task DeleteAsync_RemovesAndAppendsAudit()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest { Id = "EF-DEL-001", Title = "doomed", Section = "s", Priority = "low" }).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);
        var result = await _sut.DeleteAsync("EF-DEL-001").ConfigureAwait(true);
        Assert.True(result.Success, result.Error);
        Assert.Null(await _sut.GetByIdAsync("EF-DEL-001").ConfigureAwait(true));

        var audit = await _sut.GetAuditAsync("EF-DEL-001").ConfigureAwait(true);
        Assert.Contains(audit.Entries, e => e.Action == "deleted");
    }

    /// <summary>
    /// TR-MCP-TODO-005 / TR-MCP-TODO-006: EF mutations project deterministic TODO.yaml content
    /// and report a consistent projection after create, update, and delete.
    /// </summary>
    [Fact]
    public async Task CreateUpdateDelete_ProjectsYamlAndReportsConsistentStatus()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-PROJ-001",
            Title = "Before projection",
            Section = "mvp-support",
            Priority = "high",
            Description = ["Preserve this line"],
            ImplementationTasks = [new TodoFlatTask("write projection", false)],
        }).ConfigureAwait(true);

        Assert.True(created.Success, created.Error);
        Assert.True(File.Exists(_tempYamlPath));
        Assert.Contains("EF-PROJ-001", File.ReadAllText(_tempYamlPath));

        var createdStatus = await _sut.GetProjectionStatusAsync().ConfigureAwait(true);
        Assert.True(createdStatus.ProjectionConsistent, createdStatus.Message);
        Assert.False(createdStatus.RepairRequired);

        var updated = await _sut.UpdateAsync("EF-PROJ-001", new TodoUpdateRequest
        {
            Title = "After projection",
        }).ConfigureAwait(true);

        Assert.True(updated.Success, updated.Error);
        Assert.Contains("After projection", File.ReadAllText(_tempYamlPath));

        var deleted = await _sut.DeleteAsync("EF-PROJ-001").ConfigureAwait(true);
        Assert.True(deleted.Success, deleted.Error);
        Assert.DoesNotContain("EF-PROJ-001", File.ReadAllText(_tempYamlPath));

        var finalStatus = await _sut.GetProjectionStatusAsync().ConfigureAwait(true);
        Assert.True(finalStatus.ProjectionConsistent, finalStatus.Message);
        Assert.False(finalStatus.RepairRequired);
    }

    /// <summary>
    /// TR-MCP-TODO-006: EF projection status detects missing projected YAML and asks for repair.
    /// </summary>
    [Fact]
    public async Task GetProjectionStatusAsync_WhenProjectedYamlIsMissing_RequiresRepair()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-MISS-001",
            Title = "Missing projection",
            Section = "mvp-support",
            Priority = "medium",
        }).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);
        File.Delete(_tempYamlPath);

        var status = await _sut.GetProjectionStatusAsync().ConfigureAwait(true);

        Assert.False(status.ProjectionTargetExists);
        Assert.False(status.ProjectionConsistent);
        Assert.True(status.RepairRequired);
        Assert.Contains("does not exist", status.Message, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// TR-MCP-TODO-006: EF repair rebuilds a drifted TODO.yaml projection from authoritative DB rows.
    /// </summary>
    [Fact]
    public async Task RepairProjectionAsync_AfterProjectionDrift_RebuildsYaml()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-REPAIR-001",
            Title = "Repair target",
            Section = "mvp-support",
            Priority = "low",
        }).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        File.WriteAllText(_tempYamlPath, "mvp-support:\n  high-priority: []\n");
        var drifted = await _sut.GetProjectionStatusAsync().ConfigureAwait(true);
        Assert.False(drifted.ProjectionConsistent);
        Assert.True(drifted.RepairRequired);

        var repair = await _sut.RepairProjectionAsync().ConfigureAwait(true);

        Assert.True(repair.Success, repair.Error);
        Assert.True(repair.Status.ProjectionConsistent, repair.Status.Message);
        Assert.False(repair.Status.RepairRequired);
        Assert.Contains("EF-REPAIR-001", File.ReadAllText(_tempYamlPath));
    }

    /// <summary>
    /// TEST-MCP-097: EF create returns a projection-failure classification when the DB commit succeeds
    /// but TODO.yaml cannot be written, and operator repair later rebuilds the projection.
    /// </summary>
    [Fact]
    public async Task Create_WhenYamlProjectionFails_ReturnsProjectionFailureButKeepsAuthoritativeState()
    {
        Directory.CreateDirectory(_tempYamlPath);

        var result = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-FAIL-001",
            Title = "Projection failure",
            Section = "mvp-support",
            Priority = "high",
        }).ConfigureAwait(true);

        Assert.False(result.Success);
        Assert.Equal(TodoMutationFailureKind.ProjectionFailed, result.FailureKind);
        Assert.NotNull(await _sut.GetByIdAsync("EF-FAIL-001").ConfigureAwait(true));

        var failedStatus = await _sut.GetProjectionStatusAsync().ConfigureAwait(true);
        Assert.True(failedStatus.RepairRequired);
        Assert.NotNull(failedStatus.LastProjectionFailure);

        Directory.Delete(_tempYamlPath);
        var repair = await _sut.RepairProjectionAsync().ConfigureAwait(true);

        Assert.True(repair.Success, repair.Error);
        Assert.True(repair.Status.ProjectionConsistent, repair.Status.Message);
        Assert.Contains("EF-FAIL-001", File.ReadAllText(_tempYamlPath));
    }

    /// <summary>
    /// TR-MCP-TODO-005: EF projection stores code-review remediation references on the document section
    /// metadata row so the projected YAML preserves the source TODO shape.
    /// </summary>
    [Fact]
    public async Task UpdateAsync_CodeReviewPhaseReference_ProjectsCodeReviewSectionReference()
    {
        var created = await _sut.CreateAsync(new TodoCreateRequest
        {
            Id = "EF-REVIEW-001",
            Title = "Review phase",
            Section = "code-review-remediation",
            Priority = "high",
            Phase = "pass-1",
        }).ConfigureAwait(true);
        Assert.True(created.Success, created.Error);

        var updated = await _sut.UpdateAsync("EF-REVIEW-001", new TodoUpdateRequest
        {
            Reference = "docs/reviews/example.md",
        }).ConfigureAwait(true);

        Assert.True(updated.Success, updated.Error);
        var yaml = File.ReadAllText(_tempYamlPath);
        Assert.Contains("code-review-remediation", yaml);
        Assert.Contains("reference: docs/reviews/example.md", yaml);
        Assert.Contains("phase: pass-1", yaml);
    }

}
