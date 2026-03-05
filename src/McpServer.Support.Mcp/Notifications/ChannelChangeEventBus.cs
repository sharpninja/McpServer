using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;

namespace McpServer.Support.Mcp.Notifications;

/// <summary>
/// Fan-out event bus backed by <see cref="Channel{T}"/>. Each subscriber
/// gets its own bounded channel; publish writes to all subscriber channels.
/// </summary>
public sealed class ChannelChangeEventBus : IChangeEventBus
{
    private const int MaxBufferSize = 1000;

    private readonly ConcurrentDictionary<Guid, Channel<ChangeEvent>> _subscribers = new();

    /// <inheritdoc />
    public ValueTask PublishAsync(ChangeEvent changeEvent, CancellationToken ct = default)
    {
        foreach (var kvp in _subscribers)
        {
            // Non-blocking write; drops if subscriber is full (BoundedChannelFullMode.DropOldest).
            kvp.Value.Writer.TryWrite(changeEvent);
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
                FullMode = BoundedChannelFullMode.DropOldest,
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
