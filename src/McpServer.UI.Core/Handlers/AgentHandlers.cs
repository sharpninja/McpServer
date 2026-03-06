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
            var result = await _client.ListDefinitionsAsync(context.CancellationToken);
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
            var result = await _client.GetDefinitionAsync(query.AgentType, context.CancellationToken);
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
            var result = await _client.UpsertDefinitionAsync(command, context.CancellationToken);
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
            var result = await _client.AssignWorkspaceAgentAsync(command, context.CancellationToken);
            return Result<AgentMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="DeleteAgentDefinitionCommand"/>.</summary>
internal sealed class DeleteAgentDefinitionCommandHandler : ICommandHandler<DeleteAgentDefinitionCommand, AgentMutationOutcome>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<DeleteAgentDefinitionCommandHandler> _logger;

    public DeleteAgentDefinitionCommandHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<DeleteAgentDefinitionCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentMutationOutcome>> HandleAsync(DeleteAgentDefinitionCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentDefinitionDelete))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentDefinitionDelete);
            return Result<AgentMutationOutcome>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.DeleteDefinitionAsync(command, context.CancellationToken);
            return Result<AgentMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="SeedAgentDefaultsCommand"/>.</summary>
internal sealed class SeedAgentDefaultsCommandHandler : ICommandHandler<SeedAgentDefaultsCommand, AgentSeedOutcome>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<SeedAgentDefaultsCommandHandler> _logger;

    public SeedAgentDefaultsCommandHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<SeedAgentDefaultsCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentSeedOutcome>> HandleAsync(SeedAgentDefaultsCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentDefinitionSeed))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentDefinitionSeed);
            return Result<AgentSeedOutcome>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.SeedDefaultsAsync(context.CancellationToken);
            return Result<AgentSeedOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentSeedOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="ListWorkspaceAgentsQuery"/>.</summary>
internal sealed class ListWorkspaceAgentsQueryHandler : IQueryHandler<ListWorkspaceAgentsQuery, ListWorkspaceAgentsResult>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListWorkspaceAgentsQueryHandler> _logger;

    public ListWorkspaceAgentsQueryHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ListWorkspaceAgentsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<ListWorkspaceAgentsResult>> HandleAsync(ListWorkspaceAgentsQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentWorkspaceList))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentWorkspaceList);
            return Result<ListWorkspaceAgentsResult>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.ListWorkspaceAgentsAsync(query, context.CancellationToken);
            return Result<ListWorkspaceAgentsResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ListWorkspaceAgentsResult>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="GetWorkspaceAgentQuery"/>.</summary>
internal sealed class GetWorkspaceAgentQueryHandler : IQueryHandler<GetWorkspaceAgentQuery, WorkspaceAgentDetail?>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetWorkspaceAgentQueryHandler> _logger;

    public GetWorkspaceAgentQueryHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetWorkspaceAgentQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceAgentDetail?>> HandleAsync(GetWorkspaceAgentQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentWorkspaceGet))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentWorkspaceGet);
            return Result<WorkspaceAgentDetail?>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.GetWorkspaceAgentAsync(query, context.CancellationToken);
            return Result<WorkspaceAgentDetail?>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceAgentDetail?>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="UpsertWorkspaceAgentCommand"/>.</summary>
internal sealed class UpsertWorkspaceAgentCommandHandler : ICommandHandler<UpsertWorkspaceAgentCommand, AgentMutationOutcome>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpsertWorkspaceAgentCommandHandler> _logger;

    public UpsertWorkspaceAgentCommandHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<UpsertWorkspaceAgentCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentMutationOutcome>> HandleAsync(UpsertWorkspaceAgentCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentWorkspaceAssign))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentWorkspaceAssign);
            return Result<AgentMutationOutcome>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.UpsertWorkspaceAgentAsync(command, context.CancellationToken);
            return Result<AgentMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="DeleteWorkspaceAgentCommand"/>.</summary>
internal sealed class DeleteWorkspaceAgentCommandHandler : ICommandHandler<DeleteWorkspaceAgentCommand, AgentMutationOutcome>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<DeleteWorkspaceAgentCommandHandler> _logger;

    public DeleteWorkspaceAgentCommandHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<DeleteWorkspaceAgentCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentMutationOutcome>> HandleAsync(DeleteWorkspaceAgentCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentWorkspaceDelete))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentWorkspaceDelete);
            return Result<AgentMutationOutcome>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.DeleteWorkspaceAgentAsync(command, context.CancellationToken);
            return Result<AgentMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="BanAgentCommand"/>.</summary>
internal sealed class BanAgentCommandHandler : ICommandHandler<BanAgentCommand, AgentMutationOutcome>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<BanAgentCommandHandler> _logger;

    public BanAgentCommandHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<BanAgentCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentMutationOutcome>> HandleAsync(BanAgentCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentBan))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentBan);
            return Result<AgentMutationOutcome>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.BanAgentAsync(command, context.CancellationToken);
            return Result<AgentMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="UnbanAgentCommand"/>.</summary>
internal sealed class UnbanAgentCommandHandler : ICommandHandler<UnbanAgentCommand, AgentMutationOutcome>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UnbanAgentCommandHandler> _logger;

    public UnbanAgentCommandHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<UnbanAgentCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentMutationOutcome>> HandleAsync(UnbanAgentCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentUnban))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentUnban);
            return Result<AgentMutationOutcome>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.UnbanAgentAsync(command, context.CancellationToken);
            return Result<AgentMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="LogAgentEventCommand"/>.</summary>
internal sealed class LogAgentEventCommandHandler : ICommandHandler<LogAgentEventCommand, AgentMutationOutcome>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<LogAgentEventCommandHandler> _logger;

    public LogAgentEventCommandHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<LogAgentEventCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentMutationOutcome>> HandleAsync(LogAgentEventCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentEventLog))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentEventLog);
            return Result<AgentMutationOutcome>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.LogEventAsync(command, context.CancellationToken);
            return Result<AgentMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="GetAgentEventsQuery"/>.</summary>
internal sealed class GetAgentEventsQueryHandler : IQueryHandler<GetAgentEventsQuery, AgentEventsResult>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetAgentEventsQueryHandler> _logger;

    public GetAgentEventsQueryHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetAgentEventsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentEventsResult>> HandleAsync(GetAgentEventsQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentEventList))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentEventList);
            return Result<AgentEventsResult>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.GetEventsAsync(query, context.CancellationToken);
            return Result<AgentEventsResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentEventsResult>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="ValidateAgentQuery"/>.</summary>
internal sealed class ValidateAgentQueryHandler : IQueryHandler<ValidateAgentQuery, AgentValidateOutcome>
{
    private readonly IAgentApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ValidateAgentQueryHandler> _logger;

    public ValidateAgentQueryHandler(
        IAgentApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ValidateAgentQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentValidateOutcome>> HandleAsync(ValidateAgentQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentValidate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentValidate);
            return Result<AgentValidateOutcome>.Failure(AgentHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.ValidateAsync(query, context.CancellationToken);
            return Result<AgentValidateOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentValidateOutcome>.Failure(ex);
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
