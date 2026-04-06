using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-077: Stateless service that forwards an <see cref="HttpContext"/> request
/// to a resolved <see cref="FederationTarget"/>. Handles both regular (buffered) and
/// streaming (SSE / <c>/mcp-transport</c>) responses. Registered as a singleton.
/// </summary>
public sealed class FederationProxyService
{
    /// <summary>Custom header injected on every forwarded request to detect loops.</summary>
    public const string HopCountHeader = "X-Mcp-Federation-Hop";

    /// <summary>Diagnostic header identifying the originating server (port).</summary>
    public const string SourceHeader = "X-Mcp-Federation-Source";

    /// <summary>Named HttpClient key used to resolve the client from <see cref="IHttpClientFactory"/>.</summary>
    public const string HttpClientName = "FederationProxy";

    private static readonly HashSet<string> HopByHopHeaders = new(StringComparer.OrdinalIgnoreCase)
    {
        "Host",
        "Connection",
        "Transfer-Encoding",
        "Keep-Alive",
        "TE",
        "Trailers",
        "Proxy-Authorization",
        "Proxy-Authenticate",
        "Upgrade",
    };

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<FederationProxyService> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationProxyService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory used to resolve the named <c>FederationProxy</c> client.</param>
    /// <param name="logger">Logger.</param>
    public FederationProxyService(IHttpClientFactory httpClientFactory, ILogger<FederationProxyService> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    /// <summary>
    /// Proxies the inbound <paramref name="context"/> to <paramref name="target"/> and
    /// streams the response back, preserving status code, headers, and body.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="target">The resolved federation target.</param>
    /// <param name="hopCount">Hop depth to inject into the outbound request.</param>
    /// <param name="ct">Cancellation token.</param>
    public async Task ProxyAsync(
        HttpContext context,
        FederationTarget target,
        int hopCount,
        CancellationToken ct)
    {
        var targetUri = BuildTargetUri(context.Request, target.BaseUrl);
        _logger.LogDebug("Federation proxy: {Method} {Path} → {TargetUri} (hop {Hop})",
            context.Request.Method, context.Request.Path, targetUri, hopCount);

        using var outbound = new HttpRequestMessage(
            new HttpMethod(context.Request.Method),
            targetUri);

        // Copy request body when present
        if (HttpMethods.IsPost(context.Request.Method) ||
            HttpMethods.IsPut(context.Request.Method) ||
            HttpMethods.IsPatch(context.Request.Method) ||
            context.Request.ContentLength > 0 ||
            context.Request.Headers.TransferEncoding.Count > 0)
        {
            outbound.Content = new StreamContent(context.Request.Body);
            if (context.Request.ContentType is { } ct2)
                outbound.Content.Headers.TryAddWithoutValidation("Content-Type", ct2);
        }

        CopyRequestHeaders(context.Request, outbound, target, hopCount, context.RequestAborted);

        using var client = _httpClientFactory.CreateClient(HttpClientName);
        HttpResponseMessage response;

        try
        {
            response = await client.SendAsync(outbound, HttpCompletionOption.ResponseHeadersRead, ct)
                .ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            _logger.LogWarning(ex, "Federation proxy error forwarding to {Target}", target.BaseUrl);
            context.Response.StatusCode = 502;
            await context.Response.WriteAsync(
                $"{{\"error\":\"Federation proxy failed to reach target '{target.Name}'.\"}}", ct)
                .ConfigureAwait(false);
            return;
        }

        using (response)
        {
            context.Response.StatusCode = (int)response.StatusCode;
            CopyResponseHeaders(response, context.Response);

            if (IsStreamingRequest(context))
            {
                // SSE / MCP transport: flush each chunk immediately
                context.Response.Headers["Cache-Control"] = "no-cache";
                await using var stream = await response.Content.ReadAsStreamAsync(ct).ConfigureAwait(false);
                var buffer = new byte[4096];
                int read;
                while ((read = await stream.ReadAsync(buffer, ct).ConfigureAwait(false)) > 0)
                {
                    await context.Response.Body.WriteAsync(buffer.AsMemory(0, read), ct).ConfigureAwait(false);
                    await context.Response.Body.FlushAsync(ct).ConfigureAwait(false);
                }
            }
            else
            {
                await response.Content.CopyToAsync(context.Response.Body, ct).ConfigureAwait(false);
            }
        }
    }

    private static bool IsStreamingRequest(HttpContext context)
    {
        if (context.Request.Headers.Accept.ToString()
                .Contains("text/event-stream", StringComparison.OrdinalIgnoreCase))
            return true;

        if (context.Request.Path.StartsWithSegments("/mcp-transport", StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    private static string BuildTargetUri(HttpRequest request, string baseUrl)
    {
        var path = request.Path.Value ?? "/";
        var query = request.QueryString.Value ?? "";
        return $"{baseUrl}{path}{query}";
    }

    private static void CopyRequestHeaders(
        HttpRequest source,
        HttpRequestMessage outbound,
        FederationTarget target,
        int hopCount,
        CancellationToken _)
    {
        foreach (var header in source.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
                continue;

            // X-Api-Key: override with target-specific key when configured
            if (string.Equals(header.Key, "X-Api-Key", StringComparison.OrdinalIgnoreCase) &&
                target.ApiKey is not null)
                continue;  // will be added below

            // Content-Type is managed via Content.Headers — skip here
            if (string.Equals(header.Key, "Content-Type", StringComparison.OrdinalIgnoreCase))
                continue;

            if (!outbound.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string?>)header.Value))
                outbound.Content?.Headers.TryAddWithoutValidation(header.Key, (IEnumerable<string?>)header.Value);
        }

        // Inject target API key if configured (overrides any inbound X-Api-Key)
        if (target.ApiKey is not null)
            outbound.Headers.TryAddWithoutValidation("X-Api-Key", target.ApiKey);

        // Anti-loop hop counter
        outbound.Headers.Remove(HopCountHeader);
        outbound.Headers.TryAddWithoutValidation(HopCountHeader, hopCount.ToString());
    }

    private static void CopyResponseHeaders(HttpResponseMessage source, HttpResponse destination)
    {
        foreach (var header in source.Headers)
        {
            if (HopByHopHeaders.Contains(header.Key))
                continue;

            destination.Headers[header.Key] = header.Value.ToArray();
        }

        foreach (var header in source.Content.Headers)
        {
            if (string.Equals(header.Key, "Content-Length", StringComparison.OrdinalIgnoreCase))
                continue;  // streaming: let the body length be determined by transfer

            destination.Headers[header.Key] = header.Value.ToArray();
        }
    }
}
