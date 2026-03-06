using McpServer.Client;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="IEventStreamApiClient"/> backed by <see cref="McpServerClient"/>.
/// </summary>
internal sealed class EventStreamApiClientAdapter : IEventStreamApiClient
{
    private readonly DirectorMcpContext _context;

    public EventStreamApiClientAdapter(
        DirectorMcpContext context)
    {
        _context = context;
    }

    public async Task<IAsyncEnumerable<ChangeEventItem>> SubscribeAsync(string? category = null, CancellationToken cancellationToken = default)
    {
        var client = await GetClientAsync(cancellationToken).ConfigureAwait(true);
        var stream = client.Events.SubscribeAsync(category, cancellationToken);
        return MapAsync(stream);
    }

    private async Task<McpServerClient> GetClientAsync(CancellationToken cancellationToken)
    {
        if (_context.HasControlConnection)
            return await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(true);
        return await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(true);
    }

    private static async IAsyncEnumerable<ChangeEventItem> MapAsync(IAsyncEnumerable<McpServer.Client.Models.ChangeEvent> stream)
    {
        await foreach (var item in stream)
        {
            yield return new ChangeEventItem(
                item.Category,
                item.Action,
                item.EntityId,
                item.ResourceUri,
                item.Timestamp);
        }
    }
}
