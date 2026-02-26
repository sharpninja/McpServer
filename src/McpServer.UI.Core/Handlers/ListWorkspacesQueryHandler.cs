using McpServer.Cqrs;
using McpServer.UI.Core.Authorization;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.UI.Core.Handlers;

/// <summary>
/// Handles <see cref="ListWorkspacesQuery"/> by calling the host-provided workspace API client.
/// </summary>
internal sealed class ListWorkspacesQueryHandler : IQueryHandler<ListWorkspacesQuery, ListWorkspacesResult>
{
    private readonly IWorkspaceApiClient _workspaceApiClient;
    private readonly IAuthorizationPolicyService _authorizationPolicy;

    public ListWorkspacesQueryHandler(
        IWorkspaceApiClient workspaceApiClient,
        IAuthorizationPolicyService authorizationPolicy)
    {
        _workspaceApiClient = workspaceApiClient;
        _authorizationPolicy = authorizationPolicy;
    }

    public async Task<Result<ListWorkspacesResult>> HandleAsync(ListWorkspacesQuery query, CallContext context)
    {
        if (!_authorizationPolicy.CanExecuteAction(McpActionKeys.WorkspaceList))
        {
            var requiredRole = _authorizationPolicy.GetRequiredRole(McpActionKeys.WorkspaceList);
            return Result<ListWorkspacesResult>.Failure(BuildPermissionDenied(requiredRole));
        }

        try
        {
            var result = await _workspaceApiClient.ListWorkspacesAsync(context.CancellationToken).ConfigureAwait(false);
            return Result<ListWorkspacesResult>.Success(result);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Trace.TraceError(ex.ToString());
            return Result<ListWorkspacesResult>.Failure(ex);
        }
    }

    private static string BuildPermissionDenied(string? requiredRole)
        => string.IsNullOrWhiteSpace(requiredRole)
            ? "Permission denied."
            : $"Permission denied: requires {requiredRole}.";
}
