using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles TODO plan prompt generation using the host-provided TODO API client.
/// </summary>
internal sealed class GenerateTodoPlanPromptQueryHandler : IQueryHandler<GenerateTodoPlanPromptQuery, TodoPromptOutput>
{
    private readonly ITodoApiClient _todoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GenerateTodoPlanPromptQueryHandler> _logger;


    public GenerateTodoPlanPromptQueryHandler(ITodoApiClient todoApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GenerateTodoPlanPromptQueryHandler> logger)
    {
        _logger = logger;
        _todoApiClient = todoApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<TodoPromptOutput>> HandleAsync(GenerateTodoPlanPromptQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TodoPromptPlan))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TodoPromptPlan);
            return Result<TodoPromptOutput>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _todoApiClient.GenerateTodoPlanPromptAsync(query.TodoId, context.CancellationToken).ConfigureAwait(true);
            return Result<TodoPromptOutput>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TodoPromptOutput>.Failure(ex);
        }
    }
}
