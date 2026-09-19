using System.Reflection;
using McpServer.Cqrs;
using McpServer.Support.Mcp.Controllers;
using McpServer.Support.Mcp.Services;
using McpServer.Support.Mcp.Storage;
using McpServer.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace McpServer.Support.Mcp.Tests.Memory;

public sealed partial class MemoryS1Harness
{
    /// <summary>Test-only fixture helper to plant a directed edge without the S3 create-edge API.</summary>
    public async Task InsertEdgeFixtureAsync(
        string fromMemoryId,
        string toMemoryId,
        string edgeType,
        double weight = 1,
        string? workspacePath = null)
    {
        await using var db = CreateContext(workspacePath ?? WorkspaceA);
        db.MemoryEdges.Add(new MemoryEdgeEntity
        {
            FromMemoryId = fromMemoryId,
            ToMemoryId = toMemoryId,
            EdgeType = edgeType,
            Weight = weight,
        });
        await db.SaveChangesAsync().ConfigureAwait(true);
    }

    /// <summary>Reads EmbeddingStatus for a memory, including soft-deleted rows.</summary>
    public async Task<string?> GetEmbeddingStatusAsync(string memoryId, string? workspacePath = null)
    {
        await using var db = CreateContext(workspacePath ?? WorkspaceA);
        var entity = await db.Memories
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(memory => memory.Id == memoryId)
            .ConfigureAwait(true);
        return entity?.EmbeddingStatus;
    }

    /// <summary>Test-only fixture helper to stamp EmbeddingStatus without an indexer.</summary>
    public async Task SetEmbeddingStatusAsync(string memoryId, string? status, string? workspacePath = null)
    {
        await using var db = CreateContext(workspacePath ?? WorkspaceA);
        var entity = await db.Memories
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(memory => memory.Id == memoryId)
            .ConfigureAwait(true);
        if (entity is null)
            return;

        entity.EmbeddingStatus = status;
        await db.SaveChangesAsync().ConfigureAwait(true);
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
/// TEST-MCP-MEMORY-012: Port used only for S3 mocks-first contract proof. Not a production facade.
/// </summary>
public interface IMemoryS3Port
{
    /// <summary>Walks the neighborhood of a seed memory or query.</summary>
    Task<MemoryExploreResult> ExploreAsync(MemoryExploreRequest request, CancellationToken cancellationToken);

    /// <summary>Creates a directed explicit edge.</summary>
    Task<MemoryCreateEdgeResult> CreateEdgeAsync(MemoryCreateEdgeRequest request, CancellationToken cancellationToken);

    /// <summary>Strengthens co-retrieved edges when Hebbian is enabled.</summary>
    Task<MemoryHebbianResult> RecordHebbianAsync(
        IReadOnlyList<string> memoryIds,
        bool hebbianEnabled,
        CancellationToken cancellationToken);
}

/// <summary>
/// TEST-MCP-MEMORY-011: Port used only for S2 mocks-first contract proof. Not a production facade.
/// </summary>
public interface IMemoryS2Port
{
    /// <summary>Recalls ranked memories.</summary>
    Task<MemoryRecallResult> RecallAsync(MemoryRecallRequest request, CancellationToken cancellationToken);

    /// <summary>Indexes one memory.</summary>
    Task<MemoryIndexResult> IndexAsync(string memoryId, CancellationToken cancellationToken);

    /// <summary>Reconciles stale EmbeddingStatus.</summary>
    Task<MemoryIndexResult> ReconcileAsync(CancellationToken cancellationToken);

    /// <summary>Batch-indexes memories to a terminal status.</summary>
    Task<MemoryIndexResult> BatchIndexAsync(IReadOnlyList<string> memoryIds, CancellationToken cancellationToken);
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
