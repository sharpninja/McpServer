using McpServer.UI.Core.Messages;

namespace McpServer.UI.Core.Services;

/// <summary>
/// Host-provided API client abstraction for the server health endpoint.
/// </summary>
public interface IHealthApiClient
{
    /// <summary>
    /// Executes a health check against the active server and returns a snapshot.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A health snapshot with parsed status and raw payload.</returns>
    Task<HealthSnapshot> CheckHealthAsync(CancellationToken cancellationToken = default);
}
