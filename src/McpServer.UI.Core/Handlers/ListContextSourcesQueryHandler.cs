using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="ListContextSourcesQuery"/>.</summary>
internal sealed class ListContextSourcesQueryHandler : IQueryHandler<ListContextSourcesQuery, ContextSourcesPayload>
{
    private readonly IContextApiClient _contextApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListContextSourcesQueryHandler> _logger;


    public ListContextSourcesQueryHandler(IContextApiClient contextApiClient, IAuthorizationPolicyService authorizationPolicy,
        ILogger<ListContextSourcesQueryHandler> logger)
    {
        _logger = logger;
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
            var result = await _contextApiClient.ListSourcesAsync(context.CancellationToken);
            return Result<ContextSourcesPayload>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ContextSourcesPayload>.Failure(ex);
        }
    }
}
