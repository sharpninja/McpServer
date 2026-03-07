namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// TR-PLANNED-013: Configuration for MCP ingestion (repo root, paths, allowlists).
/// FR-SUPPORT-010: Repo and session log paths.
/// </summary>
public sealed class IngestionOptions
{
    /// <summary>TR-PLANNED-013: Repository root (e.g. workspace path).</summary>
    public string RepoRoot { get; set; } = ".";

    /// <summary>TR-PLANNED-013: Path to TODO data file when using YAML storage.</summary>
    public string TodoFilePath { get; set; } = "docs/Project/TODO.yaml";

    /// <summary>FR-SUPPORT-010: Path to session logs directory (e.g. docs/sessions).</summary>
    public string SessionsPath { get; set; } = "docs/sessions";

    /// <summary>TR-PLANNED-013: Path to UnifiedModel schema (for reference).</summary>
    public string UnifiedModelSchemaPath { get; set; } = "docs/schemas/UnifiedModel.schema.json";

    /// <summary>FR-SUPPORT-010: Path to external docs cache (e.g. docs/external).</summary>
    public string ExternalDocsPath { get; set; } = "docs/external";

    /// <summary>TR-PLANNED-013: Allowed repo path patterns (e.g. "*.md", "*.cs", "docs/**"). Null = all under RepoRoot.</summary>
    public IReadOnlyList<string>? RepoAllowlist { get; set; }

    /// <summary>TR-PLANNED-013: Maximum file size in bytes to ingest (default 1 MB).</summary>
    public long MaxFileSizeBytes { get; set; } = 1024 * 1024;

    /// <summary>TR-MCP-INGEST-003: Maximum number of web pages to ingest per request.</summary>
    public int MaxWebsitePages { get; set; } = 20;

    /// <summary>TR-MCP-INGEST-003: Maximum crawl depth for includeSubpages requests.</summary>
    public int MaxWebsiteDepth { get; set; } = 2;

    /// <summary>TR-MCP-INGEST-003: Maximum bytes downloaded per web page.</summary>
    public int MaxWebsiteBytesPerPage { get; set; } = 262144;

    /// <summary>TR-MCP-INGEST-003: Maximum redirects allowed per page fetch.</summary>
    public int MaxWebsiteRedirects { get; set; } = 3;

    /// <summary>TR-MCP-INGEST-003: HTTP timeout in seconds for website fetch requests.</summary>
    public int WebsiteRequestTimeoutSeconds { get; set; } = 120;

    /// <summary>TR-MCP-INGEST-003: Maximum HTTP fetch attempts per page request.</summary>
    public int WebsiteRequestMaxAttempts { get; set; } = 3;

    /// <summary>TR-MCP-INGEST-003: Base retry delay in milliseconds between HTTP fetch attempts.</summary>
    public int WebsiteRequestRetryDelayMilliseconds { get; set; } = 1000;

    /// <summary>TR-MCP-INGEST-003: Allowed URL schemes for website ingestion.</summary>
    public IReadOnlyList<string> WebsiteAllowedSchemes { get; set; } = ["http", "https"];

    /// <summary>TR-MCP-INGEST-003: Blocked hostnames for SSRF protection.</summary>
    public IReadOnlyList<string> WebsiteBlockedHosts { get; set; } = ["localhost"];
}
