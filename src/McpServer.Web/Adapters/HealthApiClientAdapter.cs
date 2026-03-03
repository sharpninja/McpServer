using System.Text.Json;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Web.Adapters;

internal sealed class HealthApiClientAdapter : IHealthApiClient
{
    private readonly WebMcpContext _context;

    public HealthApiClientAdapter(WebMcpContext context)
    {
        _context = context;
    }

    public async Task<HealthSnapshot> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        var client = await _context.GetRequiredControlApiClientAsync(cancellationToken).ConfigureAwait(false);
        var health = await client.Health.GetAsync(cancellationToken).ConfigureAwait(false);
        var raw = JsonSerializer.Serialize(health);
        var status = string.IsNullOrWhiteSpace(health.Status) ? "unknown" : health.Status;

        return new HealthSnapshot(
            CheckedAt: DateTimeOffset.UtcNow,
            Status: status,
            RawPayload: raw,
            ServerBaseUrl: _context.BaseUrl.ToString());
    }
}
