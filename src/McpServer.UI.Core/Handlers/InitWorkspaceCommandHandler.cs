using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="InitWorkspaceCommand"/> by delegating the initialization workflow to the host-provided workspace API client.
/// </summary>
internal sealed class InitWorkspaceCommandHandler : ICommandHandler<InitWorkspaceCommand, WorkspaceInitInfo>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public InitWorkspaceCommandHandler(
        IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<WorkspaceInitInfo>> HandleAsync(InitWorkspaceCommand command, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(command.WorkspacePath))
            return Result<WorkspaceInitInfo>.Failure("WorkspacePath is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceInit))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceInit);
            return Result<WorkspaceInitInfo>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.InitWorkspaceAsync(command.WorkspacePath, context.CancellationToken)
                .ConfigureAwait(false);
            return Result<WorkspaceInitInfo>.Success(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return Result<WorkspaceInitInfo>.Failure(ex);
        }
    }

    private static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
