// TR-PLANNED-013: Wraps System.Net.Http.HttpClient to implement Serilog.Sinks.Http.IHttpClient for Parseable ingest (custom headers).

using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Configuration;
using Serilog;

namespace FWH.Support.Mcp.Logging;

/// <summary>
/// TR-PLANNED-013: IHttpClient implementation that POSTs to Parseable with X-P-Stream and Basic auth.
/// </summary>
public sealed class ParseableHttpClient : Serilog.Sinks.Http.IHttpClient, IDisposable
{
    /// <summary>Property set on meta-logs (success/failure of push) so they can be excluded from the Parseable sink and not republished.</summary>
    public const string ParseableMetaPropertyName = "ParseableMeta";

    private readonly HttpClient _httpClient;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    /// <param name="streamName">Parseable stream name (X-P-Stream header).</param>
    /// <param name="username">Basic auth username.</param>
    /// <param name="password">Basic auth password.</param>
    public ParseableHttpClient(string streamName, string username, string password)
    {
        _httpClient = new HttpClient();
        _httpClient.DefaultRequestHeaders.Add("X-P-Stream", streamName);
        var cred = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{username}:{password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", cred);
    }

    /// <inheritdoc />
    public void Configure(IConfiguration configuration)
    {
        // No configuration needed; headers set in constructor.
    }

    /// <inheritdoc />
    public async Task<HttpResponseMessage> PostAsync(string requestUri, Stream contentStream, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(contentStream);
        try
        {
            using var buffer = new MemoryStream();
            await contentStream.CopyToAsync(buffer, cancellationToken).ConfigureAwait(false);
            buffer.Position = 0;
            var requestBody = Encoding.UTF8.GetString(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));

            using var content = new StreamContent(buffer);
            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
            var response = await _httpClient.PostAsync(new Uri(requestUri), content, cancellationToken).ConfigureAwait(false);
            if (response.IsSuccessStatusCode)
            {
                Log.ForContext(ParseableMetaPropertyName, true).Information("[Parseable] Log batch pushed successfully to {RequestUri} ({StatusCode})", requestUri, (int)response.StatusCode);
            }
            else
            {
                var responseBody = await response.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                var tempPath = Path.Combine(Path.GetTempPath(), $"parseable-request-{DateTime.UtcNow:yyyyMMdd-HHmmss}.json");
                await File.WriteAllTextAsync(tempPath, requestBody, cancellationToken).ConfigureAwait(false);
                Log.ForContext(ParseableMetaPropertyName, true).Warning("[Parseable] FAILED to push log batch to {RequestUri}: {StatusCode} {ReasonPhrase}. Response: {ResponseBody}. Full request body written to: {RequestBodyPath}", requestUri, (int)response.StatusCode, response.ReasonPhrase, responseBody, tempPath);
            }
            return response;
        }
        catch (Exception ex)
        {
            Log.ForContext(ParseableMetaPropertyName, true).Warning(ex, "[Parseable] Exception while pushing log batch to {RequestUri}: {Message}", requestUri, ex.Message);
            throw;
        }
    }

    /// <inheritdoc />
    public void Dispose() => _httpClient.Dispose();
}
