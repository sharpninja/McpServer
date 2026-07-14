using System.Globalization;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using McpServer.TransactionSecurity;
using McpServer.TransactionSecurity.Options;

namespace McpServer.TransactionSecurity.Services;

/// <summary>
/// FR-MCP-SUBLOG-001: One received-message entry logged by the subscriber to a high-performance store.
/// </summary>
/// <param name="EventName">Outcome event name (for example <c>subscriber.transaction.committed</c>).</param>
/// <param name="TransactionId">Transaction identifier, when present.</param>
/// <param name="Reason">Structured failure reason name (<c>None</c> for success).</param>
/// <param name="Details">Optional detail (for example diffgram id or status).</param>
/// <param name="TimestampUtc">UTC time the message outcome was recorded.</param>
public sealed record SubscriberMessageLogEntry(
    string EventName,
    string? TransactionId,
    string Reason,
    string? Details,
    DateTimeOffset TimestampUtc);

/// <summary>
/// Flat Parseable ingestion row for subscriber message log batches.
/// </summary>
internal sealed class SubscriberMessageLogPayload
{
    /// <summary>UTC timestamp in Parseable-friendly text form.</summary>
    public string Timestamp { get; set; } = string.Empty;

    /// <summary>Outcome event name.</summary>
    public string Event { get; set; } = string.Empty;

    /// <summary>Transaction identifier, when present.</summary>
    public string? TransactionId { get; set; }

    /// <summary>Structured result reason.</summary>
    public string Reason { get; set; } = string.Empty;

    /// <summary>Optional event detail.</summary>
    public string? Details { get; set; }
}

/// <summary>
/// FR-MCP-SUBLOG-001: High-performance sink for received transaction messages. Implementations MUST be
/// best-effort and MUST NOT throw or block the subscriber commit path on sink errors.
/// </summary>
public interface ISubscriberMessageLog
{
    /// <summary>Logs a received-message outcome. Errors are swallowed by the implementation.</summary>
    /// <param name="entry">The message outcome entry.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A task that completes when the entry has been handed to the sink.</returns>
    Task LogAsync(SubscriberMessageLogEntry entry, CancellationToken cancellationToken = default);
}

/// <summary>FR-MCP-SUBLOG-001: Default no-op sink used when high-performance message logging is disabled.</summary>
public sealed class NoopSubscriberMessageLog : ISubscriberMessageLog
{
    /// <inheritdoc />
    public Task LogAsync(SubscriberMessageLogEntry entry, CancellationToken cancellationToken = default)
        => Task.CompletedTask;
}

/// <summary>
/// FR-MCP-SUBLOG-001: Parseable HTTP sink. POSTs a single-entry flat JSON batch to <c>{Url}/api/v1/ingest</c>
/// with the <c>X-P-Stream</c> header and basic auth. All transport errors are swallowed so logging never
/// breaks the transaction.
/// </summary>
public sealed class ParseableSubscriberMessageLog : ISubscriberMessageLog
{
    private readonly HttpClient _httpClient;
    private readonly Uri _ingestUri;
    private readonly string _streamName;
    private readonly string _authValue;

    /// <summary>Initializes a new instance of the <see cref="ParseableSubscriberMessageLog"/> class.</summary>
    /// <param name="httpClient">Transport client.</param>
    /// <param name="options">Parseable sink options (URL, stream, credentials).</param>
    public ParseableSubscriberMessageLog(HttpClient httpClient, SubscriberParseableOptions options)
    {
        ArgumentNullException.ThrowIfNull(httpClient);
        ArgumentNullException.ThrowIfNull(options);
        if (string.IsNullOrWhiteSpace(options.Url))
            throw new ArgumentException("Parseable URL is required when the subscriber message log is enabled.", nameof(options));

        _httpClient = httpClient;
        _ingestUri = new Uri($"{options.Url!.TrimEnd('/')}/api/v1/ingest");
        _streamName = options.StreamName;
        _authValue = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{options.Username}:{options.Password}"));
    }

    /// <inheritdoc />
    public async Task LogAsync(SubscriberMessageLogEntry entry, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(entry);
        try
        {
            var payload = new[]
            {
                new SubscriberMessageLogPayload
                {
                    Timestamp = entry.TimestampUtc.ToUniversalTime().ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture),
                    Event = entry.EventName,
                    TransactionId = entry.TransactionId,
                    Reason = entry.Reason,
                    Details = entry.Details,
                },
            };
            var payloadJson = JsonSerializer.Serialize(payload, typeof(SubscriberMessageLogPayload[]), TransactionSecurityJsonContext.Default);

            using var request = new HttpRequestMessage(HttpMethod.Post, _ingestUri)
            {
                Content = new StringContent(payloadJson, Encoding.UTF8, "application/json"),
            };
            request.Headers.TryAddWithoutValidation("X-P-Stream", _streamName);
            request.Headers.Authorization = new AuthenticationHeaderValue("Basic", _authValue);

            using var response = await _httpClient.SendAsync(request, cancellationToken).ConfigureAwait(false);
            _ = response.IsSuccessStatusCode; // best-effort; non-success is intentionally ignored
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or OperationCanceledException or InvalidOperationException)
        {
            // Best-effort sink: never break the transaction on a logging failure.
        }
    }
}
