using McpServer.Support.Mcp.Storage;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// TR-MCP-HEALTH-003 (BUG-TRIAGE-096): cheap storage-connectivity probe. Registered with tags
/// <c>ready</c> + <c>storage</c> and surfaced as the explicit <c>storage</c> field
/// (<c>reachable</c>|<c>unreachable</c>) on the health payload. It is intentionally NOT tagged
/// <c>live</c>: <c>/health</c> keeps liveness semantics and stays Healthy with an exact nonce
/// echo during a storage-only outage, because marker trust bootstrap depends on
/// <c>/health</c> 200 + byte-for-byte nonce echo and a storage outage must not flip agents to
/// <c>MCP_UNTRUSTED</c>.
/// </summary>
public sealed class StorageConnectivityHealthCheck : IHealthCheck
{
    /// <summary>TR-MCP-HEALTH-003: registered health-check name.</summary>
    public const string Name = "storage";

    /// <summary>TR-MCP-HEALTH-003: tag that surfaces this check as the payload storage field.</summary>
    public const string StorageTag = "storage";

    private static readonly TimeSpan s_probeTimeout = TimeSpan.FromSeconds(5);

    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<StorageConnectivityHealthCheck> _logger;

    /// <summary>TR-MCP-HEALTH-003: initializes the probe.</summary>
    /// <param name="scopeFactory">Scope factory used to resolve the scoped <see cref="McpDbContext"/>.</param>
    /// <param name="logger">Logger.</param>
    public StorageConnectivityHealthCheck(
        IServiceScopeFactory scopeFactory,
        ILogger<StorageConnectivityHealthCheck> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        try
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(s_probeTimeout);
            using var scope = _scopeFactory.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<McpDbContext>();
            var reachable = await db.Database.CanConnectAsync(timeout.Token).ConfigureAwait(false);
            return reachable
                ? HealthCheckResult.Healthy("Storage backend is reachable.")
                : HealthCheckResult.Unhealthy("Storage backend is unreachable.");
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning(
                "Storage connectivity probe timed out after {TimeoutSeconds}s.", s_probeTimeout.TotalSeconds);
            return HealthCheckResult.Unhealthy(
                $"Storage connectivity probe timed out after {s_probeTimeout.TotalSeconds:0}s.");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Storage connectivity probe failed.");
            return HealthCheckResult.Unhealthy("Storage backend is unreachable.", ex);
        }
    }
}
