using System.Reflection;
using McpServer.Cqrs;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Tests.Memory;

/// <summary>
/// TEST-MCP-MEMORY-010 / TEST-MCP-MEMORY-014 / TEST-MCP-MEMORY-016:
/// Shared SQLite fixture and production-surface probes for MCP-MEMORY-002 S1 Red tests.
/// Does not implement S1 Green behavior.
/// </summary>
public sealed class MemoryS1Harness : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly DbContextOptions<McpDbContext> _options;

    /// <summary>Workspace A path used as the calling workspace.</summary>
    public string WorkspaceA { get; } = Path.Combine(Path.GetTempPath(), "mcp-s1-a-" + Guid.NewGuid().ToString("N"));

    /// <summary>Workspace B path used as a foreign workspace.</summary>
    public string WorkspaceB { get; } = Path.Combine(Path.GetTempPath(), "mcp-s1-b-" + Guid.NewGuid().ToString("N"));

    /// <summary>Creates an isolated in-memory SQLite schema.</summary>
    public MemoryS1Harness()
    {
        _connection = new SqliteConnection("Data Source=:memory:");
        _connection.Open();
        _options = new DbContextOptionsBuilder<McpDbContext>()
            .UseSqlite(_connection)
            .Options;

        using var ctx = CreateContext(WorkspaceA);
        ctx.Database.EnsureCreated();
    }

    /// <inheritdoc />
    public void Dispose() => _connection.Dispose();

    /// <summary>Creates a context bound to <paramref name="workspacePath"/>.</summary>
    public McpDbContext CreateContext(string? workspacePath)
        => new(_options, new WorkspaceContext { WorkspacePath = workspacePath });

    /// <summary>Creates the current production <see cref="MemoryService"/>.</summary>
    public static MemoryService CreateService(McpDbContext db)
        => new(db, NullLogger<MemoryService>.Instance);

    /// <summary>Creates the current production <see cref="MemoryController"/>.</summary>
    public static MemoryController CreateController(IMemoryService service)
        => new(service);

    /// <summary>
    /// Dispatches <see cref="RememberMemoryCommand"/> through a production-assembly CQRS scan.
    /// Missing handlers surface as HTTP 501 so assertions fail for missing remember behavior.
    /// </summary>
    public async Task<MemoryRememberResult> RememberAsync(
        MemoryRememberRequest? request,
        bool readOnlyCaller = false,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.SendAsync(
            new RememberMemoryCommand(WorkspaceA, request, readOnlyCaller),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryRememberResult(
            StatusCode: 501,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "memory_remember handler is not registered");
    }

    /// <summary>Lists versions through the S1 CQRS query port.</summary>
    public async Task<MemoryVersionListResult> ListVersionsAsync(
        string memoryId,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.QueryAsync(
            new ListMemoryVersionsQuery(workspacePath ?? WorkspaceA, memoryId),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryVersionListResult(
            StatusCode: 501,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "list versions handler is not registered");
    }

    /// <summary>Reverts through the S1 CQRS command port.</summary>
    public async Task<MemoryRevertResult> RevertAsync(
        string memoryId,
        int versionNumber,
        bool readOnlyCaller = false,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.SendAsync(
            new RevertMemoryCommand(WorkspaceA, memoryId, versionNumber, readOnlyCaller),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryRevertResult(
            StatusCode: 501,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "revert handler is not registered");
    }

    /// <summary>Recalls through the S1/S2 CQRS query port (used by AfterRevert).</summary>
    public async Task<MemoryQueryResult> RecallAsync(
        string query,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.QueryAsync(
            new RecallMemoryQuery(WorkspaceA, query),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryQueryResult([], 0);
    }

    /// <summary>Renders REQUIRED MEMORIES from a production injector type when present.</summary>
    public string RenderRequiredMemories(IReadOnlyList<MemoryItem> effective)
    {
        var rendererType = typeof(MemoryService).Assembly.GetTypes()
            .Concat(typeof(MemoryController).Assembly.GetTypes())
            .FirstOrDefault(type =>
                type.GetMethod("RenderRequiredMemories", BindingFlagsPublicInstanceStatic()) is not null
                || type.GetMethod("BuildRequiredMemoriesBlock", BindingFlagsPublicInstanceStatic()) is not null);
        if (rendererType is null)
            return string.Empty;

        var method = rendererType.GetMethod("RenderRequiredMemories", BindingFlagsPublicInstanceStatic())
            ?? rendererType.GetMethod("BuildRequiredMemoriesBlock", BindingFlagsPublicInstanceStatic());
        if (method is null)
            return string.Empty;

        object? instance = null;
        if (!method.IsStatic)
            instance = Activator.CreateInstance(rendererType);

        var rendered = method.Invoke(instance, [effective]);
        return rendered as string ?? string.Empty;
    }

    /// <summary>Returns the mapped EF entity type for <paramref name="clr"/> or null.</summary>
    public Microsoft.EntityFrameworkCore.Metadata.IEntityType? FindMappedEntity(Type clr)
    {
        using var ctx = CreateContext(WorkspaceA);
        return ctx.Model.FindEntityType(clr);
    }

    /// <summary>True when a unique index on (FromMemoryId, ToMemoryId, EdgeType) is mapped.</summary>
    public bool HasUniqueEdgeIndex()
    {
        var entity = FindMappedEntity(typeof(MemoryEdgeEntity));
        if (entity is null)
            return false;

        return entity.GetIndexes().Any(index =>
            index.IsUnique
            && index.Properties.Select(p => p.Name).SequenceEqual(["FromMemoryId", "ToMemoryId", "EdgeType"]));
    }

    /// <summary>Adds a compat row through the current MemoryService.</summary>
    public async Task<MemoryMutationResult> AddCompatAsync(
        MemoryAddRequest request,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = CreateContext(workspacePath ?? WorkspaceA);
        return await CreateService(db).AddAsync(request, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Lists through the current MemoryService.</summary>
    public async Task<MemoryQueryResult> ListCompatAsync(
        MemoryListRequest? request = null,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = CreateContext(workspacePath ?? WorkspaceA);
        return await CreateService(db).ListAsync(request ?? new MemoryListRequest(), cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Gets through the current MemoryService.</summary>
    public async Task<MemoryItem?> GetCompatAsync(
        string id,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = CreateContext(workspacePath ?? WorkspaceA);
        return await CreateService(db).GetAsync(id, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Updates through the current MemoryService.</summary>
    public async Task<MemoryMutationResult> UpdateCompatAsync(
        string id,
        MemoryUpdateRequest request,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = CreateContext(workspacePath ?? WorkspaceA);
        return await CreateService(db).UpdateAsync(id, request, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Removes through the current MemoryService.</summary>
    public async Task<MemoryMutationResult> RemoveCompatAsync(
        string id,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        await using var db = CreateContext(workspacePath ?? WorkspaceA);
        return await CreateService(db).RemoveAsync(id, cancellationToken).ConfigureAwait(true);
    }

    /// <summary>Maps an MVC action result to an HTTP status code.</summary>
    public static int StatusOf(IActionResult result) => result switch
    {
        ObjectResult obj => obj.StatusCode ?? 200,
        StatusCodeResult status => status.StatusCode,
        _ => 200,
    };

    private static BindingFlags BindingFlagsPublicInstanceStatic()
        => BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static;

    private IDispatcher CreateProductionDispatcher()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_options);
        services.AddCqrs(typeof(RememberMemoryCommand).Assembly, typeof(MemoryController).Assembly);
        var provider = services.BuildServiceProvider();
        return provider.GetRequiredService<IDispatcher>();
    }
}

/// <summary>
/// TEST-MCP-MEMORY-010: Port used only for mocks-first contract proof. Not a production facade.
/// </summary>
public interface IMemoryS1Port
{
    /// <summary>Remembers a multi-layer memory.</summary>
    Task<MemoryRememberResult> RememberAsync(MemoryRememberRequest? request, CancellationToken cancellationToken);

    /// <summary>Lists versions for a memory.</summary>
    Task<MemoryVersionListResult> ListVersionsAsync(string memoryId, CancellationToken cancellationToken);

    /// <summary>Reverts a memory to snapshot N.</summary>
    Task<MemoryRevertResult> RevertAsync(string memoryId, int versionNumber, CancellationToken cancellationToken);
}
