namespace Microsoft.Extensions.Hosting;

/// <summary>
/// TR-MCP-HEALTH-003: classifies exceptions that indicate the storage backend is unreachable
/// (connection-class provider failures, transient-connection exhaustion) so the shared error
/// path can map them to HTTP 503 with the stable machine-readable body
/// <c>{"error":"backend_unavailable", ...}</c> instead of a generic 500 that echoes raw
/// provider text. Register an implementation in DI; the global exception handler resolves it
/// per request and falls back to the generic 500 mapping when none is registered.
/// </summary>
public interface IBackendUnavailabilityDetector
{
    /// <summary>
    /// TR-MCP-HEALTH-003: returns <see langword="true"/> when the exception (or any inner
    /// exception) represents a backend-unavailable condition that clients should treat as
    /// retryable infrastructure failure rather than a request error.
    /// </summary>
    /// <param name="exception">The unhandled exception to classify.</param>
    /// <returns><see langword="true"/> when the failure is backend-unavailability.</returns>
    bool IsBackendUnavailable(Exception exception);
}
