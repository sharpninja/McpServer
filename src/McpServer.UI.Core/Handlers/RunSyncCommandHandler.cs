using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="RunSyncCommand"/>.</summary>
internal sealed class RunSyncCommandHandler : ICommandHandler<RunSyncCommand, SyncRunSummary>
{
    private readonly ISyncApiClient _syncApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public RunSyncCommandHandler(ISyncApiClient syncApiClient, IAuthorizationPolicyService authorizationPolicy)
    {
        _syncApiClient = syncApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<SyncRunSummary>> HandleAsync(RunSyncCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.SyncRun))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.SyncRun);
            return Result<SyncRunSummary>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _syncApiClient.RunAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<SyncRunSummary>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<SyncRunSummary>.Failure(ex);
        }
    }
}
