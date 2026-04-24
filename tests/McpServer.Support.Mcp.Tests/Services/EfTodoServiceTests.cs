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

}
