using McpServer.TransactionSecurity.Models;
using McpServer.TransactionSecurity.Options;
using McpServer.TransactionSecurity.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-TXN-001: Fails closed for persisted GitHub token mutations while
/// required turn transactions are active.
/// </summary>
public sealed class TransactionGatedGitHubWorkspaceTokenStore : IGitHubWorkspaceTokenStore
{
    private const string DeferredGitHubTokenMutationMessage =
        "GitHub token mutations are not transaction compensated while required turn transactions are active.";

    private readonly IGitHubWorkspaceTokenStore _inner;
    private readonly ITurnTransactionCoordinator? _coordinator;
    private readonly IOptions<TurnTransactionOptions>? _transactionOptions;

    /// <summary>Initializes a new instance of the <see cref="TransactionGatedGitHubWorkspaceTokenStore"/> class.</summary>
    /// <param name="inner">Underlying GitHub workspace token store.</param>
    /// <param name="coordinator">Optional turn transaction coordinator.</param>
    /// <param name="transactionOptions">Optional transaction enforcement options.</param>
    public TransactionGatedGitHubWorkspaceTokenStore(
        IGitHubWorkspaceTokenStore inner,
        ITurnTransactionCoordinator? coordinator = null,
        IOptions<TurnTransactionOptions>? transactionOptions = null)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        _coordinator = coordinator;
        _transactionOptions = transactionOptions;
    }

    /// <inheritdoc />
    public Task<GitHubWorkspaceTokenRecord?> GetAsync(string workspacePath, CancellationToken ct = default)
        => _inner.GetAsync(workspacePath, ct);

    /// <inheritdoc />
    public Task UpsertAsync(
        string workspacePath,
        string accessToken,
        DateTimeOffset? expiresAtUtc = null,
        CancellationToken ct = default)
    {
        ThrowIfMutationBlocked();
        return _inner.UpsertAsync(workspacePath, accessToken, expiresAtUtc, ct);
    }

    /// <inheritdoc />
    public Task<bool> DeleteAsync(string workspacePath, CancellationToken ct = default)
    {
        ThrowIfMutationBlocked();
        return _inner.DeleteAsync(workspacePath, ct);
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
            throw new InvalidOperationException(DeferredGitHubTokenMutationMessage);
    }

    private bool RequiresMutationTransactions(TurnTransactionStatusResponse status)
        => status.Enabled && (_transactionOptions?.Value.RequiredForMutations ?? true);
}
