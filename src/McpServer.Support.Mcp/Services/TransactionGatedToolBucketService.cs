using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Fails closed for composite tool bucket mutations while
/// required turn transactions are active.
/// </summary>
public sealed class TransactionGatedToolBucketService : IToolBucketService
{
    private const string DeferredBucketMutationMessage =
        "Tool bucket mutations are deferred while required turn transactions are active; bucket/GitHub compensation is not implemented for this slice.";

    private readonly IToolBucketService _inner;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedToolBucketService"/> class.</summary>
    /// <param name="inner">Underlying bucket service.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction enforcement options.</param>
    public TransactionGatedToolBucketService(
        IToolBucketService inner,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<BucketListResult> ListBucketsAsync(CancellationToken ct = default)
        => _inner.ListBucketsAsync(ct);

    /// <inheritdoc />
    public Task<BucketBrowseResult> BrowseAsync(string bucketName, CancellationToken ct = default)
        => _inner.BrowseAsync(bucketName, ct);

    /// <inheritdoc />
    public Task<BucketMutationResult> AddBucketAsync(BucketAddRequest request, CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new BucketMutationResult(false, error));

        return _inner.AddBucketAsync(request, ct);
    }

    /// <inheritdoc />
    public Task<BucketMutationResult> RemoveBucketAsync(string bucketName, bool uninstallTools = false, CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new BucketMutationResult(false, error));

        return _inner.RemoveBucketAsync(bucketName, uninstallTools, ct);
    }

    /// <inheritdoc />
    public Task<ToolMutationResult> InstallAsync(string bucketName, string toolName, string? workspacePath = null, CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new ToolMutationResult(false, error));

        return _inner.InstallAsync(bucketName, toolName, workspacePath, ct);
    }

    /// <inheritdoc />
    public Task<BucketSyncResult> SyncAsync(string bucketName, CancellationToken ct = default)
    {
        if (ShouldDeferMutation(out var error))
            return Task.FromResult(new BucketSyncResult(false, error));

        return _inner.SyncAsync(bucketName, ct);
    }

    private bool ShouldDeferMutation(out string error)
    {
        error = string.Empty;
        if (_coordinator is null)
            return false;

        var status = _coordinator.GetStatus();
        if (status.Degraded)
        {
            error = string.IsNullOrWhiteSpace(status.Message)
                ? "Turn transaction coordinator is degraded."
                : status.Message;
            return true;
        }

        if (!RequiresMutationTransactions(status))
            return false;

        error = DeferredBucketMutationMessage;
        return true;
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);
}
