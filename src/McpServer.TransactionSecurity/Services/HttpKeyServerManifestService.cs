using System.Net.Http.Json;
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
            () => new TransactionManifestVerifyResponse
            {
                IsValid = false,
                Reason = TransactionFailureReason.KeyServerUnavailable,
            },
            cancellationToken).ConfigureAwait(false);

    private async Task<TResponse> PostAsync<TRequest, TResponse>(
        string path,
        TRequest request,
        Func<TResponse> unavailableResponse,
        CancellationToken cancellationToken)
    {
        try
        {
            using var response = await _http.PostAsJsonAsync(path, request, cancellationToken).ConfigureAwait(false);
            var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken).ConfigureAwait(false);
            return result ?? unavailableResponse();
        }
        catch (Exception ex) when ((ex is HttpRequestException || ex is TaskCanceledException) && !cancellationToken.IsCancellationRequested)
        {
            return unavailableResponse();
        }
    }
}
