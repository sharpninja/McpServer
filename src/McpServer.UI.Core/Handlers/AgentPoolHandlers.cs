using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="ListAgentPoolAgentsQuery"/>.</summary>
internal sealed class ListAgentPoolAgentsQueryHandler : IQueryHandler<ListAgentPoolAgentsQuery, ListAgentPoolAgentsResult>
{
    private readonly IAgentPoolApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListAgentPoolAgentsQueryHandler> _logger;

    public ListAgentPoolAgentsQueryHandler(
        IAgentPoolApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ListAgentPoolAgentsQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<ListAgentPoolAgentsResult>> HandleAsync(ListAgentPoolAgentsQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentPoolAgentsList))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentPoolAgentsList);
            return Result<ListAgentPoolAgentsResult>.Failure(AgentPoolHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.ListAgentsAsync(context.CancellationToken).ConfigureAwait(true);
            return Result<ListAgentPoolAgentsResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ListAgentPoolAgentsResult>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="ListAgentPoolQueueQuery"/>.</summary>
internal sealed class ListAgentPoolQueueQueryHandler : IQueryHandler<ListAgentPoolQueueQuery, ListAgentPoolQueueResult>
{
    private readonly IAgentPoolApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ListAgentPoolQueueQueryHandler> _logger;

    public ListAgentPoolQueueQueryHandler(
        IAgentPoolApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ListAgentPoolQueueQueryHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<ListAgentPoolQueueResult>> HandleAsync(ListAgentPoolQueueQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentPoolQueueList))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentPoolQueueList);
            return Result<ListAgentPoolQueueResult>.Failure(AgentPoolHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.ListQueueAsync(context.CancellationToken).ConfigureAwait(true);
            return Result<ListAgentPoolQueueResult>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<ListAgentPoolQueueResult>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="StartAgentPoolAgentCommand"/>.</summary>
internal sealed class StartAgentPoolAgentCommandHandler : ICommandHandler<StartAgentPoolAgentCommand, AgentPoolMutationOutcome>
{
    private readonly IAgentPoolApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<StartAgentPoolAgentCommandHandler> _logger;

    public StartAgentPoolAgentCommandHandler(
        IAgentPoolApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<StartAgentPoolAgentCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentPoolMutationOutcome>> HandleAsync(StartAgentPoolAgentCommand command, CallContext context)
    {
        return await AgentPoolHandlerHelpers.HandleMutationAsync(
            McpActionKeys.AgentPoolAgentStart,
            () => _client.StartAgentAsync(command.AgentName, context.CancellationToken),
            _authorizationPolicy,
            _logger).ConfigureAwait(true);
    }
}

/// <summary>Handles <see cref="StopAgentPoolAgentCommand"/>.</summary>
internal sealed class StopAgentPoolAgentCommandHandler : ICommandHandler<StopAgentPoolAgentCommand, AgentPoolMutationOutcome>
{
    private readonly IAgentPoolApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<StopAgentPoolAgentCommandHandler> _logger;

    public StopAgentPoolAgentCommandHandler(
        IAgentPoolApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<StopAgentPoolAgentCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentPoolMutationOutcome>> HandleAsync(StopAgentPoolAgentCommand command, CallContext context)
    {
        return await AgentPoolHandlerHelpers.HandleMutationAsync(
            McpActionKeys.AgentPoolAgentStop,
            () => _client.StopAgentAsync(command.AgentName, context.CancellationToken),
            _authorizationPolicy,
            _logger).ConfigureAwait(true);
    }
}

/// <summary>Handles <see cref="RecycleAgentPoolAgentCommand"/>.</summary>
internal sealed class RecycleAgentPoolAgentCommandHandler : ICommandHandler<RecycleAgentPoolAgentCommand, AgentPoolMutationOutcome>
{
    private readonly IAgentPoolApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<RecycleAgentPoolAgentCommandHandler> _logger;

    public RecycleAgentPoolAgentCommandHandler(
        IAgentPoolApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<RecycleAgentPoolAgentCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentPoolMutationOutcome>> HandleAsync(RecycleAgentPoolAgentCommand command, CallContext context)
    {
        return await AgentPoolHandlerHelpers.HandleMutationAsync(
            McpActionKeys.AgentPoolAgentRecycle,
            () => _client.RecycleAgentAsync(command.AgentName, context.CancellationToken),
            _authorizationPolicy,
            _logger).ConfigureAwait(true);
    }
}

/// <summary>Handles <see cref="ConnectAgentPoolAgentCommand"/>.</summary>
internal sealed class ConnectAgentPoolAgentCommandHandler : ICommandHandler<ConnectAgentPoolAgentCommand, AgentPoolConnectOutcome>
{
    private readonly IAgentPoolApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ConnectAgentPoolAgentCommandHandler> _logger;

    public ConnectAgentPoolAgentCommandHandler(
        IAgentPoolApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ConnectAgentPoolAgentCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentPoolConnectOutcome>> HandleAsync(ConnectAgentPoolAgentCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentPoolAgentConnect))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentPoolAgentConnect);
            return Result<AgentPoolConnectOutcome>.Failure(AgentPoolHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.ConnectAsync(command.AgentName, context.CancellationToken).ConfigureAwait(true);
            return Result<AgentPoolConnectOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentPoolConnectOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="CancelAgentPoolQueueItemCommand"/>.</summary>
internal sealed class CancelAgentPoolQueueItemCommandHandler : ICommandHandler<CancelAgentPoolQueueItemCommand, AgentPoolMutationOutcome>
{
    private readonly IAgentPoolApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CancelAgentPoolQueueItemCommandHandler> _logger;

    public CancelAgentPoolQueueItemCommandHandler(
        IAgentPoolApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<CancelAgentPoolQueueItemCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentPoolMutationOutcome>> HandleAsync(CancelAgentPoolQueueItemCommand command, CallContext context)
    {
        return await AgentPoolHandlerHelpers.HandleMutationAsync(
            McpActionKeys.AgentPoolQueueCancel,
            () => _client.CancelQueueItemAsync(command.JobId, context.CancellationToken),
            _authorizationPolicy,
            _logger).ConfigureAwait(true);
    }
}

/// <summary>Handles <see cref="RemoveAgentPoolQueueItemCommand"/>.</summary>
internal sealed class RemoveAgentPoolQueueItemCommandHandler : ICommandHandler<RemoveAgentPoolQueueItemCommand, AgentPoolMutationOutcome>
{
    private readonly IAgentPoolApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<RemoveAgentPoolQueueItemCommandHandler> _logger;

    public RemoveAgentPoolQueueItemCommandHandler(
        IAgentPoolApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<RemoveAgentPoolQueueItemCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentPoolMutationOutcome>> HandleAsync(RemoveAgentPoolQueueItemCommand command, CallContext context)
    {
        return await AgentPoolHandlerHelpers.HandleMutationAsync(
            McpActionKeys.AgentPoolQueueRemove,
            () => _client.RemoveQueueItemAsync(command.JobId, context.CancellationToken),
            _authorizationPolicy,
            _logger).ConfigureAwait(true);
    }
}

/// <summary>Handles <see cref="MoveAgentPoolQueueItemUpCommand"/>.</summary>
internal sealed class MoveAgentPoolQueueItemUpCommandHandler : ICommandHandler<MoveAgentPoolQueueItemUpCommand, AgentPoolMutationOutcome>
{
    private readonly IAgentPoolApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<MoveAgentPoolQueueItemUpCommandHandler> _logger;

    public MoveAgentPoolQueueItemUpCommandHandler(
        IAgentPoolApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<MoveAgentPoolQueueItemUpCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentPoolMutationOutcome>> HandleAsync(MoveAgentPoolQueueItemUpCommand command, CallContext context)
    {
        return await AgentPoolHandlerHelpers.HandleMutationAsync(
            McpActionKeys.AgentPoolQueueMove,
            () => _client.MoveQueueItemUpAsync(command.JobId, context.CancellationToken),
            _authorizationPolicy,
            _logger).ConfigureAwait(true);
    }
}

/// <summary>Handles <see cref="MoveAgentPoolQueueItemDownCommand"/>.</summary>
internal sealed class MoveAgentPoolQueueItemDownCommandHandler : ICommandHandler<MoveAgentPoolQueueItemDownCommand, AgentPoolMutationOutcome>
{
    private readonly IAgentPoolApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<MoveAgentPoolQueueItemDownCommandHandler> _logger;

    public MoveAgentPoolQueueItemDownCommandHandler(
        IAgentPoolApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<MoveAgentPoolQueueItemDownCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentPoolMutationOutcome>> HandleAsync(MoveAgentPoolQueueItemDownCommand command, CallContext context)
    {
        return await AgentPoolHandlerHelpers.HandleMutationAsync(
            McpActionKeys.AgentPoolQueueMove,
            () => _client.MoveQueueItemDownAsync(command.JobId, context.CancellationToken),
            _authorizationPolicy,
            _logger).ConfigureAwait(true);
    }
}

/// <summary>Handles <see cref="ResolveAgentPoolPromptCommand"/>.</summary>
internal sealed class ResolveAgentPoolPromptCommandHandler : ICommandHandler<ResolveAgentPoolPromptCommand, AgentPoolPromptResolutionOutcome>
{
    private readonly IAgentPoolApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<ResolveAgentPoolPromptCommandHandler> _logger;

    public ResolveAgentPoolPromptCommandHandler(
        IAgentPoolApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<ResolveAgentPoolPromptCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentPoolPromptResolutionOutcome>> HandleAsync(ResolveAgentPoolPromptCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentPoolQueueResolve))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentPoolQueueResolve);
            return Result<AgentPoolPromptResolutionOutcome>.Failure(AgentPoolHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.ResolvePromptAsync(command.Request, context.CancellationToken).ConfigureAwait(true);
            return Result<AgentPoolPromptResolutionOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentPoolPromptResolutionOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="EnqueueAgentPoolPromptCommand"/>.</summary>
internal sealed class EnqueueAgentPoolPromptCommandHandler : ICommandHandler<EnqueueAgentPoolPromptCommand, AgentPoolEnqueueOutcome>
{
    private readonly IAgentPoolApiClient _client;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<EnqueueAgentPoolPromptCommandHandler> _logger;

    public EnqueueAgentPoolPromptCommandHandler(
        IAgentPoolApiClient client,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<EnqueueAgentPoolPromptCommandHandler> logger)
    {
        _client = client;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<AgentPoolEnqueueOutcome>> HandleAsync(EnqueueAgentPoolPromptCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.AgentPoolQueueEnqueue))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.AgentPoolQueueEnqueue);
            return Result<AgentPoolEnqueueOutcome>.Failure(AgentPoolHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _client.EnqueueOneShotAsync(command.Request, context.CancellationToken).ConfigureAwait(true);
            return Result<AgentPoolEnqueueOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentPoolEnqueueOutcome>.Failure(ex);
        }
    }
}

internal static class AgentPoolHandlerHelpers
{
    public static async Task<Result<AgentPoolMutationOutcome>> HandleMutationAsync(
        string actionKey,
        Func<Task<AgentPoolMutationOutcome>> operation,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger logger)
    {
        if (!authorizationPolicy.CanExecuteAction(actionKey))
        {
            var requiredRole = authorizationPolicy.GetRequiredRole(actionKey);
            return Result<AgentPoolMutationOutcome>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await operation().ConfigureAwait(true);
            return Result<AgentPoolMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<AgentPoolMutationOutcome>.Failure(ex);
        }
    }

    public static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
