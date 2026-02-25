using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="SearchContextQuery"/>.</summary>
internal sealed class SearchContextQueryHandler : IQueryHandler<SearchContextQuery, ContextSearchPayload>
{
    private readonly IContextApiClient _contextApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public SearchContextQueryHandler(IContextApiClient contextApiClient, IAuthorizationPolicyService authorizationPolicy)
    {
        _contextApiClient = contextApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<ContextSearchPayload>> HandleAsync(SearchContextQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ContextSearch))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ContextSearch);
            return Result<ContextSearchPayload>.Failure(
                string.IsNullOrWhiteSpace(requiredRole) ? "Permission denied." : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _contextApiClient.SearchAsync(query, context.CancellationToken).ConfigureAwait(false);
            return Result<ContextSearchPayload>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<ContextSearchPayload>.Failure(ex);
        }
    }
}
