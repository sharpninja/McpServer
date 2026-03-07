using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// TR-MCP-INGEST-003: Fetches remote website content and emits normalized context documents/chunks.
/// </summary>
public interface IWebsiteIngestor
{
    /// <summary>
    /// Ingests one website URL and optional same-host subpages.
    /// </summary>
    /// <param name="request">Website ingestion request.</param>
    /// <param name="onPageFetched">Optional callback invoked after each page fetch completes.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Fetched pages with generated documents/chunks and per-URL outcomes.</returns>
    Task<IReadOnlyList<WebsiteIngestPage>> IngestAsync(
        WebsiteIngestRequest request,
        Func<WebsiteIngestPage, Task>? onPageFetched = null,
        CancellationToken cancellationToken = default);
}

/// <summary>
/// TR-MCP-INGEST-003: A fetched page converted to context primitives.
/// </summary>
public sealed class WebsiteIngestPage
{
    /// <summary>Canonical URL.</summary>
    public required string Url { get; init; }

    /// <summary>Outcome details for this URL.</summary>
    public required WebsiteIngestUrlResult Outcome { get; init; }

    /// <summary>Generated context document when status is <c>ingested</c>.</summary>
    public ContextDocument? Document { get; init; }

    /// <summary>Generated chunks when status is <c>ingested</c>.</summary>
    public IReadOnlyList<ContextChunk> Chunks { get; init; } = [];

    /// <summary>Discovered links extracted from HTML content.</summary>
    public IReadOnlyList<string> DiscoveredLinks { get; init; } = [];
}
