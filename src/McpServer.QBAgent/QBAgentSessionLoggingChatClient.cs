using Microsoft.Extensions.AI;

namespace McpServer.QBAgent;

/// <summary>Holds the current JSONL log so <c>new session</c> can swap files.</summary>
internal sealed class QBAgentSessionLogBox(QBAgentSessionLog current)
{
    public QBAgentSessionLog Current { get; set; } = current;
}

/// <summary>Appends every chat message (user, assistant, tool) to the JSONL session log.</summary>
internal sealed class QBAgentSessionLoggingChatClient(IChatClient inner, QBAgentSessionLogBox log) : IChatClient
{
    /// <inheritdoc />
    public async Task<ChatResponse> GetResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        var list = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        foreach (var message in list)
            log.Current.AppendChatMessage(message);

        var response = await inner.GetResponseAsync(list, options, cancellationToken).ConfigureAwait(false);
        foreach (var message in response.Messages)
            log.Current.AppendChatMessage(message);
        return response;
    }

    /// <inheritdoc />
    public async IAsyncEnumerable<ChatResponseUpdate> GetStreamingResponseAsync(
        IEnumerable<ChatMessage> messages,
        ChatOptions? options = null,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var list = messages as IReadOnlyList<ChatMessage> ?? messages.ToList();
        foreach (var message in list)
            log.Current.AppendChatMessage(message);

        await foreach (var update in inner.GetStreamingResponseAsync(list, options, cancellationToken).ConfigureAwait(false))
        {
            if (update.Role is not null && update.Contents.Count > 0)
            {
                var text = string.Concat(update.Contents.OfType<TextContent>().Select(static part => part.Text));
                if (!string.IsNullOrEmpty(text))
                    log.Current.Append(update.Role.Value.Value, text);
            }

            yield return update;
        }
    }

    /// <inheritdoc />
    public object? GetService(Type serviceType, object? serviceKey = null)
        => inner.GetService(serviceType, serviceKey);

    /// <inheritdoc />
    public void Dispose() => inner.Dispose();
}
