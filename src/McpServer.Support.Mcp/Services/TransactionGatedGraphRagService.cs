using McpServer.Support.Mcp.Models;
using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Fails closed for GraphRAG mutations while required turn
/// transactions are active because GraphRAG DB/vector/artifact side effects are not compensated.
/// </summary>
public sealed class TransactionGatedGraphRagService : IGraphRagService
{
    private const string DeferredGraphRagMutationMessage =
        "GraphRAG mutations are not transaction compensated while required turn transactions are active.";

    private readonly IGraphRagService _inner;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedGraphRagService"/> class.</summary>
    /// <param name="inner">Underlying GraphRAG service.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction enforcement options.</param>
    public TransactionGatedGraphRagService(
        IGraphRagService inner,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<GraphRagStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
        => _inner.GetStatusAsync(cancellationToken);

    /// <inheritdoc />
    public Task<GraphRagStatusResponse> InitializeAsync(CancellationToken cancellationToken = default)
    {
        ThrowIfMutationBlocked();
        return _inner.InitializeAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<GraphRagStatusResponse> IndexAsync(
        GraphRagIndexRequest? request = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfMutationBlocked();
        return _inner.IndexAsync(request, cancellationToken);
    }

    /// <inheritdoc />
    public Task<GraphRagQueryResponse> QueryAsync(
        GraphRagQueryRequest request,
        CancellationToken cancellationToken = default)
        => _inner.QueryAsync(request, cancellationToken);

    /// <inheritdoc />
    public Task<GraphRagIngestTextResponse> IngestTextAsync(
        GraphRagIngestTextRequest request,
        CancellationToken ct = default)
    {
        ThrowIfMutationBlocked();
        return _inner.IngestTextAsync(request, ct);
    }

    /// <inheritdoc />
    public Task<GraphRagDocumentListResponse> ListDocumentsAsync(
        int skip = 0,
        int take = 50,
        string? sourceType = null,
        CancellationToken ct = default)
        => _inner.ListDocumentsAsync(skip, take, sourceType, ct);

    /// <inheritdoc />
    public Task<GraphRagDocumentChunksResponse?> GetDocumentChunksAsync(string documentId, CancellationToken ct = default)
        => _inner.GetDocumentChunksAsync(documentId, ct);

    /// <inheritdoc />
    public Task<GraphRagDocumentDeleteResponse> DeleteDocumentAsync(string documentId, CancellationToken ct = default)
    {
        ThrowIfMutationBlocked();
        return _inner.DeleteDocumentAsync(documentId, ct);
    }

    /// <inheritdoc />
    public Task<GraphEntityResponse> CreateEntityAsync(GraphEntityRequest request, CancellationToken ct = default)
    {
        ThrowIfMutationBlocked();
        return _inner.CreateEntityAsync(request, ct);
    }

    /// <inheritdoc />
    public Task<GraphEntityResponse?> GetEntityAsync(string entityId, CancellationToken ct = default)
        => _inner.GetEntityAsync(entityId, ct);

    /// <inheritdoc />
    public Task<GraphEntityResponse?> UpdateEntityAsync(
        string entityId,
        GraphEntityRequest request,
        CancellationToken ct = default)
    {
        ThrowIfMutationBlocked();
        return _inner.UpdateEntityAsync(entityId, request, ct);
    }

    /// <inheritdoc />
    public Task<GraphEntityListResponse> ListEntitiesAsync(
        int skip = 0,
        int take = 50,
        string? entityType = null,
        CancellationToken ct = default)
        => _inner.ListEntitiesAsync(skip, take, entityType, ct);

    /// <inheritdoc />
    public Task<bool> DeleteEntityAsync(string entityId, CancellationToken ct = default)
    {
        ThrowIfMutationBlocked();
        return _inner.DeleteEntityAsync(entityId, ct);
    }

    /// <inheritdoc />
    public Task<GraphRelationshipResponse> CreateRelationshipAsync(
        GraphRelationshipRequest request,
        CancellationToken ct = default)
    {
        ThrowIfMutationBlocked();
        return _inner.CreateRelationshipAsync(request, ct);
    }

    /// <inheritdoc />
    public Task<GraphRelationshipResponse?> GetRelationshipAsync(string relationshipId, CancellationToken ct = default)
        => _inner.GetRelationshipAsync(relationshipId, ct);

    /// <inheritdoc />
    public Task<GraphRelationshipResponse?> UpdateRelationshipAsync(
        string relationshipId,
        GraphRelationshipRequest request,
        CancellationToken ct = default)
    {
        ThrowIfMutationBlocked();
        return _inner.UpdateRelationshipAsync(relationshipId, request, ct);
    }

    /// <inheritdoc />
    public Task<GraphRelationshipListResponse> ListRelationshipsAsync(
        int skip = 0,
        int take = 50,
        string? entityId = null,
        string? relationshipType = null,
        CancellationToken ct = default)
        => _inner.ListRelationshipsAsync(skip, take, entityId, relationshipType, ct);

    /// <inheritdoc />
    public Task<bool> DeleteRelationshipAsync(string relationshipId, CancellationToken ct = default)
    {
        ThrowIfMutationBlocked();
        return _inner.DeleteRelationshipAsync(relationshipId, ct);
    }

    private void ThrowIfMutationBlocked()
    {
        if (_coordinator is null)
            return;

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            throw new InvalidOperationException(
                string.IsNullOrWhiteSpace(status.Message)
                    ? "Turn transaction coordinator is degraded."
                    : status.Message);
        }

        if (RequiresMutationTransactions(status))
            throw new InvalidOperationException(DeferredGraphRagMutationMessage);
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);
}
