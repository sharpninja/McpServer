using McpServer.UI.Core.Authorization;

namespace McpServer.Director.Auth;

/// <summary>
/// Director RBAC policy implementation for tab visibility and command authorization.
/// Uses JWT-derived roles via <see cref="IRoleContext"/>.
/// </summary>
internal sealed class DirectorAuthorizationPolicyService : IAuthorizationPolicyService
{
    private readonly IRoleContext _roleContext;

    private static readonly IReadOnlyDictionary<McpArea, string> s_areaRoles = new Dictionary<McpArea, string>
    {
        [McpArea.Health] = McpRoles.Viewer,
        [McpArea.Workspaces] = McpRoles.Admin,
        [McpArea.Policy] = McpRoles.Admin,
        [McpArea.Agents] = McpRoles.AgentManager,
        [McpArea.Todo] = McpRoles.Viewer,
        [McpArea.SessionLogs] = McpRoles.Viewer,
        [McpArea.DispatcherLogs] = McpRoles.Viewer,
        [McpArea.Context] = McpRoles.Viewer,
        [McpArea.Repo] = McpRoles.Viewer,
        [McpArea.ToolRegistry] = McpRoles.Viewer,
        [McpArea.GitHub] = McpRoles.Viewer,
        [McpArea.Requirements] = McpRoles.Viewer,
        [McpArea.Voice] = McpRoles.Viewer,
        [McpArea.Events] = McpRoles.Viewer,
        [McpArea.Diagnostic] = McpRoles.Viewer,
        [McpArea.AuthConfig] = McpRoles.Viewer,
        [McpArea.Templates] = McpRoles.Viewer,
    };

