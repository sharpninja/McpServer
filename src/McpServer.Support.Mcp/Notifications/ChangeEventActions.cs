namespace McpServer.Support.Mcp.Notifications;

/// <summary>Well-known change event actions.</summary>
public static class ChangeEventActions
{
    /// <summary>Entity was created.</summary>
    public const string Created = "created";

    /// <summary>Entity was updated.</summary>
    public const string Updated = "updated";

    /// <summary>Entity was deleted.</summary>
    public const string Deleted = "deleted";

    /// <summary>Stream connection established.</summary>
    public const string Connected = "connected";

    /// <summary>Stream connection failed.</summary>
    public const string ConnectionFailed = "connection_failed";
}
