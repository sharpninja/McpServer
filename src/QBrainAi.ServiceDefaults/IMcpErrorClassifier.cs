namespace Microsoft.Extensions.Hosting;

/// <summary>
/// FR-MCP-TRIAGEERR-001 / TR-MCP-TRIAGEERR-001: classifies exceptions into the shared
/// machine-readable error envelope consumed by REST, MCP tools, REPL, and plugins.
/// </summary>
public interface IMcpErrorClassifier
{
    /// <summary>Classifies <paramref name="exception"/> into a stable envelope.</summary>
    /// <param name="exception">The thrown exception. Must not be null.</param>
    /// <returns>The classified envelope.</returns>
    McpErrorClassification Classify(Exception exception);
}

/// <summary>
/// FR-MCP-TRIAGEERR-001: stable error classification used by HTTP, MCP tools, and REPL.
/// </summary>
/// <param name="Code">Snake_case machine code (for example <c>persistence_error</c>).</param>
/// <param name="Message">Human-readable summary without raw SqlClient retry ads.</param>
/// <param name="Retryable">Whether the caller should retry.</param>
/// <param name="Details">Optional structured details; EF inner text lives under <c>inner</c>.</param>
/// <param name="StatusCode">Suggested HTTP status code.</param>
public sealed record McpErrorClassification(
    string Code,
    string Message,
    bool Retryable,
    IReadOnlyDictionary<string, object?>? Details,
    int StatusCode);
