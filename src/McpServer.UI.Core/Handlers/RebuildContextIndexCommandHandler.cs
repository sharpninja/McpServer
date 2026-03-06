using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="RebuildContextIndexCommand"/>.</summary>
internal sealed class RebuildContextIndexCommandHandler : ICommandHandler<RebuildContextIndexCommand, ContextRebuildResult>
{
    private readonly IContextApiClient _contextApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<RebuildContextIndexCommandHandler> _logger;


    public RebuildContextIndexCommandHandler(IContextApiClient contextApiClient, IAuthorizationPolicyService authorizationPolicy,
        ILogger<RebuildContextIndexCommandHandler> logger)
    {
        _logger = logger;
        _contextApiClient = contextApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<ContextRebuildResult>> HandleAsync(RebuildContextIndexCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.ContextRebuildIndex))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.ContextRebuildIndex);
            return Result<ContextRebuildResult>.Failure(
                string.IsNullOrWhiteSpace(requiredRole) ? "Permission denied." : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _contextApiClient.RebuildIndexAsync(context.CancellationToken).ConfigureAwait(true);
            return Result<ContextRebuildResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ContextRebuildResult>.Failure(ex);
        }
    }
}
