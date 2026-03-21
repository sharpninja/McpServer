using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Notifications;

/// <summary>
/// TR-MCP-EVT-001: Fan-out event bus backed by <see cref="Channel{T}"/>. Each subscriber gets
/// its own bounded channel; publish remains non-blocking by rejecting writes to full subscriber
/// buffers so overflow is logged instead of silently discarding older events.
/// </summary>
public sealed class ChannelChangeEventBus : IChangeEventBus
{
    private const int MaxBufferSize = 1000;

    private readonly ConcurrentDictionary<Guid, Channel<ChangeEvent>> _subscribers = new();
    private readonly ILogger<ChannelChangeEventBus> _logger;
    private long _rejectedSubscriberWrites;

    /// <summary>TR-MCP-EVT-001: Constructor.</summary>
    /// <param name="logger">Logger instance.</param>
    public ChannelChangeEventBus(ILogger<ChannelChangeEventBus> logger)
    {
        _logger = logger;
    }

    /// <inheritdoc />
    public ValueTask PublishAsync(ChangeEvent changeEvent, CancellationToken ct = default)
    {
        var rejectedSubscribers = 0;
        foreach (var kvp in _subscribers)
        {
            if (!kvp.Value.Writer.TryWrite(changeEvent))
            {
                rejectedSubscribers++;
            }
        }

        if (rejectedSubscribers > 0)
        {
            var totalRejectedWrites = Interlocked.Add(ref _rejectedSubscriberWrites, rejectedSubscribers);
            _logger.LogWarning(
                "Change event delivery rejected {RejectedSubscriberCount} subscriber writes for {Category}/{Action} entity {EntityId} because subscriber buffers are full. TotalRejectedSubscriberWrites={TotalRejectedSubscriberWrites}",
                rejectedSubscribers,
                changeEvent.Category,
                changeEvent.Action,
                changeEvent.EntityId ?? "(none)",
                totalRejectedWrites);
        }

        return ValueTask.CompletedTask;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChangeEvent> SubscribeAsync(
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        var id = Guid.NewGuid();
        var channel = Channel.CreateBounded<ChangeEvent>(
            new BoundedChannelOptions(MaxBufferSize)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleWriter = false,
                SingleReader = true,
            });

        _subscribers.TryAdd(id, channel);
        try
        {
            await foreach (var evt in channel.Reader.ReadAllAsync(ct).ConfigureAwait(false))
            {
                yield return evt;
            }
        }
        finally
        {
            _subscribers.TryRemove(id, out _);
        }
    }
}
