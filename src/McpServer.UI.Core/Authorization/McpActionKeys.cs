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

    /// <summary>Sync run action.</summary>
    public const string SyncRun = "sync.run";

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
}
