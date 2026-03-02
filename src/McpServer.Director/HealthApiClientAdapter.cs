using System.Text.Json;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="IHealthApiClient"/> backed by <see cref="McpHttpClient"/>.
/// </summary>
internal sealed class HealthApiClientAdapter : IHealthApiClient
{
    private readonly McpHttpClient? _client;

    /// <summary>Initializes a new adapter instance.</summary>
    /// <param name="client">Director HTTP client, or null if no marker file is available.</param>
    public HealthApiClientAdapter(McpHttpClient? client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<HealthSnapshot> CheckHealthAsync(CancellationToken cancellationToken = default)
    {
        if (_client is null)
            throw new InvalidOperationException("Health API client is unavailable. No workspace marker file was found.");

        var raw = await _client.GetStringAsync("/health", cancellationToken).ConfigureAwait(false);
        var status = TryParseStatus(raw) ?? "unknown";

        return new HealthSnapshot(
            CheckedAt: DateTimeOffset.UtcNow,
            Status: status,
            RawPayload: raw,
            ServerBaseUrl: _client.BaseUrl);
    }

    private static string? TryParseStatus(string raw)
    {
        try
        {
            using var doc = JsonDocument.Parse(raw);
            return doc.RootElement.TryGetProperty("status", out var statusProp)
                ? statusProp.GetString()
                : null;
        }
        catch
        {
            return null;
        }
    }
}
