using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="ListAgentDefinitionsQuery"/>.</summary>
internal sealed class ListAgentDefinitionsQueryHandler : IQueryHandler<ListAgentDefinitionsQuery, ListAgentDefinitionsResult>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListAgentDefinitionsQueryHandler> _logger;

    public ListAgentDefinitionsQueryHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ListAgentDefinitionsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<ListAgentDefinitionsResult>> HandleAsync(ListAgentDefinitionsQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentDefinitionList))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentDefinitionList);
            return Result<ListAgentDefinitionsResult>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.ListDefinitionsAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<ListAgentDefinitionsResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ListAgentDefinitionsResult>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="GetAgentDefinitionQuery"/>.</summary>
internal sealed class GetAgentDefinitionQueryHandler : IQueryHandler<GetAgentDefinitionQuery, AgentDefinitionDetail?>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetAgentDefinitionQueryHandler> _logger;

    public GetAgentDefinitionQueryHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetAgentDefinitionQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentDefinitionDetail?>> HandleAsync(GetAgentDefinitionQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentDefinitionGet))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentDefinitionGet);
            return Result<AgentDefinitionDetail?>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.GetDefinitionAsync(query.AgentType, context.CancellationToken).ConfigureAwait(false);
            return Result<AgentDefinitionDetail?>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentDefinitionDetail?>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="UpsertAgentDefinitionCommand"/>.</summary>
internal sealed class UpsertAgentDefinitionCommandHandler : ICommandHandler<UpsertAgentDefinitionCommand, AgentMutationOutcome>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpsertAgentDefinitionCommandHandler> _logger;

    public UpsertAgentDefinitionCommandHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<UpsertAgentDefinitionCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentMutationOutcome>> HandleAsync(UpsertAgentDefinitionCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentDefinitionUpsert))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentDefinitionUpsert);
            return Result<AgentMutationOutcome>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.UpsertDefinitionAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<AgentMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="AssignWorkspaceAgentCommand"/>.</summary>
internal sealed class AssignWorkspaceAgentCommandHandler : ICommandHandler<AssignWorkspaceAgentCommand, AgentMutationOutcome>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<AssignWorkspaceAgentCommandHandler> _logger;

    public AssignWorkspaceAgentCommandHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<AssignWorkspaceAgentCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentMutationOutcome>> HandleAsync(AssignWorkspaceAgentCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentWorkspaceAssign))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentWorkspaceAssign);
            return Result<AgentMutationOutcome>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.AssignWorkspaceAgentAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<AgentMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentMutationOutcome>.Failure(ex);
        }
    }
}

internal static class AgentHandlerHelpers
{
    public static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
