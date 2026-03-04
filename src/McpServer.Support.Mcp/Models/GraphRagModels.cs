namespace McpServer.Support.Mcp.Models;

#pragma warning disable CS1591

/// <summary>Query request for GraphRAG retrieval.</summary>
public sealed class GraphRagQueryRequest
{
    public string? Query { get; set; }
    public string? Mode { get; set; }
    public int? MaxChunks { get; set; }
    public bool IncludeContextChunks { get; set; } = true;
}

/// <summary>Index request for GraphRAG.</summary>
public sealed class GraphRagIndexRequest
{
    public bool Force { get; set; }
}

/// <summary>Status payload for GraphRAG readiness and workspace state.</summary>
public sealed class GraphRagStatusResponse
{
    public bool Enabled { get; set; }
    public string WorkspacePath { get; set; } = string.Empty;
    public string GraphRoot { get; set; } = string.Empty;
    public bool IsInitialized { get; set; }
    public bool IsIndexed { get; set; }
    public DateTimeOffset? LastIndexedAtUtc { get; set; }
    public string? LastError { get; set; }
    public string Backend { get; set; } = "internal-fallback";
}

/// <summary>Citation payload from GraphRAG query responses.</summary>
public sealed class GraphRagCitation
{
    public string SourceKey { get; set; } = string.Empty;
    public string? ChunkId { get; set; }
    public string? Snippet { get; set; }
}

/// <summary>Query response for GraphRAG operations.</summary>
public sealed class GraphRagQueryResponse
{
    public string Query { get; set; } = string.Empty;
    public string Mode { get; set; } = "local";
    public string Answer { get; set; } = string.Empty;
    public IReadOnlyList<GraphRagCitation> Citations { get; set; } = [];
    public IReadOnlyList<ContextChunk> Chunks { get; set; } = [];
    public IReadOnlyList<string> SourceKeys { get; set; } = [];
    public bool FallbackUsed { get; set; }
    public string Backend { get; set; } = "internal-fallback";
}

#pragma warning restore CS1591
