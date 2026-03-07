using System.ComponentModel.DataAnnotations;

namespace McpServer.Support.Mcp.Models;

/// <summary>
/// FR-MCP-065, TR-MCP-INGEST-003: Request contract for direct website URL ingestion.
/// </summary>
public sealed class WebsiteIngestRequest
{
    /// <summary>Primary URL to ingest.</summary>
    [Required]
    public string Url { get; set; } = string.Empty;

    /// <summary>Whether to crawl discovered subpages on the same host.</summary>
    public bool IncludeSubpages { get; set; }

    /// <summary>Maximum pages to fetch for this request.</summary>
    [Range(1, int.MaxValue)]
    public int MaxPages { get; set; } = 20;

    /// <summary>Maximum crawl depth when <see cref="IncludeSubpages"/> is true.</summary>
    [Range(0, 10)]
    public int MaxDepth { get; set; } = 1;

    /// <summary>Maximum bytes to download per page.</summary>
    [Range(4096, 10_485_760)]
    public int MaxBytesPerPage { get; set; } = 262_144;

    /// <summary>Forces refresh semantics for existing indexed source keys.</summary>
    public bool ForceRefresh { get; set; }

    /// <summary>Runs GraphRAG indexing after successful page ingestion.</summary>
    public bool TriggerGraphRagIndex { get; set; }
}

/// <summary>
/// FR-MCP-065, TR-MCP-INGEST-003: Per-URL ingestion outcome information.
/// </summary>
public sealed class WebsiteIngestUrlResult
{
    /// <summary>Input or canonical URL associated with this result.</summary>
    public string Url { get; set; } = string.Empty;

    /// <summary>Outcome status: <c>ingested</c>, <c>skipped</c>, or <c>error</c>.</summary>
    public string Status { get; set; } = "error";

    /// <summary>Document source key written for successful ingestion.</summary>
    public string? SourceKey { get; set; }

    /// <summary>Human-readable details for skip/error outcomes.</summary>
    public string? Message { get; set; }

    /// <summary>Number of chunks generated for this URL.</summary>
    public int ChunksWritten { get; set; }
}

/// <summary>
/// FR-MCP-065, TR-MCP-INGEST-003: Aggregated response for website ingestion requests.
/// </summary>
public sealed class WebsiteIngestResult
{
    /// <summary>Unique operation identifier.</summary>
    public string RunId { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>UTC start timestamp.</summary>
    public DateTime StartedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>UTC completion timestamp.</summary>
    public DateTime CompletedAtUtc { get; set; } = DateTime.UtcNow;

    /// <summary>Top-level status: <c>completed</c> or <c>partial-failure</c>.</summary>
    public string Status { get; set; } = "completed";

    /// <summary>Total number of documents upserted.</summary>
    public int DocumentsIngested { get; set; }

    /// <summary>Total number of chunks written.</summary>
    public int ChunksWritten { get; set; }

    /// <summary>Per-URL outcomes.</summary>
    public IReadOnlyList<WebsiteIngestUrlResult> UrlResults { get; set; } = [];

    /// <summary>True when GraphRAG index was requested and completed.</summary>
    public bool GraphRagIndexed { get; set; }

    /// <summary>Error detail when GraphRAG index trigger fails.</summary>
    public string? GraphRagIndexError { get; set; }
}

/// <summary>
/// FR-MCP-065, TR-MCP-INGEST-003: Streaming progress event for website ingestion SSE clients.
/// </summary>
public sealed class WebsiteIngestProgressEvent
{
    /// <summary>Ingestion operation identifier.</summary>
    public string RunId { get; set; } = string.Empty;

    /// <summary>Event type (for example: <c>started</c>, <c>page</c>, <c>completed</c>, <c>error</c>).</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Current top-level ingest status.</summary>
    public string? Status { get; set; }

    /// <summary>Optional human-readable message.</summary>
    public string? Message { get; set; }

    /// <summary>Processed page count for this request.</summary>
    public int PagesProcessed { get; set; }

    /// <summary>Total documents upserted so far.</summary>
    public int DocumentsIngested { get; set; }

    /// <summary>Total chunks written so far.</summary>
    public int ChunksWritten { get; set; }

    /// <summary>Per-page outcome when available.</summary>
    public WebsiteIngestUrlResult? UrlResult { get; set; }

    /// <summary>Final aggregate result when event type is <c>completed</c>.</summary>
    public WebsiteIngestResult? Result { get; set; }
}
