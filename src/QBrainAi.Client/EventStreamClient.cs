using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using QBrainAi.Client.Models;
using System.Net.Http;

namespace QBrainAi.Client;

/// <summary>
/// Client for change-notification SSE endpoints (<c>/qbrainai/events</c>).
/// </summary>
/// <seealso cref="QBrainAiClient.Events"/>
public sealed class EventStreamClient : McpClientBase
{
    private static readonly JsonSerializerOptions s_jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = McpClientJsonContext.Default
    };

    /// <inheritdoc />
    public EventStreamClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options) { }

    internal EventStreamClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>
    /// Subscribes to the workspace change-event stream.
    /// </summary>
    /// <param name="category">Optional category filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>An async stream of <see cref="ChangeEvent"/> values.</returns>
    public async IAsyncEnumerable<ChangeEvent> SubscribeAsync(
        string? category = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var path = category is null
            ? "qbrainai/events"
            : $"qbrainai/events?category={Uri.EscapeDataString(category)}";

        ChangeEvent? connectionFailedEvent = null;
        IAsyncEnumerable<string>? sseStream = null;
        try
        {
            sseStream = StreamSseAsync(path, cancellationToken);
        }
        catch (Exception ex)
        {
            connectionFailedEvent = new ChangeEvent
            {
                Category = "connection",
                Action = "connection_failed",
                EntityId = ex.Message,
                Timestamp = DateTimeOffset.UtcNow
            };
        }

        if (connectionFailedEvent is not null)
        {
            yield return connectionFailedEvent;
            yield break;
        }

        await foreach (var payload in sseStream!.WithCancellation(cancellationToken))
        {
            ChangeEvent? changeEvent;
            try
            {
                changeEvent = (ChangeEvent?)JsonSerializer.Deserialize(payload, s_jsonOptions.GetTypeInfo(typeof(ChangeEvent)));
            }
            catch (JsonException)
            {
                continue;
            }

            if (changeEvent is not null)
                yield return changeEvent;
        }
    }
}
