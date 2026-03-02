using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="GetSessionLogQuery"/> using the host-provided session-log API client.
/// </summary>
internal sealed class GetSessionLogQueryHandler : IQueryHandler<GetSessionLogQuery, SessionLogDetail?>
{
    private readonly ISessionLogApiClient _sessionLogApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetSessionLogQueryHandler> _logger;


    public GetSessionLogQueryHandler(ISessionLogApiClient sessionLogApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetSessionLogQueryHandler> logger)
    {
        _logger = logger;
        _sessionLogApiClient = sessionLogApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<SessionLogDetail?>> HandleAsync(GetSessionLogQuery query, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(query.SessionId))
            return Result<SessionLogDetail?>.Failure("SessionId is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.SessionLogQuery))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.SessionLogQuery);
            return Result<SessionLogDetail?>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _sessionLogApiClient.GetSessionLogAsync(query.SessionId, context.CancellationToken)
                .ConfigureAwait(false);
            return Result<SessionLogDetail?>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<SessionLogDetail?>.Failure(ex);
        }
    }
}
