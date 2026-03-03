using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>Handles <see cref="CreateWorkspaceCommand"/>.</summary>
internal sealed class CreateWorkspaceCommandHandler : ICommandHandler<CreateWorkspaceCommand, WorkspaceMutationOutcome>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<CreateWorkspaceCommandHandler> _logger;

    public CreateWorkspaceCommandHandler(
        IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<CreateWorkspaceCommandHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceMutationOutcome>> HandleAsync(CreateWorkspaceCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceCreate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceCreate);
            return Result<WorkspaceMutationOutcome>.Failure(WorkspaceMutationHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.CreateWorkspaceAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<WorkspaceMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="UpdateWorkspaceCommand"/>.</summary>
internal sealed class UpdateWorkspaceCommandHandler : ICommandHandler<UpdateWorkspaceCommand, WorkspaceMutationOutcome>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpdateWorkspaceCommandHandler> _logger;

    public UpdateWorkspaceCommandHandler(
        IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<UpdateWorkspaceCommandHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceMutationOutcome>> HandleAsync(UpdateWorkspaceCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceUpdate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceUpdate);
            return Result<WorkspaceMutationOutcome>.Failure(WorkspaceMutationHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.UpdateWorkspaceAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<WorkspaceMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="DeleteWorkspaceCommand"/>.</summary>
internal sealed class DeleteWorkspaceCommandHandler : ICommandHandler<DeleteWorkspaceCommand, WorkspaceMutationOutcome>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<DeleteWorkspaceCommandHandler> _logger;

    public DeleteWorkspaceCommandHandler(
        IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<DeleteWorkspaceCommandHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceMutationOutcome>> HandleAsync(DeleteWorkspaceCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceDelete))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceDelete);
            return Result<WorkspaceMutationOutcome>.Failure(WorkspaceMutationHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.DeleteWorkspaceAsync(command.WorkspacePath, context.CancellationToken).ConfigureAwait(false);
            return Result<WorkspaceMutationOutcome>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceMutationOutcome>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="StartWorkspaceCommand"/>.</summary>
internal sealed class StartWorkspaceCommandHandler : ICommandHandler<StartWorkspaceCommand, WorkspaceRuntimeStatus>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<StartWorkspaceCommandHandler> _logger;

    public StartWorkspaceCommandHandler(
        IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<StartWorkspaceCommandHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceRuntimeStatus>> HandleAsync(StartWorkspaceCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceStart))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceStart);
            return Result<WorkspaceRuntimeStatus>.Failure(WorkspaceMutationHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.StartWorkspaceAsync(command.WorkspacePath, context.CancellationToken).ConfigureAwait(false);
            return Result<WorkspaceRuntimeStatus>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceRuntimeStatus>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="StopWorkspaceCommand"/>.</summary>
internal sealed class StopWorkspaceCommandHandler : ICommandHandler<StopWorkspaceCommand, WorkspaceRuntimeStatus>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<StopWorkspaceCommandHandler> _logger;

    public StopWorkspaceCommandHandler(
        IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<StopWorkspaceCommandHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceRuntimeStatus>> HandleAsync(StopWorkspaceCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceStop))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceStop);
            return Result<WorkspaceRuntimeStatus>.Failure(WorkspaceMutationHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.StopWorkspaceAsync(command.WorkspacePath, context.CancellationToken).ConfigureAwait(false);
            return Result<WorkspaceRuntimeStatus>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceRuntimeStatus>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="GetWorkspaceStatusQuery"/>.</summary>
internal sealed class GetWorkspaceStatusQueryHandler : IQueryHandler<GetWorkspaceStatusQuery, WorkspaceRuntimeStatus>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetWorkspaceStatusQueryHandler> _logger;

    public GetWorkspaceStatusQueryHandler(
        IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetWorkspaceStatusQueryHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<WorkspaceRuntimeStatus>> HandleAsync(GetWorkspaceStatusQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceStatus))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceStatus);
            return Result<WorkspaceRuntimeStatus>.Failure(WorkspaceMutationHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.GetWorkspaceStatusAsync(query.WorkspacePath, context.CancellationToken).ConfigureAwait(false);
            return Result<WorkspaceRuntimeStatus>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceRuntimeStatus>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="GetGlobalPromptQuery"/>.</summary>
internal sealed class GetGlobalPromptQueryHandler : IQueryHandler<GetGlobalPromptQuery, GlobalPromptInfo>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetGlobalPromptQueryHandler> _logger;

    public GetGlobalPromptQueryHandler(
        IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetGlobalPromptQueryHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GlobalPromptInfo>> HandleAsync(GetGlobalPromptQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspacePromptGet))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspacePromptGet);
            return Result<GlobalPromptInfo>.Failure(WorkspaceMutationHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.GetGlobalPromptAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<GlobalPromptInfo>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GlobalPromptInfo>.Failure(ex);
        }
    }
}

/// <summary>Handles <see cref="UpdateGlobalPromptCommand"/>.</summary>
internal sealed class UpdateGlobalPromptCommandHandler : ICommandHandler<UpdateGlobalPromptCommand, GlobalPromptInfo>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpdateGlobalPromptCommandHandler> _logger;

    public UpdateGlobalPromptCommandHandler(
        IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<UpdateGlobalPromptCommandHandler> logger)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
        _logger = logger;
    }

    public async Task<Result<GlobalPromptInfo>> HandleAsync(UpdateGlobalPromptCommand command, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspacePromptUpdate))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspacePromptUpdate);
            return Result<GlobalPromptInfo>.Failure(WorkspaceMutationHandlerHelpers.BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.UpdateGlobalPromptAsync(command, context.CancellationToken).ConfigureAwait(false);
            return Result<GlobalPromptInfo>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<GlobalPromptInfo>.Failure(ex);
        }
    }
}

internal static class WorkspaceMutationHandlerHelpers
{
    public static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
