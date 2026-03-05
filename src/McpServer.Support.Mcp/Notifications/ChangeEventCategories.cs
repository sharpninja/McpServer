namespace McpServer.Support.Mcp.Notifications;

/// <summary>Well-known change event categories.</summary>
public static class ChangeEventCategories
{
    /// <summary>TODO item changes.</summary>
    public const string Todo = "todo";

    /// <summary>Session log changes.</summary>
    public const string SessionLog = "session_log";

    /// <summary>Repository file changes.</summary>
    public const string Repo = "repo";

    /// <summary>Context index changes (sync/rebuild).</summary>
    public const string Context = "context";

    /// <summary>Tool definition changes.</summary>
    public const string ToolRegistry = "tool_registry";

    /// <summary>Tool bucket changes.</summary>
    public const string ToolBucket = "tool_bucket";

    /// <summary>Workspace configuration changes.</summary>
    public const string Workspace = "workspace";

    /// <summary>GitHub issue/PR changes.</summary>
    public const string GitHub = "github";

    /// <summary>Marker file regeneration.</summary>
    public const string Marker = "marker";

    /// <summary>Agent definitions and workspace agent state changes.</summary>
    public const string Agent = "agent";

    /// <summary>Requirements document and mapping changes.</summary>
    public const string Requirements = "requirements";

    /// <summary>Connection lifecycle events (connected, disconnected).</summary>
    public const string Connection = "connection";
}
