using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;
using McpServer.UI.Core.Models;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Bridges <see cref="McpAgentEventStreamService"/> to UI.Core's <see cref="IEventStreamApiClient"/>
/// so that <c>EventStreamViewModel</c> can subscribe to change events from the app-owned service.
/// </summary>
internal sealed class UiCoreEventStreamApiClientAdapter : IEventStreamApiClient
{
    private readonly McpAgentEventStreamService _service;

    public UiCoreEventStreamApiClientAdapter(McpAgentEventStreamService service)
    {
        _service = service ?? throw new ArgumentNullException(nameof(service));
    }

    public Task<IAsyncEnumerable<ChangeEventItem>> SubscribeAsync(
        string? category = null,
        CancellationToken cancellationToken = default)
    {
        IAsyncEnumerable<ChangeEventItem> stream = MapStream(_service.StreamEventsAsync(category, cancellationToken), cancellationToken);
        return Task.FromResult(stream);
    }

    private static async IAsyncEnumerable<ChangeEventItem> MapStream(
        IAsyncEnumerable<McpIncomingChangeEvent> source,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        await foreach (var evt in source.WithCancellation(cancellationToken))
        {
            yield return new ChangeEventItem(
                evt.Category ?? string.Empty,
                evt.Action ?? string.Empty,
                evt.EntityId,
                evt.ResourceUri,
                evt.Timestamp ?? DateTimeOffset.UtcNow);
        }
    }
}

