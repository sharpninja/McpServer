using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="GetTodoQuery"/> using the host-provided TODO API client.
/// </summary>
internal sealed class GetTodoQueryHandler : IQueryHandler<GetTodoQuery, TodoDetail?>
{
    private readonly ITodoApiClient _todoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public GetTodoQueryHandler(
        ITodoApiClient todoApiClient,
        IAuthorizationPolicyService authorizationPolicy)
    {
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
            var result = await _todoApiClient.GetTodoAsync(query.TodoId, context.CancellationToken).ConfigureAwait(false);
            return Result<TodoDetail?>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TodoDetail?>.Failure(ex);
        }
    }
}
