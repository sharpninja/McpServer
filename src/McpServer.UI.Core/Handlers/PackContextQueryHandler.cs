using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="PackContextQuery"/>.</summary>
internal sealed class PackContextQueryHandler : IQueryHandler<PackContextQuery, ContextPackPayload>
{
    private readonly IContextApiClient _contextApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public PackContextQueryHandler(IContextApiClient contextApiClient, IAuthorizationPolicyService authorizationPolicy)
    {
        _contextApiClient = contextApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<ContextPackPayload>> HandleAsync(PackContextQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ContextPack))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ContextPack);
            return Result<ContextPackPayload>.Failure(
                string.IsNullOrWhiteSpace(requiredRole) ? "Permission denied." : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _contextApiClient.PackAsync(query, context.CancellationToken).ConfigureAwait(false);
            return Result<ContextPackPayload>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<ContextPackPayload>.Failure(ex);
        }
    }
}
