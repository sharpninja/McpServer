using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="CreateWorkspaceCommand"/> using the host-provided workspace API client.</summary>
internal sealed class CreateWorkspaceCommandHandler : ICommandHandler<CreateWorkspaceCommand, WorkspaceMutationOutcome>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CreateWorkspaceCommandHandler> _logger;

    public CreateWorkspaceCommandHandler(IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<CreateWorkspaceCommandHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceMutationOutcome>> HandleAsync(CreateWorkspaceCommand command, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(command.WorkspacePath))
            return Result<WorkspaceMutationOutcome>.Failure("WorkspacePath is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceCreate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceCreate);
            return Result<WorkspaceMutationOutcome>.Failure(WorkspaceHandlerMessages.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.CreateWorkspaceAsync(command, context.CancellationToken).ConfigureAwait(true);
            return Result<WorkspaceMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="UpdateWorkspaceCommand"/> using the host-provided workspace API client.</summary>
internal sealed class UpdateWorkspaceCommandHandler : ICommandHandler<UpdateWorkspaceCommand, WorkspaceMutationOutcome>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpdateWorkspaceCommandHandler> _logger;

    public UpdateWorkspaceCommandHandler(IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<UpdateWorkspaceCommandHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceMutationOutcome>> HandleAsync(UpdateWorkspaceCommand command, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(command.WorkspacePath))
            return Result<WorkspaceMutationOutcome>.Failure("WorkspacePath is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceUpdate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceUpdate);
            return Result<WorkspaceMutationOutcome>.Failure(WorkspaceHandlerMessages.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.UpdateWorkspaceAsync(command, context.CancellationToken).ConfigureAwait(true);
            return Result<WorkspaceMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="DeleteWorkspaceCommand"/> using the host-provided workspace API client.</summary>
internal sealed class DeleteWorkspaceCommandHandler : ICommandHandler<DeleteWorkspaceCommand, WorkspaceMutationOutcome>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<DeleteWorkspaceCommandHandler> _logger;

    public DeleteWorkspaceCommandHandler(IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<DeleteWorkspaceCommandHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceMutationOutcome>> HandleAsync(DeleteWorkspaceCommand command, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(command.WorkspacePath))
            return Result<WorkspaceMutationOutcome>.Failure("WorkspacePath is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceDelete))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceDelete);
            return Result<WorkspaceMutationOutcome>.Failure(WorkspaceHandlerMessages.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.DeleteWorkspaceAsync(command, context.CancellationToken).ConfigureAwait(true);
            return Result<WorkspaceMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="GetWorkspaceStatusQuery"/> using the host-provided workspace API client.</summary>
internal sealed class GetWorkspaceStatusQueryHandler : IQueryHandler<GetWorkspaceStatusQuery, WorkspaceProcessState>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetWorkspaceStatusQueryHandler> _logger;

    public GetWorkspaceStatusQueryHandler(IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetWorkspaceStatusQueryHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceProcessState>> HandleAsync(GetWorkspaceStatusQuery query, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(query.WorkspacePath))
            return Result<WorkspaceProcessState>.Failure("WorkspacePath is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceStatus))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceStatus);
            return Result<WorkspaceProcessState>.Failure(WorkspaceHandlerMessages.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.GetWorkspaceStatusAsync(query.WorkspacePath, context.CancellationToken)
                .ConfigureAwait(true);
            return Result<WorkspaceProcessState>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceProcessState>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="StartWorkspaceCommand"/> using the host-provided workspace API client.</summary>
internal sealed class StartWorkspaceCommandHandler : ICommandHandler<StartWorkspaceCommand, WorkspaceProcessState>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<StartWorkspaceCommandHandler> _logger;

    public StartWorkspaceCommandHandler(IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<StartWorkspaceCommandHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceProcessState>> HandleAsync(StartWorkspaceCommand command, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(command.WorkspacePath))
            return Result<WorkspaceProcessState>.Failure("WorkspacePath is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceStart))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceStart);
            return Result<WorkspaceProcessState>.Failure(WorkspaceHandlerMessages.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.StartWorkspaceAsync(command.WorkspacePath, context.CancellationToken)
                .ConfigureAwait(true);
            return Result<WorkspaceProcessState>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceProcessState>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="StopWorkspaceCommand"/> using the host-provided workspace API client.</summary>
internal sealed class StopWorkspaceCommandHandler : ICommandHandler<StopWorkspaceCommand, WorkspaceProcessState>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<StopWorkspaceCommandHandler> _logger;

    public StopWorkspaceCommandHandler(IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<StopWorkspaceCommandHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceProcessState>> HandleAsync(StopWorkspaceCommand command, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(command.WorkspacePath))
            return Result<WorkspaceProcessState>.Failure("WorkspacePath is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceStop))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceStop);
            return Result<WorkspaceProcessState>.Failure(WorkspaceHandlerMessages.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.StopWorkspaceAsync(command.WorkspacePath, context.CancellationToken)
                .ConfigureAwait(true);
            return Result<WorkspaceProcessState>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceProcessState>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="GetWorkspaceGlobalPromptQuery"/> using the host-provided workspace API client.</summary>
internal sealed class CheckWorkspaceHealthQueryHandler : IQueryHandler<CheckWorkspaceHealthQuery, WorkspaceHealthState>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CheckWorkspaceHealthQueryHandler> _logger;

    public CheckWorkspaceHealthQueryHandler(IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<CheckWorkspaceHealthQueryHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceHealthState>> HandleAsync(CheckWorkspaceHealthQuery query, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(query.WorkspacePath))
            return Result<WorkspaceHealthState>.Failure("WorkspacePath is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceHealth))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceHealth);
            return Result<WorkspaceHealthState>.Failure(WorkspaceHandlerMessages.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.CheckWorkspaceHealthAsync(query.WorkspacePath, context.CancellationToken)
                .ConfigureAwait(true);
            return Result<WorkspaceHealthState>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceHealthState>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="GetWorkspaceGlobalPromptQuery"/> using the host-provided workspace API client.</summary>
internal sealed class GetWorkspaceGlobalPromptQueryHandler : IQueryHandler<GetWorkspaceGlobalPromptQuery, WorkspaceGlobalPromptState>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetWorkspaceGlobalPromptQueryHandler> _logger;

    public GetWorkspaceGlobalPromptQueryHandler(IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetWorkspaceGlobalPromptQueryHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceGlobalPromptState>> HandleAsync(GetWorkspaceGlobalPromptQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceGlobalPromptGet))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceGlobalPromptGet);
            return Result<WorkspaceGlobalPromptState>.Failure(WorkspaceHandlerMessages.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.GetWorkspaceGlobalPromptAsync(context.CancellationToken)
                .ConfigureAwait(true);
            return Result<WorkspaceGlobalPromptState>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceGlobalPromptState>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="UpdateWorkspaceGlobalPromptCommand"/> using the host-provided workspace API client.</summary>
internal sealed class UpdateWorkspaceGlobalPromptCommandHandler : ICommandHandler<UpdateWorkspaceGlobalPromptCommand, WorkspaceGlobalPromptState>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpdateWorkspaceGlobalPromptCommandHandler> _logger;

    public UpdateWorkspaceGlobalPromptCommandHandler(IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<UpdateWorkspaceGlobalPromptCommandHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceGlobalPromptState>> HandleAsync(UpdateWorkspaceGlobalPromptCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceGlobalPromptUpdate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceGlobalPromptUpdate);
            return Result<WorkspaceGlobalPromptState>.Failure(WorkspaceHandlerMessages.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.UpdateWorkspaceGlobalPromptAsync(command, context.CancellationToken)
                .ConfigureAwait(true);
            return Result<WorkspaceGlobalPromptState>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceGlobalPromptState>.Failure(ex);
        }
    }
}

file static class WorkspaceHandlerMessages
{
    public static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
