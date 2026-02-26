using McpServer.UI.Core.Authorization;

namespace McpServer.Director.Auth;

/// <summary>
/// Director RBAC policy implementation for tab visibility and command authorization.
/// Uses JWT-derived roles via <see cref="IRoleContext"/>.
/// </summary>
internal sealed class DirectorAuthorizationPolicyService : IAuthorizationPolicyService
{
    private readonly IRoleContext _roleContext;

    private static readonly IReadOnlyDictionary<McpArea, string> AreaRoles = new Dictionary<McpArea, string>
    {
        [McpArea.Health] = McpRoles.Viewer,
        [McpArea.Workspaces] = McpRoles.Admin,
        [McpArea.Policy] = McpRoles.Admin,
        [McpArea.Agents] = McpRoles.AgentManager,
        [McpArea.Todo] = McpRoles.Viewer,
        [McpArea.SessionLogs] = McpRoles.Viewer,
        [McpArea.DispatcherLogs] = McpRoles.Viewer,
        [McpArea.Sync] = McpRoles.Admin,
        [McpArea.Context] = McpRoles.Viewer,
        [McpArea.Repo] = McpRoles.Viewer,
        [McpArea.ToolRegistry] = McpRoles.Viewer,
        [McpArea.GitHub] = McpRoles.Viewer,
        [McpArea.Events] = McpRoles.Viewer,
        [McpArea.Diagnostic] = McpRoles.Viewer,
        [McpArea.AuthConfig] = McpRoles.Viewer,
    };

    private static readonly IReadOnlyDictionary<string, string> ActionRoles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [McpActionKeys.WorkspaceList] = McpRoles.Admin,
            [McpActionKeys.WorkspaceGet] = McpRoles.Admin,
            [McpActionKeys.WorkspaceUpdatePolicy] = McpRoles.Admin,
            [McpActionKeys.WorkspaceInit] = McpRoles.Admin,
            [McpActionKeys.SyncRun] = McpRoles.Admin,
            [McpActionKeys.SyncStatus] = McpRoles.Viewer,
            [McpActionKeys.SessionLogQuery] = McpRoles.Viewer,
            [McpActionKeys.RepoList] = McpRoles.Viewer,
            [McpActionKeys.RepoRead] = McpRoles.Viewer,
            [McpActionKeys.RepoWrite] = McpRoles.Admin,
            [McpActionKeys.ContextSearch] = McpRoles.Viewer,
            [McpActionKeys.ContextPack] = McpRoles.Viewer,
            [McpActionKeys.ContextSources] = McpRoles.Viewer,
            [McpActionKeys.ContextRebuildIndex] = McpRoles.Admin,
            [McpActionKeys.AuthConfigGet] = McpRoles.Viewer,
            [McpActionKeys.DiagnosticExecutionPath] = McpRoles.Viewer,
            [McpActionKeys.DiagnosticAppSettingsPath] = McpRoles.Viewer,
            [McpActionKeys.TodoList] = McpRoles.Viewer,
            [McpActionKeys.TodoGet] = McpRoles.Viewer,
            [McpActionKeys.TodoCreate] = McpRoles.Viewer,
            [McpActionKeys.TodoUpdate] = McpRoles.Viewer,
            [McpActionKeys.TodoDelete] = McpRoles.Viewer,
            [McpActionKeys.TodoRequirements] = McpRoles.Viewer,
            [McpActionKeys.TodoPromptStatus] = McpRoles.Viewer,
            [McpActionKeys.TodoPromptImplement] = McpRoles.Viewer,
            [McpActionKeys.TodoPromptPlan] = McpRoles.Viewer,
            ["agents.mutate"] = McpRoles.AgentManager,
        };

    /// <summary>Initializes a new instance of the policy service.</summary>
    /// <param name="roleContext">Current role context.</param>
    public DirectorAuthorizationPolicyService(IRoleContext roleContext)
    {
        _roleContext = roleContext;
    }

    /// <inheritdoc />
    public bool CanViewArea(McpArea area) => IsAllowed(GetRequiredRole(area));

    /// <inheritdoc />
    public bool CanExecuteAction(string actionKey) => IsAllowed(GetRequiredRole(actionKey));

    /// <inheritdoc />
    public string? GetRequiredRole(McpArea area)
        => AreaRoles.TryGetValue(area, out var role) ? role : McpRoles.Viewer;

    /// <inheritdoc />
    public string? GetRequiredRole(string actionKey)
    {
        if (string.IsNullOrWhiteSpace(actionKey))
            return null;

        return ActionRoles.TryGetValue(actionKey, out var role) ? role : McpRoles.Viewer;
    }

    private bool IsAllowed(string? requiredRole)
    {
        var normalizedRole = McpRoles.Normalize(requiredRole);
        if (string.IsNullOrEmpty(normalizedRole))
            return true;

        // Preserve existing API-key-only usage by treating unauthenticated users as viewer-equivalent
        // for view-level surfaces; higher-privilege areas still require explicit JWT roles.
        if (normalizedRole == McpRoles.Viewer)
            return true;

        if (normalizedRole == McpRoles.Admin)
            return _roleContext.HasRole(McpRoles.Admin);

        if (normalizedRole == McpRoles.AgentManager)
            return _roleContext.HasRole(McpRoles.AgentManager) || _roleContext.HasRole(McpRoles.Admin);

        return _roleContext.HasRole(normalizedRole);
    }
}
