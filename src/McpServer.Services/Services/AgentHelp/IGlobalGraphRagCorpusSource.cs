namespace McpServer.Support.Mcp.Services.AgentHelp;

/// <summary>
/// TR-MCP-GRAPHRAG-GLOBAL-001: Supplies canonical McpServer context excerpts from the host-global GraphRAG corpus.
/// </summary>
public interface IGlobalGraphRagCorpusSource
{
    /// <summary>
    /// Queries the global GraphRAG corpus for excerpts relevant to the supplied search text.
    /// </summary>
    /// <param name="query">Search text.</param>
    /// <param name="maxChunks">Maximum excerpts to return.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Ordered source/text pairs, or an empty list when global GraphRAG is unavailable.</returns>
    Task<IReadOnlyList<GlobalGraphRagCorpusExcerpt>> QueryAsync(
        string query,
        int maxChunks,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-GRAPHRAG-GLOBAL-001: A single excerpt from the global GraphRAG corpus.
/// </summary>
/// <param name="SourceKey">Display source key for the excerpt.</param>
/// <param name="Text">Excerpt text.</param>
public sealed record GlobalGraphRagCorpusExcerpt(string SourceKey, string Text);