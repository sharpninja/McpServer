using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-077: Health check that probes the configured federation upstream target.
/// When federation is disabled or no default target is configured the check is skipped
/// (reports <see cref="HealthStatus.Healthy"/> with a descriptive message so it does not
/// pollute the health report when not in use).
/// </summary>
public sealed class FederationUpstreamHealthCheck : IHealthCheck
{
    private readonly FederationRegistry _registry;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FederationUpstreamHealthCheck> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationUpstreamHealthCheck"/> class.
    /// </summary>
    /// <param name="registry">Federation target registry.</param>
    /// <param name="httpClientFactory">Factory used to create the probe HTTP client.</param>
    /// <param name="logger">Logger.</param>
    public FederationUpstreamHealthCheck(
        FederationRegistry registry,
        IHttpClientFactory httpClientFactory,
        ILogger<FederationUpstreamHealthCheck> logger)
    {
        _registry = registry;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        if (!_registry.IsEnabled)
            return HealthCheckResult.Healthy("Federation disabled.");

        // Use null workspace path to resolve the default target
        var target = _registry.ResolveTarget(null);
        if (target is null)
            return HealthCheckResult.Healthy("Federation enabled — no upstream target configured.");

        var healthUrl = $"{target.BaseUrl}/health";
        try
        {
            using var client = _httpClientFactory.CreateClient(FederationProxyService.HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, healthUrl);
            if (target.ApiKey is not null)
                request.Headers.TryAddWithoutValidation("X-Api-Key", target.ApiKey);

            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            var body = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);

            var data = new Dictionary<string, object>
            {
                ["target"] = target.Name,
                ["url"] = healthUrl,
                ["statusCode"] = (int)response.StatusCode,
            };

            if (response.IsSuccessStatusCode)
            {
                _logger.LogDebug("Federation upstream health OK: {Target} ({StatusCode})", target.Name, (int)response.StatusCode);
                return HealthCheckResult.Healthy($"Upstream '{target.Name}' is healthy.", data);
            }

            _logger.LogWarning("Federation upstream health degraded: {Target} returned {StatusCode}", target.Name, (int)response.StatusCode);
            return HealthCheckResult.Degraded(
                $"Upstream '{target.Name}' returned HTTP {(int)response.StatusCode}.",
                data: data);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException)
        {
            _logger.LogWarning(ex, "Federation upstream health check failed: {Target} at {Url}", target.Name, healthUrl);
            return HealthCheckResult.Unhealthy(
                $"Upstream '{target.Name}' unreachable: {ex.Message}",
                exception: ex,
                data: new Dictionary<string, object>
                {
                    ["target"] = target.Name,
                    ["url"] = healthUrl,
                });
        }
    }
}
