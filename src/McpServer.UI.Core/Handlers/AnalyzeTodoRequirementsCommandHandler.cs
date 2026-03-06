using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles TODO requirements analysis using the host-provided TODO API client.
/// </summary>
internal sealed class AnalyzeTodoRequirementsCommandHandler : ICommandHandler<AnalyzeTodoRequirementsCommand, TodoRequirementsAnalysis>
{
    private readonly ITodoApiClient _todoApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<AnalyzeTodoRequirementsCommandHandler> _logger;


    public AnalyzeTodoRequirementsCommandHandler(ITodoApiClient todoApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<AnalyzeTodoRequirementsCommandHandler> logger)
    {
        _logger = logger;
        _todoApiClient = todoApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<TodoRequirementsAnalysis>> HandleAsync(AnalyzeTodoRequirementsCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.TodoRequirements))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.TodoRequirements);
            return Result<TodoRequirementsAnalysis>.Failure(
                string.IsNullOrWhiteSpace(requiredRole)
                    ? "Permission denied."
                    : $"Permission denied: requires {requiredRole}.");
        }

        try
        {
            var result = await _todoApiClient.AnalyzeTodoRequirementsAsync(command.TodoId, context.CancellationToken);
            return Result<TodoRequirementsAnalysis>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<TodoRequirementsAnalysis>.Failure(ex);
        }
    }
}
