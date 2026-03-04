namespace McpServer.Support.Mcp.Options;

#pragma warning disable CS1591

/// <summary>
/// Configuration for GraphRAG integration.
/// </summary>
public sealed class GraphRagOptions
{
    public const string SectionName = "Mcp:GraphRag";

    /// <summary>Enable GraphRAG endpoints and service behavior.</summary>
    public bool Enabled { get; set; }

    /// <summary>
    /// When true, context search attempts GraphRAG before falling back to the existing search service.
    /// </summary>
    public bool EnhanceContextSearch { get; set; } = true;

    /// <summary>
    /// GraphRAG root path. Relative paths are resolved against the workspace root.
    /// </summary>
    public string RootPath { get; set; } = "mcp-data/graphrag";

    /// <summary>Default query mode (for example: local, global, drift).</summary>
    public string DefaultQueryMode { get; set; } = "local";

    /// <summary>Maximum chunks to include by default in GraphRAG query responses.</summary>
    public int DefaultMaxChunks { get; set; } = 20;

    /// <summary>Timeout for index operations in seconds.</summary>
    public int IndexTimeoutSeconds { get; set; } = 600;

    /// <summary>Timeout for query operations in seconds.</summary>
    public int QueryTimeoutSeconds { get; set; } = 120;

    /// <summary>
    /// Optional external backend command for GraphRAG execution.
    /// When null/empty, internal fallback mode is used.
    /// </summary>
    public string? BackendCommand { get; set; }

    /// <summary>Optional arguments template for the external backend command.</summary>
    public string? BackendArgs { get; set; }

    /// <summary>
    /// Maximum concurrent index jobs per workspace. Current implementation supports one active job.
    /// </summary>
    public int MaxConcurrentIndexJobsPerWorkspace { get; set; } = 1;

    /// <summary>
    /// Optional artifact version label written into workspace GraphRAG status for compatibility checks.
    /// </summary>
    public string ArtifactVersion { get; set; } = "v1";
}

#pragma warning restore CS1591
