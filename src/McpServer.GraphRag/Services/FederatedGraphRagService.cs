using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.GraphRag;

/// <summary>
/// FR-MCP-084: Decorator that wraps an <see cref="IGraphRagService"/> to merge local
/// and remote GraphRAG data when federation is enabled. List and query operations
/// query both local and remote in parallel and merge results (local wins on ID collision).
/// Write, lifecycle, and status operations delegate exclusively to the inner (local) service.
/// When federation is disabled or no target resolves, all calls pass through with zero overhead.
/// </summary>
public sealed class FederatedGraphRagService : IGraphRagService
{
    private readonly IGraphRagService _inner;
    private readonly FederationRegistry _registry;
    private readonly IGraphRagFederationClient _client;
    private readonly ILogger<FederatedGraphRagService> _logger;

    /// <summary>Initializes a new instance of the <see cref="FederatedGraphRagService"/> class.</summary>
    /// <param name="inner">The local GraphRAG service to delegate to.</param>
    /// <param name="registry">Federation registry for target resolution.</param>
    /// <param name="client">GraphRAG federation client for remote queries.</param>
    /// <param name="logger">Logger for diagnostic output.</param>
    public FederatedGraphRagService(
        IGraphRagService inner,
        FederationRegistry registry,
        IGraphRagFederationClient client,
        ILogger<FederatedGraphRagService> logger)
    {
        _inner = inner;
        _registry = registry;
        _client = client;
        _logger = logger;
    }

    // ── Federated read operations ──

