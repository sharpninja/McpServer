using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-HEALTH-003 / FR-MCP-TRIAGESTORE-002: runs host-startup storage work without
/// killing the process when the backend is unreachable. Liveness (<c>/health</c>) must
/// still come up after an SSL pre-login handshake timeout or similar connection-class
/// failure.
/// </summary>
public static class StartupStorageBootstrap
{
    /// <summary>
    /// Invokes <paramref name="initialize"/>. Returns <see langword="true"/> on success.
    /// Classified backend-unavailable failures are logged and return <see langword="false"/>.
    /// Other exceptions propagate.
    /// </summary>
    /// <param name="initialize">Startup storage work (migrate, probe, backfill).</param>
    /// <param name="logger">Logger for swallowed backend-unavailable failures.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns><see langword="true"/> when initialize completed.</returns>
    public static async Task<bool> TryInitializeAsync(
        Func<CancellationToken, Task> initialize,
        ILogger logger,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(initialize);
        ArgumentNullException.ThrowIfNull(logger);

        try
        {
            await initialize(cancellationToken).ConfigureAwait(false);
            return true;
        }
        catch (Exception ex) when (StorageBackendUnavailability.IsBackendUnavailable(ex))
        {
            logger.LogError(
                ex,
                "Startup storage initialization skipped because the backend is unreachable. Process remains up for /health.");
            return false;
        }
    }
}
