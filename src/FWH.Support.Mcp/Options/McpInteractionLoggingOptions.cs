namespace FWH.Support.Mcp.Options;

/// <summary>
/// TR-PLANNED-013: Options for MCP interaction structured logging and optional async submission to a logging service.
/// </summary>
public sealed class McpInteractionLoggingOptions
{
    /// <summary>Configuration section name.</summary>
    public const string SectionName = "Mcp:InteractionLogging";

    /// <summary>
    /// Base URL of the logging service to receive structured log entries (POST JSON).
    /// When null or empty, only local structured logging is performed; no HTTP submission.
    /// </summary>
    public string? LoggingServiceUrl { get; set; }

    /// <summary>Maximum number of log entries to hold in the submission queue. Default 1000.</summary>
    public int QueueCapacity { get; set; } = 1000;

    /// <summary>Include query string in log entries when true. Default false to avoid logging sensitive or large query data.</summary>
    public bool IncludeQueryString { get; set; }

    /// <summary>Capture and include request body (input) in log entries. Default true.</summary>
    public bool IncludeRequestBody { get; set; } = true;

    /// <summary>Capture and include response body (output) in log entries. Default true.</summary>
    public bool IncludeResponseBody { get; set; } = true;

    /// <summary>Maximum body size (in characters) to capture. Bodies larger than this are truncated. Default 32 768.</summary>
    public int MaxBodyCaptureSize { get; set; } = 32_768;
}
