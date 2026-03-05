using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="UpdateWorkspacePolicyCommand"/> by sending the compliance policy to the host-provided workspace API client.
/// </summary>
internal sealed class UpdateWorkspacePolicyCommandHandler : ICommandHandler<UpdateWorkspacePolicyCommand, bool>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<UpdateWorkspacePolicyCommandHandler> _logger;


    public UpdateWorkspacePolicyCommandHandler(IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<UpdateWorkspacePolicyCommandHandler> logger)
    {
        _logger = logger;
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<bool>> HandleAsync(UpdateWorkspacePolicyCommand command, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(command.WorkspacePath))
            return Result<bool>.Failure("WorkspacePath is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceUpdatePolicy))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceUpdatePolicy);
            return Result<bool>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            var success = await _workspaceApiClient.UpdateWorkspacePolicyAsync(command, context.CancellationToken)
                .ConfigureAwait(false);

            return success
                ? Result<bool>.Success(true)
                : Result<bool>.Failure("Workspace policy update failed.");
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<bool>.Failure(ex);
        }
    }

    private static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
