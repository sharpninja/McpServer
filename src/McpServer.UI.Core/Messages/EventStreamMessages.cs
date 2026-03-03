using McpServer.Cqrs;

namespace McpServer.UI.Core.Messages;

/// <summary>Query to subscribe to workspace change events via SSE.</summary>
public sealed record SubscribeToEventsQuery(string? Category = null) : IQuery<IAsyncEnumerable<ChangeEventItem>>;

/// <summary>Change event item surfaced to UI/Core consumers.</summary>
public sealed record ChangeEventItem(
    string Category,
    string Action,
    string? EntityId,
    string? ResourceUri,
    DateTimeOffset Timestamp);
