using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Abstraction over event-stream subscriptions used by UI.Core CQRS handlers.
/// </summary>
public interface IEventStreamApiClient
{
    /// <summary>Subscribes to workspace change events.</summary>
    Task<IAsyncEnumerable<ChangeEventItem>> SubscribeAsync(string? category = null, CancellationToken cancellationToken = default);
}