    private static readonly IReadOnlyDictionary<string, string> s_actionRoles =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            [McpActionKeys.WorkspaceList] = McpRoles.Admin,
            [McpActionKeys.WorkspaceGet] = McpRoles.Admin,
            [McpActionKeys.WorkspaceUpdatePolicy] = McpRoles.Admin,
            [McpActionKeys.WorkspaceInit] = McpRoles.Admin,
            [McpActionKeys.WorkspaceCreate] = McpRoles.Admin,
            [McpActionKeys.WorkspaceUpdate] = McpRoles.Admin,
            [McpActionKeys.WorkspaceDelete] = McpRoles.Admin,
            [McpActionKeys.WorkspaceStart] = McpRoles.Admin,
            [McpActionKeys.WorkspaceStop] = McpRoles.Admin,
            [McpActionKeys.WorkspaceStatus] = McpRoles.Admin,
            [McpActionKeys.WorkspacePromptGet] = McpRoles.Admin,
            [McpActionKeys.WorkspacePromptUpdate] = McpRoles.Admin,
            [McpActionKeys.SessionLogQuery] = McpRoles.Viewer,
            [McpActionKeys.SessionLogSubmit] = McpRoles.Viewer,
            [McpActionKeys.SessionLogAppendDialog] = McpRoles.Viewer,
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
            [McpActionKeys.AgentDefinitionList] = McpRoles.AgentManager,
            [McpActionKeys.AgentDefinitionGet] = McpRoles.AgentManager,
            [McpActionKeys.AgentDefinitionUpsert] = McpRoles.AgentManager,
            [McpActionKeys.AgentDefinitionDelete] = McpRoles.AgentManager,
            [McpActionKeys.AgentDefinitionSeed] = McpRoles.AgentManager,
            [McpActionKeys.AgentWorkspaceList] = McpRoles.AgentManager,
            [McpActionKeys.AgentWorkspaceGet] = McpRoles.AgentManager,
            [McpActionKeys.AgentWorkspaceAssign] = McpRoles.AgentManager,
            [McpActionKeys.AgentWorkspaceDelete] = McpRoles.AgentManager,
            [McpActionKeys.AgentBan] = McpRoles.AgentManager,
            [McpActionKeys.AgentUnban] = McpRoles.AgentManager,
            [McpActionKeys.AgentEventLog] = McpRoles.AgentManager,
            [McpActionKeys.AgentEventList] = McpRoles.AgentManager,
            [McpActionKeys.AgentValidate] = McpRoles.AgentManager,
            [McpActionKeys.AgentPoolAgentsList] = McpRoles.AgentManager,
            [McpActionKeys.AgentPoolQueueList] = McpRoles.AgentManager,
            [McpActionKeys.AgentPoolAgentStart] = McpRoles.AgentManager,
            [McpActionKeys.AgentPoolAgentStop] = McpRoles.AgentManager,
            [McpActionKeys.AgentPoolAgentRecycle] = McpRoles.AgentManager,
            [McpActionKeys.AgentPoolAgentConnect] = McpRoles.AgentManager,
            [McpActionKeys.AgentPoolQueueCancel] = McpRoles.AgentManager,
            [McpActionKeys.AgentPoolQueueRemove] = McpRoles.AgentManager,
            [McpActionKeys.AgentPoolQueueMove] = McpRoles.AgentManager,
            [McpActionKeys.AgentPoolQueueResolve] = McpRoles.AgentManager,
            [McpActionKeys.AgentPoolQueueEnqueue] = McpRoles.AgentManager,
            [McpActionKeys.TemplateList] = McpRoles.Viewer,
            [McpActionKeys.TemplateGet] = McpRoles.Viewer,
            [McpActionKeys.TemplateTest] = McpRoles.Viewer,
            [McpActionKeys.TemplateCreate] = McpRoles.Admin,
            [McpActionKeys.TemplateUpdate] = McpRoles.Admin,
            [McpActionKeys.TemplateDelete] = McpRoles.Admin,
            [McpActionKeys.ToolRegistryList] = McpRoles.Viewer,
            [McpActionKeys.ToolRegistrySearch] = McpRoles.Viewer,
            [McpActionKeys.ToolRegistryGet] = McpRoles.Viewer,
            [McpActionKeys.ToolRegistryMutate] = McpRoles.Admin,
            [McpActionKeys.ToolRegistryBucketList] = McpRoles.Viewer,
            [McpActionKeys.ToolRegistryBucketBrowse] = McpRoles.Viewer,
            [McpActionKeys.ToolRegistryBucketMutate] = McpRoles.Admin,
            [McpActionKeys.GitHubIssueList] = McpRoles.Viewer,
            [McpActionKeys.GitHubIssueGet] = McpRoles.Viewer,
            [McpActionKeys.GitHubIssueMutate] = McpRoles.Admin,
            [McpActionKeys.GitHubLabelList] = McpRoles.Viewer,
            [McpActionKeys.GitHubPullList] = McpRoles.Viewer,
            [McpActionKeys.GitHubPullComment] = McpRoles.Admin,
            [McpActionKeys.GitHubSync] = McpRoles.Admin,
            [McpActionKeys.RequirementsRead] = McpRoles.Viewer,
            [McpActionKeys.RequirementsWrite] = McpRoles.Admin,
            [McpActionKeys.RequirementsGenerate] = McpRoles.Viewer,
            [McpActionKeys.VoiceCreateSession] = McpRoles.Viewer,
            [McpActionKeys.VoiceSubmitTurn] = McpRoles.Viewer,
            [McpActionKeys.VoiceInterrupt] = McpRoles.Viewer,
            [McpActionKeys.VoiceStatus] = McpRoles.Viewer,
            [McpActionKeys.VoiceTranscript] = McpRoles.Viewer,
            [McpActionKeys.VoiceDeleteSession] = McpRoles.Viewer,
            [McpActionKeys.EventsSubscribe] = McpRoles.Viewer,
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
        => s_areaRoles.TryGetValue(area, out var role) ? role : McpRoles.Viewer;

    /// <inheritdoc />
    public string? GetRequiredRole(string actionKey)
    {
        if (string.IsNullOrWhiteSpace(actionKey))
            return null;

        return s_actionRoles.TryGetValue(actionKey, out var role) ? role : McpRoles.Viewer;
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
