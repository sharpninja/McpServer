using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="ListContextSourcesQuery"/>.</summary>
internal sealed class ListContextSourcesQueryHandler : IQueryHandler<ListContextSourcesQuery, ContextSourcesPayload>
{
    private readonly IContextApiClient _contextApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public ListContextSourcesQueryHandler(IContextApiClient contextApiClient, IAuthorizationPolicyService authorizationPolicy)
    {
        _contextApiClient = contextApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<ContextSourcesPayload>> HandleAsync(ListContextSourcesQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ContextSources))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ContextSources);
            return Result<ContextSourcesPayload>.Failure(
                string.IsNullOrWhiteSpace(requiredRole) ? "Permission denied." : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _contextApiClient.ListSourcesAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<ContextSourcesPayload>.Success(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return Result<ContextSourcesPayload>.Failure(ex);
        }
    }
}
