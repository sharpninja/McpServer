using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="ListSessionLogsQuery"/> using the host-provided session-log API client.
/// </summary>
internal sealed class ListSessionLogsQueryHandler : IQueryHandler<ListSessionLogsQuery, ListSessionLogsResult>
{
    private readonly ISessionLogApiClient _sessionLogApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListSessionLogsQueryHandler> _logger;


    public ListSessionLogsQueryHandler(ISessionLogApiClient sessionLogApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ListSessionLogsQueryHandler> logger)
    {
        _logger = logger;
        _sessionLogApiClient = sessionLogApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<ListSessionLogsResult>> HandleAsync(ListSessionLogsQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.SessionLogQuery))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.SessionLogQuery);
            return Result<ListSessionLogsResult>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _sessionLogApiClient.ListSessionLogsAsync(query, context.CancellationToken).ConfigureAwait(false);
            return Result<ListSessionLogsResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ListSessionLogsResult>.Failure(ex);
        }
    }
}
