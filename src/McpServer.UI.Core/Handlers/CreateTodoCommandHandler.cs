using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="CreateTodoCommand"/> using the host-provided TODO API client.
/// </summary>
internal sealed class CreateTodoCommandHandler : ICommandHandler<CreateTodoCommand, TodoMutationOutcome>
{
    private readonly ITodoApiClient _todoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CreateTodoCommandHandler> _logger;


    public CreateTodoCommandHandler(ITodoApiClient todoApiClient, IAuthorizationPolicyService authorizationPolicy,
        ILogger<CreateTodoCommandHandler> logger)
    {
        _logger = logger;
        _todoApiClient = todoApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<TodoMutationOutcome>> HandleAsync(CreateTodoCommand command, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(command.Id))
            return Result<TodoMutationOutcome>.Failure("Id is required.");
        if (string.IsNullOrWhiteSpace(command.Title))
            return Result<TodoMutationOutcome>.Failure("Title is required.");
        if (string.IsNullOrWhiteSpace(command.Section))
            return Result<TodoMutationOutcome>.Failure("Section is required.");
        if (string.IsNullOrWhiteSpace(command.Priority))
            return Result<TodoMutationOutcome>.Failure("Priority is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TodoCreate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TodoCreate);
            return Result<TodoMutationOutcome>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _todoApiClient.CreateTodoAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<TodoMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TodoMutationOutcome>.Failure(ex);
        }
    }
}
