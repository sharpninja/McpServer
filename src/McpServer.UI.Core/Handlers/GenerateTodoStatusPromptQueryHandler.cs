using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles TODO status prompt generation using the host-provided TODO API client.
/// </summary>
internal sealed class GenerateTodoStatusPromptQueryHandler : IQueryHandler<GenerateTodoStatusPromptQuery, TodoPromptOutput>
{
    private readonly ITodoApiClient _todoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public GenerateTodoStatusPromptQueryHandler(
        ITodoApiClient todoApiClient,
        IAuthorizationPolicyService authorizationPolicy)
    {
        _todoApiClient = todoApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<TodoPromptOutput>> HandleAsync(GenerateTodoStatusPromptQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TodoPromptStatus))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TodoPromptStatus);
            return Result<TodoPromptOutput>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _todoApiClient.GenerateTodoStatusPromptAsync(query.TodoId, context.CancellationToken).ConfigureAwait(false);
            return Result<TodoPromptOutput>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TodoPromptOutput>.Failure(ex);
        }
    }
}
