using System.Reflection;
using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Controllers;
using QBrainAi.Support.Mcp.Services;
using QBrainAi.Support.Mcp.Storage;
using QBrainAi.Support.Mcp.Storage.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// FR-MCP-MEMORY-002; FR-MCP-MEMORY-003; TR-MCP-MEMORY-003; TR-MCP-USECASE-018 AC-006: this part of MemoryS1Harness
/// holds the fixture and probe helpers: InsertEdgeFixtureAsync, GetEmbeddingStatusAsync and SetEmbeddingStatusAsync
/// over the harness database; RenderRequiredMemories, which finds a production REQUIRED MEMORIES renderer by
/// reflection; FindMappedEntity and HasUniqueEdgeIndex over the EF model; the MemoryService passthroughs
/// AddCompatAsync, ListCompatAsync, GetCompatAsync, UpdateCompatAsync and RemoveCompatAsync; StatusOf for MVC results;
/// the Clock a test may set for the handlers; and CreateProductionDispatcher, which every dispatch helper of the
/// harness uses. The memory tests that build a MemoryS1Harness use them.
/// </summary>
/// <remarks>2026-11-07, FR-MCP-MEMORY-002; FR-MCP-MEMORY-003; TR-MCP-MEMORY-003; TR-MCP-USECASE-018 AC-006: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the code is unchanged.</remarks>
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

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006 (appended 2026-11-05): the clock the production handlers read when a test sets it; null
    /// leaves every handler on its default clock (hostile validation round 12 finding R12-F2, sibling S37).
    /// </summary>
    internal TimeProvider? Clock { get; set; }

    /// <summary>
    /// TR-MCP-USECASE-018 AC-006: a CQRS dispatcher over the production memory handlers, with logging, the harness's
    /// options and, when <see cref="Clock"/> is set, that clock registered for the handlers to read.
    /// </summary>
    /// <returns>The dispatcher.</returns>
    /// <remarks>2026-11-05, TR-MCP-USECASE-018 AC-006: this entry documents the helper, changed in slice Z4 to register <see cref="Clock"/> when a test sets it (hostile validation round 12 finding R12-F2, sibling S37), because the tenth holistic read before hostile validation round 13 (finding d1; MEMORY-FACT-008) found it without XML documentation.</remarks>
    private IDispatcher CreateProductionDispatcher()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton(_options);
        if (Clock is not null)
            services.AddSingleton(Clock);
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
