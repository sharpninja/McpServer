using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="ISessionLogApiClient"/> backed by the selected workspace
/// <see cref="McpServer.Client.McpServerClient"/> in <see cref="DirectorMcpContext"/>.
/// </summary>
internal sealed class SessionLogApiClientAdapter : ISessionLogApiClient
{
    private readonly DirectorMcpContext _context;

    /// <summary>Initializes a new adapter instance.</summary>
    /// <param name="context">Director connection context.</param>
    public SessionLogApiClientAdapter(DirectorMcpContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task<ListSessionLogsResult> ListSessionLogsAsync(ListSessionLogsQuery query, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(false);
        var result = await client.SessionLog.QueryAsync(
            agent: string.IsNullOrWhiteSpace(query.Agent) ? null : query.Agent,
            model: string.IsNullOrWhiteSpace(query.Model) ? null : query.Model,
            text: string.IsNullOrWhiteSpace(query.Text) ? null : query.Text,
            limit: Math.Max(1, query.Limit),
            offset: Math.Max(0, query.Offset),
            cancellationToken: cancellationToken).ConfigureAwait(false);

        var items = result.Items
            .Select(MapItem)
            .ToList();

        var totalCount = result.TotalCount;
        var limit = result.Limit <= 0 ? Math.Max(1, query.Limit) : result.Limit;
        var offset = result.Offset < 0 ? Math.Max(0, query.Offset) : result.Offset;
        return new ListSessionLogsResult(items, totalCount, limit, offset);
    }

    private static SessionLogSummary MapItem(UnifiedSessionLogDto item)
        => new(
            SessionId: item.SessionId ?? string.Empty,
            SourceType: item.SourceType ?? string.Empty,
            Title: item.Title ?? string.Empty,
            Status: item.Status ?? string.Empty,
            Model: item.Model,
            Started: item.Started,
            LastUpdated: item.LastUpdated,
            EntryCount: item.EntryCount);
}
