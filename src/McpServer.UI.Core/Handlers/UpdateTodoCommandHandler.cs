using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="UpdateTodoCommand"/> using the host-provided TODO API client.
/// </summary>
internal sealed class UpdateTodoCommandHandler : ICommandHandler<UpdateTodoCommand, TodoMutationOutcome>
{
    private readonly ITodoApiClient _todoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpdateTodoCommandHandler> _logger;


    public UpdateTodoCommandHandler(ITodoApiClient todoApiClient, IAuthorizationPolicyService authorizationPolicy,
        ILogger<UpdateTodoCommandHandler> logger)
    {
        _logger = logger;
        _todoApiClient = todoApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<TodoMutationOutcome>> HandleAsync(UpdateTodoCommand command, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(command.TodoId))
            return Result<TodoMutationOutcome>.Failure("TodoId is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TodoUpdate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TodoUpdate);
            return Result<TodoMutationOutcome>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _todoApiClient.UpdateTodoAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<TodoMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TodoMutationOutcome>.Failure(ex);
        }
    }
}
