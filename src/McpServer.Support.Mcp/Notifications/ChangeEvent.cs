namespace McpServer.Support.Mcp.Notifications;

/// <summary>Domain event emitted when workspace data changes.</summary>
public sealed record ChangeEvent
{
    /// <summary>Event category (e.g. "todo", "session_log", "repo").</summary>
    public required string Category { get; init; }

    /// <summary>Mutation action: "created", "updated", or "deleted".</summary>
    public required string Action { get; init; }

    /// <summary>Optional entity identifier (e.g. TODO id, file path).</summary>
    public string? EntityId { get; init; }

    /// <summary>MCP resource URI (e.g. "mcp://workspace/todo/MVP-APP-001").</summary>
    public string? ResourceUri { get; init; }

    /// <summary>When the event occurred.</summary>
    public DateTimeOffset Timestamp { get; init; } = DateTimeOffset.UtcNow;
}
