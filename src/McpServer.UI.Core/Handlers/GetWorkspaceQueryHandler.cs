using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using Microsoft.Extensions.Logging;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="GetWorkspaceQuery"/> by loading a workspace from the host-provided workspace API client.
/// </summary>
internal sealed class GetWorkspaceQueryHandler : IQueryHandler<GetWorkspaceQuery, WorkspaceDetail?>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;
    private readonly ILogger<GetWorkspaceQueryHandler> _logger;


    public GetWorkspaceQueryHandler(IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy,
        ILogger<GetWorkspaceQueryHandler> logger)
    {
        _logger = logger;
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<WorkspaceDetail?>> HandleAsync(GetWorkspaceQuery query, CallContext context)
    {
        if (string.IsNullOrWhiteSpace(query.WorkspacePath))
            return Result<WorkspaceDetail?>.Failure("WorkspacePath is required.");

        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceGet))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceGet);
            return Result<WorkspaceDetail?>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.GetWorkspaceAsync(query.WorkspacePath, context.CancellationToken)
                .ConfigureAwait(false);
            return Result<WorkspaceDetail?>.Success(result);
        }
        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            return Result<WorkspaceDetail?>.Failure(ex);
        }
    }

    private static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
