using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="GetTodoQuery"/> using the host-provided TODO API client.
/// </summary>
internal sealed class GetTodoQueryHandler : IQueryHandler<GetTodoQuery, TodoDetail?>
{
    private readonly ITodoApiClient _todoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetTodoQueryHandler> _logger;


    public GetTodoQueryHandler(ITodoApiClient todoApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetTodoQueryHandler> logger)
    {
        _logger = logger;
        _todoApiClient = todoApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<TodoDetail?>> HandleAsync(GetTodoQuery query, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(query.TodoId))
            return Result<TodoDetail?>.Failure("TodoId is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TodoGet))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TodoGet);
            return Result<TodoDetail?>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _todoApiClient.GetTodoAsync(query.TodoId, context.CancellationToken);
            return Result<TodoDetail?>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TodoDetail?>.Failure(ex);
        }
    }
}
