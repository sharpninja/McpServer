namespace McpServer.Support.Mcp.Models;

/// <summary>
/// TR-PLANNED-013: Structured log entry for a single MCP interaction (request/response).
/// Used for local structured logging and optional async submission to a logging service.
/// </summary>
public sealed class InteractionLogEntry
{
    /// <summary>UTC timestamp when the request was completed.</summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>HTTP method (e.g. GET, POST).</summary>
    public string Method { get; set; } = string.Empty;

    /// <summary>Request path (e.g. /mcpserver/context/search).</summary>
    public string Path { get; set; } = string.Empty;

    /// <summary>Optional query string (may be omitted if sensitive or large).</summary>
    public string? QueryString { get; set; }

    /// <summary>HTTP response status code.</summary>
    public int StatusCode { get; set; }

    /// <summary>Duration of the request in milliseconds.</summary>
    public double DurationMs { get; set; }

    /// <summary>Request correlation id (e.g. HttpContext.TraceIdentifier).</summary>
    public string? RequestId { get; set; }

    /// <summary>Captured request body (input). Null when body capture is disabled or body was empty.</summary>
    public string? RequestBody { get; set; }

    /// <summary>Captured response body (output). Null when body capture is disabled or body was empty.</summary>
    public string? ResponseBody { get; set; }
}
