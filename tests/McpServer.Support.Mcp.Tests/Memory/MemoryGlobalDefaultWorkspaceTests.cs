using McpServer.Cqrs;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// FR-MCP-MEMORY-001: Global memory create from an empty or configured default workspace context.
/// </summary>
public sealed class MemoryGlobalDefaultWorkspaceTests : IDisposable
{
    private readonly MemoryGlobalWorkspaceFixture _gate = MemoryGlobalWorkspaceFixture.CreateAsync().GetAwaiter().GetResult();

    /// <summary>Releases the shared SQLite connection.</summary>
    public void Dispose() => _gate.Dispose();

    /// <summary>
    /// Global add stores Scope=Global and WorkspaceId=null when the active workspace is empty.
    /// Workspace scope in that same context is rejected.
    /// </summary>
    [Fact]
    public async Task MemoryService_GlobalCreate_EmptyWorkspace_StoresNullOwner()
    {
        await using var db = _gate.OpenContext(string.Empty);
        var service = new MemoryService(db, NullLogger<MemoryService>.Instance);

        var created = await service.AddAsync(new MemoryAddRequest
        {
            Category = "global",
            Scope = MemoryScope.Global,
            Text = "global while workspace context is empty",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var rejected = await service.AddAsync(new MemoryAddRequest
        {
            Category = "local",
            Scope = MemoryScope.Workspace,
            Text = "workspace without an owner",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(created.Success, created.Error);
        Assert.Equal(MemoryScope.Global, created.Memory!.Scope);
        Assert.Null(created.Memory.WorkspacePath);
        Assert.False(rejected.Success);
        Assert.Equal("Workspace memory requires an active workspace.", rejected.Error);

        var row = await db.Memories.IgnoreQueryFilters()
            .SingleAsync(memory => memory.Id == created.Memory.Id, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(MemoryEntity.GlobalScope, row.Scope);
        Assert.Null(row.WorkspaceId);
    }

    /// <summary>
    /// Global add from the configured default workspace still stores WorkspaceId=null.
    /// Workspace scope on that same path keeps the default workspace as owner.
    /// </summary>
    [Fact]
    public async Task MemoryService_GlobalCreate_DefaultWorkspace_StoresNullOwner()
    {
        await using var db = _gate.OpenContext(_gate.DefaultWorkspacePath);
        var service = new MemoryService(db, NullLogger<MemoryService>.Instance);

        var created = await service.AddAsync(new MemoryAddRequest
        {
            Category = "default",
            Scope = MemoryScope.Global,
            Text = "global inside the default workspace",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var owned = await service.AddAsync(new MemoryAddRequest
        {
            Category = "default",
            Scope = MemoryScope.Workspace,
            Text = "workspace memory in the default workspace",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        Assert.True(created.Success, created.Error);
        Assert.Null(created.Memory!.WorkspacePath);
        Assert.True(owned.Success, owned.Error);
        Assert.Equal(_gate.DefaultWorkspacePath, owned.Memory!.WorkspacePath);

        var globalRow = await db.Memories.IgnoreQueryFilters()
            .SingleAsync(memory => memory.Id == created.Memory.Id, TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(MemoryEntity.GlobalScope, globalRow.Scope);
        Assert.Null(globalRow.WorkspaceId);
    }

    /// <summary>
    /// POST /mcpserver/memory and remember both persist a Global row when WorkspaceContext is empty.
    /// </summary>
    [Fact]
    public async Task Controller_GlobalCreate_EmptyWorkspace_ReturnsCreated()
    {
        await using var db = _gate.OpenContext(null);
        var service = new MemoryService(db, NullLogger<MemoryService>.Instance);
        var workspace = new WorkspaceContext { WorkspacePath = string.Empty };
        var controller = new MemoryController(service, dispatcher: _gate.Dispatcher, workspaceContext: workspace);

        var add = await controller.AddAsync(new MemoryAddRequest
        {
            Category = "http",
            Scope = MemoryScope.Global,
            Text = "http global from empty workspace",
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);
        var remember = await controller.RememberAsync(new MemoryRememberRequest
        {
            Content = "http remember from empty workspace",
            Type = "fact",
            Scope = MemoryScope.Global,
        }, TestContext.Current.CancellationToken).ConfigureAwait(true);

        var created = Assert.IsType<CreatedResult>(add.Result);
        var added = Assert.IsType<MemoryMutationResult>(created.Value);
        Assert.True(added.Success, added.Error);
        Assert.Null(added.Memory!.WorkspacePath);

        var remembered = Assert.IsType<ObjectResult>(remember.Result);
        Assert.Equal(201, remembered.StatusCode);
        var rememberBody = Assert.IsType<MemoryRememberResult>(remembered.Value);
        Assert.Equal(MemoryScope.Global, rememberBody.Memory!.Scope);
        Assert.Null(rememberBody.Memory.WorkspacePath);

        await using var verify = _gate.OpenContext(string.Empty);
        var rows = await verify.Memories.IgnoreQueryFilters()
            .Where(memory => memory.Id == added.Memory.Id || memory.Id == rememberBody.MemoryId)
            .ToListAsync(TestContext.Current.CancellationToken)
            .ConfigureAwait(true);
        Assert.Equal(2, rows.Count);
        Assert.All(rows, row =>
        {
            Assert.Equal(MemoryEntity.GlobalScope, row.Scope);
            Assert.Null(row.WorkspaceId);
        });
    }
}

/// <summary>SQLite fixture shared by Global-memory default-workspace tests.</summary>
public sealed class MemoryGlobalWorkspaceFixture : IAsyncDisposable, IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly ServiceProvider _provider;

    private MemoryGlobalWorkspaceFixture(
        SqliteConnection connection,
        DbContextOptions<McpDbContext> options,
        ServiceProvider provider,
        McpDbContext db,
        MemoryService service,
        IDispatcher dispatcher,
        string defaultWorkspacePath)
    {
        _connection = connection;
        _provider = provider;
        Options = options;
        Db = db;
        Service = service;
        Dispatcher = dispatcher;
        DefaultWorkspacePath = defaultWorkspacePath;
    }

    /// <summary>SQLite options shared with the CQRS remember handler.</summary>
    public DbContextOptions<McpDbContext> Options { get; }

    /// <summary>Context used by MCP tool tests. Current workspace id starts empty.</summary>
    public McpDbContext Db { get; }

    /// <summary>Memory service bound to <see cref="Db"/>.</summary>
    public MemoryService Service { get; }

    /// <summary>Production CQRS dispatcher used by remember.</summary>
    public IDispatcher Dispatcher { get; }

    /// <summary>Stand-in for the configured default workspace path.</summary>
    public string DefaultWorkspacePath { get; }

    /// <summary>Opens a schema and dispatcher against one in-memory SQLite database.</summary>
    public static async Task<MemoryGlobalWorkspaceFixture> CreateAsync()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync().ConfigureAwait(false);
        var options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(connection)
            .Options;
        var db = new McpDbContext(options, new WorkspaceContext { WorkspacePath = string.Empty });
        await db.Database.EnsureCreatedAsync().ConfigureAwait(false);

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(options);
        services.AddCqrs(typeof(RememberMemoryCommand).Assembly, typeof(MemoryController).Assembly);
        var provider = services.BuildServiceProvider();
        return new MemoryGlobalWorkspaceFixture(
            connection,
            options,
            provider,
            db,
            new MemoryService(db, NullLogger<MemoryService>.Instance),
            provider.GetRequiredService<IDispatcher>(),
            Path.Combine(Path.GetTempPath(), "mcp-default-workspace"));
    }

    /// <summary>Opens another context on the same database for the given workspace path.</summary>
    public McpDbContext OpenContext(string? workspacePath)
        => new(Options, new WorkspaceContext { WorkspacePath = workspacePath });

    /// <inheritdoc />
    public void Dispose()
    {
        Db.Dispose();
        _provider.Dispose();
        _connection.Dispose();
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await Db.DisposeAsync().ConfigureAwait(false);
        await _provider.DisposeAsync().ConfigureAwait(false);
        await _connection.DisposeAsync().ConfigureAwait(false);
    }
}
