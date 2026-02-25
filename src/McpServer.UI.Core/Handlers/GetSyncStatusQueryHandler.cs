using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="GetSyncStatusQuery"/>.</summary>
internal sealed class GetSyncStatusQueryHandler : IQueryHandler<GetSyncStatusQuery, SyncStatusSnapshot>
{
    private readonly ISyncApiClient _syncApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public GetSyncStatusQueryHandler(ISyncApiClient syncApiClient, IAuthorizationPolicyService authorizationPolicy)
    {
        _syncApiClient = syncApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<SyncStatusSnapshot>> HandleAsync(GetSyncStatusQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.SyncStatus))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.SyncStatus);
            return Result<SyncStatusSnapshot>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _syncApiClient.GetStatusAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<SyncStatusSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<SyncStatusSnapshot>.Failure(ex);
        }
    }
}
