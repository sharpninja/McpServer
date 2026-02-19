using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-PLANNED-013: Channel for enqueueing interaction log entries to be submitted asynchronously to a logging service.
/// </summary>
public interface IInteractionLogSubmissionChannel
{
    /// <summary>Attempts to enqueue an entry without blocking. Returns false if channel is full.</summary>
    /// <param name="entry">The interaction log entry to enqueue.</param>
    /// <returns><see langword="true"/> if the entry was enqueued; <see langword="false"/> if the channel is full.</returns>
    bool TryEnqueue(InteractionLogEntry entry);

    /// <summary>Attempts to dequeue the next entry. Returns (true, entry) or (false, null). Used by the background submission service.</summary>
    /// <param name="cancellationToken">Token to cancel the wait.</param>
    /// <returns>A tuple indicating success and the dequeued entry (or <see langword="null"/>).</returns>
    ValueTask<(bool Success, InteractionLogEntry? Entry)> TryDequeueAsync(CancellationToken cancellationToken = default);
}
