using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles TODO implement prompt generation using the host-provided TODO API client.
/// </summary>
internal sealed class GenerateTodoImplementPromptQueryHandler : IQueryHandler<GenerateTodoImplementPromptQuery, TodoPromptOutput>
{
    private readonly ITodoApiClient _todoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GenerateTodoImplementPromptQueryHandler> _logger;


    public GenerateTodoImplementPromptQueryHandler(ITodoApiClient todoApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GenerateTodoImplementPromptQueryHandler> logger)
    {
        _logger = logger;
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
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TodoPromptOutput>.Failure(ex);
        }
    }
}
