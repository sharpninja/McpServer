using System.Threading.Channels;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Channel-based buffer for interaction log entries. Non-blocking enqueue; async dequeue for background submission.
/// </summary>
public sealed class InteractionLogSubmissionChannel : IInteractionLogSubmissionChannel
{
    private readonly Channel<InteractionLogEntry> _channel;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    /// <param name="options">Interaction logging options providing queue capacity.</param>
    public InteractionLogSubmissionChannel(IOptions<McpInteractionLoggingOptions> options)
    {
        var capacity = options?.Value?.QueueCapacity ?? 1000;
        _channel = Channel.CreateBounded<InteractionLogEntry>(new BoundedChannelOptions(capacity)
        {
            FullMode = BoundedChannelFullMode.DropOldest
        });
    }

    /// <inheritdoc />
    public bool TryEnqueue(InteractionLogEntry entry)
    {
        return _channel.Writer.TryWrite(entry);
    }

    /// <inheritdoc />
    public async ValueTask<(bool Success, InteractionLogEntry? Entry)> TryDequeueAsync(CancellationToken cancellationToken = default)
    {
        if (_channel.Reader.TryRead(out var entry))
            return (true, entry);
        try
        {
            if (await _channel.Reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
            {
                if (_channel.Reader.TryRead(out entry))
                    return (true, entry);
            }
        }
        catch (OperationCanceledException)
        {
            // Expected on shutdown
        }

        return (false, null);
    }
}
