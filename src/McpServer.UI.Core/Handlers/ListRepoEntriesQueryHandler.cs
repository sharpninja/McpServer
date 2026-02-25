using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="ListRepoEntriesQuery"/>.</summary>
internal sealed class ListRepoEntriesQueryHandler : IQueryHandler<ListRepoEntriesQuery, RepoListResultView>
{
    private readonly IRepoApiClient _repoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public ListRepoEntriesQueryHandler(IRepoApiClient repoApiClient, IAuthorizationPolicyService authorizationPolicy)
    {
        _repoApiClient = repoApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<RepoListResultView>> HandleAsync(ListRepoEntriesQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.RepoList))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.RepoList);
            return Result<RepoListResultView>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _repoApiClient.ListAsync(query, context.CancellationToken).ConfigureAwait(false);
            return Result<RepoListResultView>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<RepoListResultView>.Failure(ex);
        }
    }
}
