namespace McpServer.Support.Mcp.Notifications;

/// <summary>In-process pub/sub for domain change events.</summary>
public interface IChangeEventBus
{
    /// <summary>Publish a change event to all subscribers.</summary>
    ValueTask PublishAsync(ChangeEvent changeEvent, CancellationToken ct = default);

    /// <summary>Subscribe to change events. Each call creates an independent subscriber.</summary>
    IAsyncEnumerable<ChangeEvent> SubscribeAsync(CancellationToken ct = default);
}
