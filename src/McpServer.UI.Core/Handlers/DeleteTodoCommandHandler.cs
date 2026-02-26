using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="DeleteTodoCommand"/> using the host-provided TODO API client.
/// </summary>
internal sealed class DeleteTodoCommandHandler : ICommandHandler<DeleteTodoCommand, TodoMutationOutcome>
{
    private readonly ITodoApiClient _todoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public DeleteTodoCommandHandler(ITodoApiClient todoApiClient, IAuthorizationPolicyService authorizationPolicy)
    {
        _todoApiClient = todoApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<TodoMutationOutcome>> HandleAsync(DeleteTodoCommand command, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(command.TodoId))
            return Result<TodoMutationOutcome>.Failure("TodoId is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TodoDelete))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TodoDelete);
            return Result<TodoMutationOutcome>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _todoApiClient.DeleteTodoAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<TodoMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return Result<TodoMutationOutcome>.Failure(ex);
        }
    }
}
