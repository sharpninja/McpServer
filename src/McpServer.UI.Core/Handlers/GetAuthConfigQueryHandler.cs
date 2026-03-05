using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="GetAuthConfigQuery"/>.</summary>
internal sealed class GetAuthConfigQueryHandler : IQueryHandler<GetAuthConfigQuery, AuthConfigSnapshot>
{
    private readonly IAuthConfigApiClient _authConfigApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetAuthConfigQueryHandler> _logger;


    public GetAuthConfigQueryHandler(IAuthConfigApiClient authConfigApiClient, IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetAuthConfigQueryHandler> logger)
    {
        _logger = logger;
        _authConfigApiClient = authConfigApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<AuthConfigSnapshot>> HandleAsync(GetAuthConfigQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AuthConfigGet))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AuthConfigGet);
            return Result<AuthConfigSnapshot>.Failure(
                string.IsNullOrWhiteSpace(requiredRole) ? "Permission denied." : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _authConfigApiClient.GetAuthConfigAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<AuthConfigSnapshot>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AuthConfigSnapshot>.Failure(ex);
        }
    }
}
