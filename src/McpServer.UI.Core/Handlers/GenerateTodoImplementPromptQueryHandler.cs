using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles TODO implement prompt generation using the host-provided TODO API client.
/// </summary>
internal sealed class GenerateTodoImplementPromptQueryHandler : IQueryHandler<GenerateTodoImplementPromptQuery, TodoPromptOutput>
{
    private readonly ITodoApiClient _todoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public GenerateTodoImplementPromptQueryHandler(
        ITodoApiClient todoApiClient,
        IAuthorizationPolicyService authorizationPolicy)
    {
        _todoApiClient = todoApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<TodoPromptOutput>> HandleAsync(GenerateTodoImplementPromptQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TodoPromptImplement))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TodoPromptImplement);
            return Result<TodoPromptOutput>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _todoApiClient.GenerateTodoImplementPromptAsync(query.TodoId, context.CancellationToken).ConfigureAwait(false);
            return Result<TodoPromptOutput>.Success(result);
        }
        catch (Exception ex)
        {
            return Result<TodoPromptOutput>.Failure(ex);
        }
    }
}
