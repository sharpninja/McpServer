using Microsoft.Extensions.AI;

namespace McpServer.QBAgent;

/// <summary>Forwards chat calls to QuadBrain and emits progress lines around each round-trip.</summary>
internal sealed class QBAgentProgressChatClient(IChatClient inner, Action<string> progress) : IChatClient
{
    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        progress(HasPendingToolHandback(list) ? "feeding tool result to QuadBrain" : "calling QuadBrain");
        try
        {
            return await inner.GetResponseAsync(list, options, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            progress("QuadBrain returned");
        }
    }

    /// <inheritdoc />
    public IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        progress("calling QuadBrain");
        return inner.GetStreamingResponseAsync(messages, options, cancellationToken);
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => inner.GetService(serviceType, serviceKey);

    /// <inheritdoc />
    public void Dispose() => inner.Dispose();

    /// <summary>
    /// True only when the newest messages are still tool results (a live handback), not when older
    /// tool results sit in session history under a later user/assistant turn.
    /// </summary>
    internal static bool HasPendingToolHandback(IReadOnlyList<ChatMessage> messages)
    {
        for (var i = messages.Count - 1; i >= 0; i--)
        {
            var message = messages[i];
            if (message.Contents.OfType<FunctionResultContent>().Any())
                return true;
            if (message.Role == ChatRole.User || message.Role == ChatRole.Assistant)
                return false;
        }

        return false;
    }
}
