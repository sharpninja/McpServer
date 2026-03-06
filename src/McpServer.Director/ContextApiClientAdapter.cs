using McpServer.Client;
using McpServer.Client.Models;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>Director adapter for <see cref="IContextApiClient"/>.</summary>
internal sealed class ContextApiClientAdapter : IContextApiClient
{
    private readonly DirectorMcpContext _context;

    public ContextApiClientAdapter(DirectorMcpContext context)
    {
        _context = context;
    }

    public async Task<ContextSearchPayload> SearchAsync(SearchContextQuery query, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Context.SearchAsync(
            query.Query,
            query.SourceType,
            query.Limit <= 0 ? 20 : query.Limit,
            cancellationToken).ConfigureAwait(true);
        return new ContextSearchPayload(
            result.Query,
            result.Chunks.Select(MapChunk).ToList(),
            result.SourceKeys?.ToList() ?? []);
    }

    public async Task<ContextPackPayload> PackAsync(PackContextQuery query, CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Context.PackAsync(
            query.Query,
            query.QueryId,
            query.Limit <= 0 ? 20 : query.Limit,
            cancellationToken).ConfigureAwait(true);
        return new ContextPackPayload(
            result.QueryId,
            result.Chunks.Select(MapChunk).ToList(),
            result.SourceKeys?.ToList() ?? []);
    }

    public async Task<ContextSourcesPayload> ListSourcesAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Context.ListSourcesAsync(cancellationToken).ConfigureAwait(true);
        return new ContextSourcesPayload(
            result.Sources.Select(s => new ContextSourceView(s.SourceKey, s.SourceType, s.IngestedAt)).ToList());
    }

    public async Task<ContextRebuildResult> RebuildIndexAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredActiveWorkspaceApiClientAsync(cancellationToken).ConfigureAwait(true);
        var result = await client.Context.RebuildIndexAsync(cancellationToken).ConfigureAwait(true);
        return new ContextRebuildResult(result.Status, DateTimeOffset.UtcNow);
    }

    private static ContextChunkView MapChunk(ContextChunkResult c)
        => new(c.Id, c.DocumentId, c.Content, c.TokenCount, c.ChunkIndex, c.Score);
}
