using System.Text.Json;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.McpStdio;

/// <summary>
/// FR-MCP-TRIAGEERR-001 / TR-MCP-HEALTH-003: the single mapping point for MCP tool error payloads.
/// Every failure emits <c>code</c>, <c>message</c>, <c>retryable</c>, and optional <c>details</c>.
/// <c>error</c> is kept as an alias of <c>code</c> for existing backend_unavailable clients.
/// </summary>
internal static class McpToolErrors
{
    /// <summary>TR-MCP-HEALTH-003: stable error code for storage-connectivity failures.</summary>
    internal const string BackendUnavailableError = McpErrorClassifier.BackendUnavailable;

    /// <summary>TR-MCP-HEALTH-003: stable client-facing message for storage-connectivity failures.</summary>
    internal const string BackendUnavailableMessage = McpErrorClassifier.BackendUnavailableMessage;

    /// <summary>
    /// FR-MCP-TRIAGEERR-001: serializes the classified tool error payload.
    /// </summary>
    /// <param name="exception">The caught tool exception.</param>
    /// <returns>The JSON error payload for the MCP tool response.</returns>
    internal static string Serialize(Exception exception)
    {
        var classified = McpErrorClassifier.Classify(exception);
        return JsonSerializer.Serialize(new
        {
            code = classified.Code,
            error = classified.Code,
            message = classified.Message,
            retryable = classified.Retryable,
            details = classified.Details,
        });
    }
}
