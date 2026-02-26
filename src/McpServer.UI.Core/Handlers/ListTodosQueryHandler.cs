using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="ListTodosQuery"/> using the host-provided TODO API client.
/// </summary>
internal sealed class ListTodosQueryHandler : IQueryHandler<ListTodosQuery, ListTodosResult>
{
    private readonly ITodoApiClient _todoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListTodosQueryHandler> _logger;


    public ListTodosQueryHandler(ITodoApiClient todoApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ListTodosQueryHandler> logger)
    {
        _logger = logger;
        _todoApiClient = todoApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<ListTodosResult>> HandleAsync(ListTodosQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TodoList))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TodoList);
            return Result<ListTodosResult>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _todoApiClient.ListTodosAsync(query, context.CancellationToken).ConfigureAwait(false);
            return Result<ListTodosResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ListTodosResult>.Failure(ex);
        }
    }
}
