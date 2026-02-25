using System.Text.Json;
using McpServer.UI.Core.Messages;
using McpServer.UI.Core.Services;

namespace McpServer.Director;

/// <summary>
/// Director adapter for <see cref="ISessionLogApiClient"/> backed by <see cref="McpHttpClient"/>.
/// </summary>
internal sealed class SessionLogApiClientAdapter : ISessionLogApiClient
{
    private readonly McpHttpClient? _client;

    /// <summary>Initializes a new adapter instance.</summary>
    /// <param name="client">Director HTTP client, or null if no marker file is available.</param>
    public SessionLogApiClientAdapter(McpHttpClient? client)
    {
        _client = client;
    }

    /// <inheritdoc />
    public async Task<ListSessionLogsResult> ListSessionLogsAsync(ListSessionLogsQuery query, CancellationToken cancellationToken = default)
    {
        if (_client is null)
            throw new InvalidOperationException("Session-log API client is unavailable. No workspace marker file was found.");

        var path = BuildPath(query);
        var result = await _client.GetAsync<JsonElement>(path, cancellationToken).ConfigureAwait(false);

        if (!result.TryGetProperty("items", out var itemsElement) || itemsElement.ValueKind != JsonValueKind.Array)
            throw new InvalidOperationException("Session-log query response did not contain an items array.");

        var items = new List<SessionLogSummary>();
        foreach (var item in itemsElement.EnumerateArray())
        {
            items.Add(new SessionLogSummary(
                SessionId: GetString(item, "sessionId"),
                SourceType: GetString(item, "sourceType"),
                Title: GetString(item, "title"),
                Status: GetString(item, "status"),
                Model: GetOptionalString(item, "model"),
                Started: GetOptionalString(item, "started"),
                LastUpdated: GetOptionalString(item, "lastUpdated"),
                EntryCount: GetInt(item, "entryCount")));
        }

        var totalCount = GetInt(result, "totalCount", items.Count);
        var limit = GetInt(result, "limit", query.Limit <= 0 ? 20 : query.Limit);
        var offset = GetInt(result, "offset", Math.Max(0, query.Offset));

        return new ListSessionLogsResult(items, totalCount, limit, offset);
    }

    private static string BuildPath(ListSessionLogsQuery query)
    {
        var parts = new List<string>();

        if (!string.IsNullOrWhiteSpace(query.Agent))
            parts.Add($"agent={Uri.EscapeDataString(query.Agent)}");
        if (!string.IsNullOrWhiteSpace(query.Model))
            parts.Add($"model={Uri.EscapeDataString(query.Model)}");
        if (!string.IsNullOrWhiteSpace(query.Text))
            parts.Add($"text={Uri.EscapeDataString(query.Text)}");

        parts.Add($"limit={Math.Max(1, query.Limit)}");
        parts.Add($"offset={Math.Max(0, query.Offset)}");

        return $"/mcp/sessionlog?{string.Join("&", parts)}";
    }

    private static string GetString(JsonElement element, string property)
        => element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString() ?? string.Empty
            : string.Empty;

    private static string? GetOptionalString(JsonElement element, string property)
        => element.TryGetProperty(property, out var prop) && prop.ValueKind == JsonValueKind.String
            ? prop.GetString()
            : null;

    private static int GetInt(JsonElement element, string property, int defaultValue = 0)
        => element.TryGetProperty(property, out var prop) && prop.TryGetInt32(out var value)
            ? value
            : defaultValue;
}
