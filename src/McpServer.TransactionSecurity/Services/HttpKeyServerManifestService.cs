using System.Net.Http.Json;
using System.Text.Json.Serialization.Metadata;
using McpServer.TransactionSecurity;
using McpServer.TransactionSecurity.Models;

namespace McpServer.TransactionSecurity.Services;

/// <summary>
/// TR-MCP-SUBSCRIBER-001: HTTP-backed keyserver manifest client used by the separate subscriber host.
/// </summary>
public sealed class HttpKeyServerManifestService : IKeyServerManifestService
{
    private readonly HttpClient _http;

    /// <summary>Initializes a new instance of the <see cref="HttpKeyServerManifestService"/> class.</summary>
    /// <param name="http">HTTP client targeting the separate keyserver host.</param>
    public HttpKeyServerManifestService(HttpClient http)
    {
        _http = http ?? throw new ArgumentNullException(nameof(http));
    }

    /// <inheritdoc />
    public async Task<TransactionManifestSignResponse> SignManifestAsync(
        TransactionManifestSignRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync(
            "mcpserver/keyserver/manifests/sign",
            request,
            TransactionSecurityJsonContext.Default.TransactionManifestSignRequest,
            TransactionSecurityJsonContext.Default.TransactionManifestSignResponse,
            () => new TransactionManifestSignResponse
            {
                Success = false,
                Reason = TransactionFailureReason.KeyServerUnavailable,
            },
            cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<TransactionManifestVerifyResponse> VerifyManifestAsync(
        TransactionManifestVerifyRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync(
            "mcpserver/keyserver/manifests/verify",
            request,
            TransactionSecurityJsonContext.Default.TransactionManifestVerifyRequest,
            TransactionSecurityJsonContext.Default.TransactionManifestVerifyResponse,
            () => new TransactionManifestVerifyResponse
            {
                IsValid = false,
                Reason = TransactionFailureReason.KeyServerUnavailable,
            },
            cancellationToken).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<TransactionManifestTraceRecord?> GetManifestAsync(
        string transactionId,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(transactionId))
            return null;

        try
        {
            return await _http
                .GetFromJsonAsync(
                    $"mcpserver/keyserver/manifests/{Uri.EscapeDataString(transactionId.Trim())}",
                    TransactionSecurityJsonContext.Default.TransactionManifestTraceRecord,
                    cancellationToken)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when ((ex is HttpRequestException || ex is TaskCanceledException) && !cancellationToken.IsCancellationRequested)
        {
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<TransactionManifestTraceReport> GetManifestReportAsync(
        TransactionManifestTraceReportRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var limit = Math.Clamp(request.Limit ?? 100, 1, 500);
        try
        {
            return await _http
                .GetFromJsonAsync(
                    $"mcpserver/keyserver/manifests/report{BuildReportQuery(request, limit)}",
                    TransactionSecurityJsonContext.Default.TransactionManifestTraceReport,
                    cancellationToken)
                .ConfigureAwait(false)
                ?? EmptyReport(request, limit);
        }
        catch (Exception ex) when ((ex is HttpRequestException || ex is TaskCanceledException) && !cancellationToken.IsCancellationRequested)
        {
            return EmptyReport(request, limit);
        }
    }

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        JsonTypeInfo<TRequest> requestTypeInfo,
        JsonTypeInfo<TResponse> responseTypeInfo,
        Func<TResponse> unavailableResponse,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(path, request, requestTypeInfo, cancellationToken).ConfigureAwait(false);
            var result = await response.Content.ReadFromJsonAsync(responseTypeInfo, cancellationToken).ConfigureAwait(false);
            return result ?? unavailableResponse();
        }
        catch (Exception ex) when ((ex is HttpRequestException || ex is TaskCanceledException) && !cancellationToken.IsCancellationRequested)
        {
            return unavailableResponse();
        }
    }

    private static string BuildReportQuery(TransactionManifestTraceReportRequest request, int limit)
    {
        var query = new List<string>();
        Add(query, "publisherPartyId", request.PublisherPartyId);
        Add(query, "subscriberPartyId", request.SubscriberPartyId);
        Add(query, "status", request.Status);
        Add(query, "fromUtc", request.FromUtc?.ToString("O"));
        Add(query, "toUtc", request.ToUtc?.ToString("O"));
        query.Add($"limit={limit}");
        return query.Count == 0 ? string.Empty : "?" + string.Join("&", query);
    }

    private static void Add(List<string> query, string name, string? value)
    {
        if (!string.IsNullOrWhiteSpace(value))
            query.Add($"{name}={Uri.EscapeDataString(value.Trim())}");
    }

    private static TransactionManifestTraceReport EmptyReport(
        TransactionManifestTraceReportRequest request,
        int limit)
        => new()
        {
            GeneratedAtUtc = DateTimeOffset.UtcNow,
            PublisherPartyId = string.IsNullOrWhiteSpace(request.PublisherPartyId) ? null : request.PublisherPartyId.Trim(),
            SubscriberPartyId = string.IsNullOrWhiteSpace(request.SubscriberPartyId) ? null : request.SubscriberPartyId.Trim(),
            Status = string.IsNullOrWhiteSpace(request.Status) ? null : request.Status.Trim(),
            FromUtc = request.FromUtc,
            ToUtc = request.ToUtc,
            Limit = limit,
            Records = [],
        };
}
