using McpServer.Cqrs;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.Tests.Memory;

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
