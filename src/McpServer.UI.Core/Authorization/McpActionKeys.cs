namespace McpServer.UI.Core.Authorization;

/// <summary>
/// Shared action-key constants for UI/Core authorization checks.
/// Hosts can map these keys to role policies, and handlers/ViewModels can reference them without string duplication.
/// </summary>
public static class McpActionKeys
{
    /// <summary>Workspace list query action.</summary>
    public const string WorkspaceList = "workspace.list";

    /// <summary>Workspace detail query action.</summary>
    public const string WorkspaceGet = "workspace.get";

    /// <summary>Workspace policy update action.</summary>
    public const string WorkspaceUpdatePolicy = "workspace.update-policy";

    /// <summary>Workspace initialization action.</summary>
    public const string WorkspaceInit = "workspace.init";

    /// <summary>Session-log query action.</summary>
    public const string SessionLogQuery = "sessionlog.query";

    /// <summary>TODO list query action.</summary>
    public const string TodoList = "todo.list";

    /// <summary>TODO detail query action.</summary>
    public const string TodoGet = "todo.get";

    /// <summary>TODO create action.</summary>
    public const string TodoCreate = "todo.create";

    /// <summary>TODO update action.</summary>
    public const string TodoUpdate = "todo.update";

    /// <summary>TODO delete action.</summary>
    public const string TodoDelete = "todo.delete";

    /// <summary>TODO requirements analysis action.</summary>
    public const string TodoRequirements = "todo.requirements";

    /// <summary>TODO status prompt generation action.</summary>
    public const string TodoPromptStatus = "todo.prompt.status";

    /// <summary>TODO implement prompt generation action.</summary>
    public const string TodoPromptImplement = "todo.prompt.implement";

    /// <summary>TODO plan prompt generation action.</summary>
    public const string TodoPromptPlan = "todo.prompt.plan";

    /// <summary>Repo list action.</summary>
    public const string RepoList = "repo.list";

    /// <summary>Repo read action.</summary>
    public const string RepoRead = "repo.read";

    /// <summary>Repo write action.</summary>
    public const string RepoWrite = "repo.write";

    /// <summary>Context search action.</summary>
    public const string ContextSearch = "context.search";

    /// <summary>Context pack action.</summary>
    public const string ContextPack = "context.pack";

    /// <summary>Context sources action.</summary>
    public const string ContextSources = "context.sources";

    /// <summary>Context rebuild-index action.</summary>
    public const string ContextRebuildIndex = "context.rebuild-index";

    /// <summary>Auth config query action.</summary>
    public const string AuthConfigGet = "auth.config.get";

    /// <summary>Diagnostic execution-path query action.</summary>
    public const string DiagnosticExecutionPath = "diagnostic.execution-path";

    /// <summary>Diagnostic appsettings-path query action.</summary>
    public const string DiagnosticAppSettingsPath = "diagnostic.appsettings-path";

    /// <summary>Tunnel list query action.</summary>
    public const string TunnelList = "tunnel.list";

    /// <summary>Tunnel enable action.</summary>
    public const string TunnelEnable = "tunnel.enable";

    /// <summary>Tunnel disable action.</summary>
    public const string TunnelDisable = "tunnel.disable";

    /// <summary>Tunnel start action.</summary>
    public const string TunnelStart = "tunnel.start";

    /// <summary>Tunnel stop action.</summary>
    public const string TunnelStop = "tunnel.stop";

    /// <summary>Tunnel restart action.</summary>
    public const string TunnelRestart = "tunnel.restart";

    /// <summary>Agent-definition list action.</summary>
    public const string AgentDefinitionList = "agent.definition.list";

    /// <summary>Agent-definition detail action.</summary>
    public const string AgentDefinitionGet = "agent.definition.get";

    /// <summary>Agent-definition upsert action.</summary>
    public const string AgentDefinitionUpsert = "agent.definition.upsert";

    /// <summary>Workspace-agent assignment action.</summary>
    public const string AgentWorkspaceAssign = "agent.workspace.assign";

    /// <summary>Agent-pool runtime list action.</summary>
    public const string AgentPoolAgentsList = "agentpool.agents.list";

    /// <summary>Agent-pool queue list action.</summary>
    public const string AgentPoolQueueList = "agentpool.queue.list";

    /// <summary>Agent-pool start action.</summary>
    public const string AgentPoolAgentStart = "agentpool.agent.start";

    /// <summary>Agent-pool stop action.</summary>
    public const string AgentPoolAgentStop = "agentpool.agent.stop";

    /// <summary>Agent-pool recycle action.</summary>
    public const string AgentPoolAgentRecycle = "agentpool.agent.recycle";

    /// <summary>Agent-pool connect action.</summary>
    public const string AgentPoolAgentConnect = "agentpool.agent.connect";

    /// <summary>Agent-pool queue cancel action.</summary>
    public const string AgentPoolQueueCancel = "agentpool.queue.cancel";

    /// <summary>Agent-pool queue remove action.</summary>
    public const string AgentPoolQueueRemove = "agentpool.queue.remove";

    /// <summary>Agent-pool queue reorder action.</summary>
    public const string AgentPoolQueueMove = "agentpool.queue.move";

    /// <summary>Agent-pool prompt resolve action.</summary>
    public const string AgentPoolQueueResolve = "agentpool.queue.resolve";

    /// <summary>Agent-pool enqueue action.</summary>
    public const string AgentPoolQueueEnqueue = "agentpool.queue.enqueue";

    /// <summary>Template list query action.</summary>
    public const string TemplateList = "template.list";

    /// <summary>Template detail query action.</summary>
    public const string TemplateGet = "template.get";

    /// <summary>Template create action.</summary>
    public const string TemplateCreate = "template.create";

    /// <summary>Template update action.</summary>
    public const string TemplateUpdate = "template.update";

    /// <summary>Template delete action.</summary>
    public const string TemplateDelete = "template.delete";

    /// <summary>Template test/render action.</summary>
    public const string TemplateTest = "template.test";
}
