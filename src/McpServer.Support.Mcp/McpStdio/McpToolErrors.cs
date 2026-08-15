using System.Text.Json;
using McpServer.Support.Mcp.Services;

namespace McpServer.Support.Mcp.McpStdio;

/// <summary>
/// TR-MCP-HEALTH-003 (BUG-TRIAGE-096): the single mapping point for MCP tool error payloads.
/// Connection-class storage failures serialize as the stable machine-readable payload
/// <c>{"error":"backend_unavailable","message":...,"retryable":true}</c> instead of echoing raw
/// provider text (raw SqlClient messages, the EnableRetryOnFailure hint); every other exception
/// keeps the existing untyped <c>{"error": message}</c> shape.
/// </summary>
internal static class McpToolErrors
{
    /// <summary>TR-MCP-HEALTH-003: stable error code for storage-connectivity failures.</summary>
    internal const string BackendUnavailableError = "backend_unavailable";

    /// <summary>TR-MCP-HEALTH-003: stable client-facing message for storage-connectivity failures.</summary>
    internal const string BackendUnavailableMessage =
        "The storage backend is currently unreachable. Retry the operation once connectivity is restored.";

    /// <summary>
    /// TR-MCP-HEALTH-003: serializes the tool error payload for the given exception, applying the
    /// backend-unavailable classification from <see cref="StorageBackendUnavailability"/>.
    /// </summary>
    /// <param name="exception">The caught tool exception.</param>
    /// <returns>The JSON error payload for the MCP tool response.</returns>
    internal static string Serialize(Exception exception)
        => StorageBackendUnavailability.IsBackendUnavailable(exception)
            ? JsonSerializer.Serialize(new
            {
                error = BackendUnavailableError,
                message = BackendUnavailableMessage,
                retryable = true,
            })
            : JsonSerializer.Serialize(new { error = exception.Message });
}
