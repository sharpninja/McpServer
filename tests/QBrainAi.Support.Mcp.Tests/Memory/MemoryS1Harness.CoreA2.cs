using QBrainAi.Cqrs;
using QBrainAi.Support.Mcp.Services;

namespace QBrainAi.Support.Mcp.Tests.Memory;

/// <summary>
/// No requirement id applies: this part of MemoryS1Harness holds the S2 and S3 dispatch helpers IndexAsync,
/// ReconcileIndexAsync, BatchIndexAsync, ExploreAsync, CreateEdgeAsync and RecordHebbianAsync; each sends its memory
/// command or query through the production CQRS dispatcher (CreateProductionDispatcher, in MemoryS1Harness.CoreB.cs)
/// for workspace A unless another workspace is given, and returns a 501 result carrying the dispatch error when no
/// handler answers (IndexAsync also reports the stored EmbeddingStatus). The memory tests that build a MemoryS1Harness
/// use them.
/// </summary>
/// <remarks>2026-11-07, no requirement id applies: documented in slice S9 of hostile validation finding R14-F4, because it had no XML documentation (CLAUDE.md: what is tested, the data or fixtures used, the requirement ids validated); the code is unchanged.</remarks>
public sealed partial class MemoryS1Harness
{
    /// <summary>Indexes one memory through the S2 CQRS command port when a handler is registered.</summary>
    public async Task<MemoryIndexResult> IndexAsync(
        string memoryId,
        string? workspacePath = null,
        string? provider = null,
        bool cloudEnabled = false,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.SendAsync(
            new IndexMemoryCommand(workspacePath ?? WorkspaceA, memoryId, provider, cloudEnabled),
            cancellationToken).ConfigureAwait(true);
        var status = await GetEmbeddingStatusAsync(memoryId, workspacePath).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryIndexResult(
            StatusCode: 501,
            MemoryId: memoryId,
            EmbeddingStatus: status,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "memory index handler is not registered");
    }

    /// <summary>Reconciles stale EmbeddingStatus through the S2 CQRS command port.</summary>
    public async Task<MemoryIndexResult> ReconcileIndexAsync(
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.SendAsync(
            new ReconcileMemoryIndexCommand(workspacePath ?? WorkspaceA),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryIndexResult(
            StatusCode: 501,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "memory index reconcile handler is not registered");
    }

    /// <summary>Batch-indexes memories through the S2 CQRS command port.</summary>
    public async Task<MemoryIndexResult> BatchIndexAsync(
        IReadOnlyList<string>? memoryIds = null,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.SendAsync(
            new BatchIndexMemoriesCommand(workspacePath ?? WorkspaceA, memoryIds),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryIndexResult(
            StatusCode: 501,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "memory batch index handler is not registered");
    }

    /// <summary>Explores through the S3 CQRS query port when a handler is registered.</summary>
    public async Task<MemoryExploreResult> ExploreAsync(
        MemoryExploreRequest request,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.QueryAsync(
            new ExploreMemoryQuery(
                workspacePath ?? WorkspaceA,
                request.SeedId,
                request.Query,
                request.Depth,
                request.MaxNeighbors,
                request.HebbianEnabled),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryExploreResult(
            StatusCode: 501,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "memory_explore handler is not registered");
    }

    /// <summary>Creates a directed edge through the S3 CQRS command port when a handler is registered.</summary>
    public async Task<MemoryCreateEdgeResult> CreateEdgeAsync(
        MemoryCreateEdgeRequest? request,
        string? workspacePath = null,
        bool readOnlyCaller = false,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.SendAsync(
            new CreateMemoryEdgeCommand(workspacePath ?? WorkspaceA, request, readOnlyCaller),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryCreateEdgeResult(
            StatusCode: 501,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "memory create-edge handler is not registered");
    }

    /// <summary>Records a Hebbian co-retrieval through the S3 CQRS command port when a handler is registered.</summary>
    public async Task<MemoryHebbianResult> RecordHebbianAsync(
        IReadOnlyList<string> memoryIds,
        bool hebbianEnabled = true,
        string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        var dispatcher = CreateProductionDispatcher();
        var dispatched = await dispatcher.SendAsync(
            new RecordHebbianCoRetrievalCommand(workspacePath ?? WorkspaceA, memoryIds, hebbianEnabled),
            cancellationToken).ConfigureAwait(true);
        if (dispatched.IsSuccess && dispatched.Value is not null)
            return dispatched.Value;

        return new MemoryHebbianResult(
            StatusCode: 501,
            FailureKind: MemoryMutationFailureKind.None,
            Error: dispatched.Error ?? dispatched.Exception?.Message ?? "memory Hebbian handler is not registered");
    }
}