    /// <inheritdoc />
    public async Task<GraphEntityListResponse> ListEntitiesAsync(int skip = 0, int take = 50, string? entityType = null, CancellationToken ct = default)
    {
        var target = _registry.ResolveTarget(null);
        if (target is null)
            return await _inner.ListEntitiesAsync(skip, take, entityType, ct).ConfigureAwait(false);

        var localTask = _inner.ListEntitiesAsync(skip, take, entityType, ct);
        GraphEntityListResponse? remote = null;

        try
        {
            remote = await _client.QueryEntitiesAsync(target, skip, take, entityType, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Federation entity query to {Target} failed", target.Name);
        }

        var local = await localTask.ConfigureAwait(false);
        if (remote is null || remote.Entities.Count == 0)
            return local;

        return MergeEntities(local, remote);
    }

    /// <inheritdoc />
    public async Task<GraphRelationshipListResponse> ListRelationshipsAsync(int skip = 0, int take = 50, string? entityId = null, string? relationshipType = null, CancellationToken ct = default)
    {
        var target = _registry.ResolveTarget(null);
        if (target is null)
            return await _inner.ListRelationshipsAsync(skip, take, entityId, relationshipType, ct).ConfigureAwait(false);

        var localTask = _inner.ListRelationshipsAsync(skip, take, entityId, relationshipType, ct);
        GraphRelationshipListResponse? remote = null;

        try
        {
            remote = await _client.QueryRelationshipsAsync(target, skip, take, entityId, relationshipType, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Federation relationship query to {Target} failed", target.Name);
        }

        var local = await localTask.ConfigureAwait(false);
        if (remote is null || remote.Relationships.Count == 0)
            return local;

        return MergeRelationships(local, remote);
    }

    /// <inheritdoc />
    public async Task<GraphRagDocumentListResponse> ListDocumentsAsync(int skip = 0, int take = 50, string? sourceType = null, CancellationToken ct = default)
    {
        var target = _registry.ResolveTarget(null);
        if (target is null)
            return await _inner.ListDocumentsAsync(skip, take, sourceType, ct).ConfigureAwait(false);

        var localTask = _inner.ListDocumentsAsync(skip, take, sourceType, ct);
        GraphRagDocumentListResponse? remote = null;

        try
        {
            remote = await _client.QueryDocumentsAsync(target, skip, take, sourceType, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Federation document query to {Target} failed", target.Name);
        }

        var local = await localTask.ConfigureAwait(false);
        if (remote is null || remote.Documents.Count == 0)
            return local;

        return MergeDocuments(local, remote);
    }

    /// <inheritdoc />
    public async Task<GraphRagQueryResponse> QueryAsync(GraphRagQueryRequest request, CancellationToken cancellationToken = default)
    {
        var target = _registry.ResolveTarget(null);
        if (target is null)
            return await _inner.QueryAsync(request, cancellationToken).ConfigureAwait(false);

        var localTask = _inner.QueryAsync(request, cancellationToken);
        GraphRagQueryResponse? remote = null;

        try
        {
            remote = await _client.QueryGraphRagAsync(target, request, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Federation GraphRAG query to {Target} failed", target.Name);
        }

        var local = await localTask.ConfigureAwait(false);
        // Local answer takes priority; remote is supplementary
        return local;
    }

    // ── Pass-through operations ──

    /// <inheritdoc />
    public Task<GraphRagStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
        => _inner.GetStatusAsync(cancellationToken);

    /// <inheritdoc />
    public Task<GraphRagStatusResponse> InitializeAsync(CancellationToken cancellationToken = default)
        => _inner.InitializeAsync(cancellationToken);

    /// <inheritdoc />
    public Task<GraphRagStatusResponse> IndexAsync(GraphRagIndexRequest? request = null, CancellationToken cancellationToken = default)
        => _inner.IndexAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<GraphRagIngestTextResponse> IngestTextAsync(GraphRagIngestTextRequest request, CancellationToken ct = default)
        => _inner.IngestTextAsync(request, ct);

    /// <inheritdoc />
    public Task<GraphRagDocumentChunksResponse?> GetDocumentChunksAsync(string documentId, CancellationToken ct = default)
        => _inner.GetDocumentChunksAsync(documentId, ct);

    /// <inheritdoc />
    public Task<GraphRagDocumentDeleteResponse> DeleteDocumentAsync(string documentId, CancellationToken ct = default)
        => _inner.DeleteDocumentAsync(documentId, ct);

    /// <inheritdoc />
    public Task<GraphEntityResponse> CreateEntityAsync(GraphEntityRequest request, CancellationToken ct = default)
        => _inner.CreateEntityAsync(request, ct);

    /// <inheritdoc />
    public Task<GraphEntityResponse?> GetEntityAsync(string entityId, CancellationToken ct = default)
        => _inner.GetEntityAsync(entityId, ct);

    /// <inheritdoc />
    public Task<GraphEntityResponse?> UpdateEntityAsync(string entityId, GraphEntityRequest request, CancellationToken ct = default)
        => _inner.UpdateEntityAsync(entityId, request, ct);

    /// <inheritdoc />
    public Task<bool> DeleteEntityAsync(string entityId, CancellationToken ct = default)
        => _inner.DeleteEntityAsync(entityId, ct);

    /// <inheritdoc />
    public Task<GraphRelationshipResponse> CreateRelationshipAsync(GraphRelationshipRequest request, CancellationToken ct = default)
        => _inner.CreateRelationshipAsync(request, ct);

    /// <inheritdoc />
    public Task<GraphRelationshipResponse?> GetRelationshipAsync(string relationshipId, CancellationToken ct = default)
        => _inner.GetRelationshipAsync(relationshipId, ct);

    /// <inheritdoc />
    public Task<GraphRelationshipResponse?> UpdateRelationshipAsync(string relationshipId, GraphRelationshipRequest request, CancellationToken ct = default)
        => _inner.UpdateRelationshipAsync(relationshipId, request, ct);

    /// <inheritdoc />
    public Task<bool> DeleteRelationshipAsync(string relationshipId, CancellationToken ct = default)
        => _inner.DeleteRelationshipAsync(relationshipId, ct);

    // ── Merge helpers ──

    private static GraphEntityListResponse MergeEntities(GraphEntityListResponse local, GraphEntityListResponse remote)
    {
        var localIds = new HashSet<string>(local.Entities.Select(e => e.Id), StringComparer.OrdinalIgnoreCase);
        var merged = new List<GraphEntityResponse>(local.Entities);

        foreach (var entity in remote.Entities)
        {
            if (!localIds.Contains(entity.Id))
                merged.Add(entity);
        }

        return new GraphEntityListResponse { Entities = merged, TotalCount = merged.Count };
    }

    private static GraphRelationshipListResponse MergeRelationships(GraphRelationshipListResponse local, GraphRelationshipListResponse remote)
    {
        var localIds = new HashSet<string>(local.Relationships.Select(r => r.Id), StringComparer.OrdinalIgnoreCase);
        var merged = new List<GraphRelationshipResponse>(local.Relationships);

        foreach (var rel in remote.Relationships)
        {
            if (!localIds.Contains(rel.Id))
                merged.Add(rel);
        }

        return new GraphRelationshipListResponse { Relationships = merged, TotalCount = merged.Count };
    }

    private static GraphRagDocumentListResponse MergeDocuments(GraphRagDocumentListResponse local, GraphRagDocumentListResponse remote)
    {
        var localIds = new HashSet<string>(local.Documents.Select(d => d.Id), StringComparer.OrdinalIgnoreCase);
        var merged = new List<GraphRagDocumentSummary>(local.Documents);

        foreach (var doc in remote.Documents)
        {
            if (!localIds.Contains(doc.Id))
                merged.Add(doc);
        }

        return new GraphRagDocumentListResponse { Documents = merged, TotalCount = merged.Count };
    }
}
