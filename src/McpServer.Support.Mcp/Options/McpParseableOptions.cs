namespace McpServer.Support.Mcp.Options;

/// <summary>
/// TR-PLANNED-013: Options for sending Serilog logs to Parseable (local Docker or remote).
/// When using Docker (scripts/Setup-Parseable.ps1), Parseable listens on port 8000 for both UI and ingestion; set Url to http://localhost:8000.
/// </summary>
public sealed class McpParseableOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:Parseable";

    /// <summary>
    /// Parseable ingestion base URL (e.g. http://localhost:8000). When null or empty, Serilog does not send to Parseable.
    /// Ingestion endpoint used: {Url}/api/v1/ingest
    /// </summary>
    public string? Url { get; set; }

    /// <summary>Stream name (X-P-Stream header). Default: mcp.</summary>
    public string StreamName { get; set; } = "mcp";

    /// <summary>Basic auth username. Default: admin.</summary>
    public string Username { get; set; } = "admin";

    /// <summary>Basic auth password. Default: admin.</summary>
    public string Password { get; set; } = "admin";

    /// <summary>
    /// Optional file path for fallback logging when publishing to Parseable fails (e.g. Parseable down or unreachable).
    /// When Url is set and this is set, Serilog also writes to this file (rolling daily). Example: logs/mcp-.log
    /// </summary>
    public string? FallbackLogPath { get; set; }
}
