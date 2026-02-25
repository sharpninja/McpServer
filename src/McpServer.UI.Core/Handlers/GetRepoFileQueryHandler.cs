using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="GetRepoFileQuery"/>.</summary>
internal sealed class GetRepoFileQueryHandler : IQueryHandler<GetRepoFileQuery, RepoFileDetail>
{
    private readonly IRepoApiClient _repoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public GetRepoFileQueryHandler(IRepoApiClient repoApiClient, IAuthorizationPolicyService authorizationPolicy)
    {
        _repoApiClient = repoApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<RepoFileDetail>> HandleAsync(GetRepoFileQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.RepoRead))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.RepoRead);
            return Result<RepoFileDetail>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _repoApiClient.ReadFileAsync(query.Path, context.CancellationToken).ConfigureAwait(false);
            return Result<RepoFileDetail>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<RepoFileDetail>.Failure(ex);
        }
    }
}
