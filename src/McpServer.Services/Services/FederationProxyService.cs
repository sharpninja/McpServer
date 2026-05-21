using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using McpServer.Support.Mcp.Options;
using System.Text.Json;

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
    private readonly IFederationTopologyService? _topologyService;
    private readonly IOptions<FederationOptions>? _options;

    /// <summary>
    /// Initializes a new instance of the <see cref="FederationProxyService"/> class.
    /// </summary>
    /// <param name="httpClientFactory">Factory used to resolve the named <c>FederationProxy</c> client.</param>
    /// <param name="logger">Logger.</param>
    /// <param name="topologyService">Optional topology service used to queue local proxy writes after hub outages.</param>
    /// <param name="options">Optional federation options.</param>
    public FederationProxyService(
        IHttpClientFactory httpClientFactory,
        ILogger<FederationProxyService> logger,
        IFederationTopologyService? topologyService = null,
        IOptions<FederationOptions>? options = null)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
        _topologyService = topologyService;
        _options = options;
    }

    /// <summary>
    /// Proxies the inbound <paramref name="context"/> to <paramref name="target"/> and
    /// streams the response back, preserving status code, headers, and body.
    /// </summary>
    /// <param name="context">The current HTTP context.</param>
    /// <param name="target">The resolved federation target.</param>
    /// <param name="hopCount">Hop depth to inject into the outbound request.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <param name="proxyId">Optional proxy identifier to send to the hub.</param>
    /// <param name="globalWorkspaceId">Optional global workspace identifier to send to the hub.</param>
    /// <param name="queueOnFailure">Whether mutating requests should be queued when the target cannot be reached.</param>
    public async Task ProxyAsync(
        HttpContext context,
        FederationTarget target,
        int hopCount,
        CancellationToken ct,
        string? proxyId = null,
        string? globalWorkspaceId = null,
        bool queueOnFailure = false)
    {
        var targetUri = BuildTargetUri(context.Request, target.BaseUrl);
        _logger.LogDebug("Federation proxy: {Method} {Path} → {TargetUri} (hop {Hop})",
            context.Request.Method, context.Request.Path, targetUri, hopCount);

        var operationId = ResolveOperationId(context.Request);
        var sourceOperationId = context.Request.Headers[FederationHeaders.SourceOperationId].FirstOrDefault();
        var bodyCapture = await CaptureBodyForQueueAsync(context.Request, queueOnFailure, ct).ConfigureAwait(false);

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
            outbound.Content = bodyCapture.Body is null
                ? new StreamContent(context.Request.Body)
                : new ByteArrayContent(bodyCapture.Body);
            if (context.Request.ContentType is { } ct2)
                outbound.Content.Headers.TryAddWithoutValidation("Content-Type", ct2);
        }

        CopyRequestHeaders(context.Request, outbound, target, hopCount, proxyId, globalWorkspaceId, operationId, sourceOperationId);

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
            if (await TryQueueFailedWriteAsync(
                    context,
                    operationId,
                    sourceOperationId,
                    proxyId,
                    globalWorkspaceId,
                    bodyCapture,
                    ct).ConfigureAwait(false))
            {
                return;
            }

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
        string? proxyId,
        string? globalWorkspaceId,
        string operationId,
        string? sourceOperationId)
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

        if (!string.IsNullOrWhiteSpace(proxyId))
        {
            outbound.Headers.Remove(FederationHeaders.ProxyId);
            outbound.Headers.TryAddWithoutValidation(FederationHeaders.ProxyId, proxyId);
        }

        if (!string.IsNullOrWhiteSpace(globalWorkspaceId))
        {
            outbound.Headers.Remove(FederationHeaders.GlobalWorkspaceId);
            outbound.Headers.TryAddWithoutValidation(FederationHeaders.GlobalWorkspaceId, globalWorkspaceId);
        }

        outbound.Headers.Remove(FederationHeaders.OperationId);
        outbound.Headers.TryAddWithoutValidation(FederationHeaders.OperationId, operationId);

        if (!string.IsNullOrWhiteSpace(sourceOperationId))
        {
            outbound.Headers.Remove(FederationHeaders.SourceOperationId);
            outbound.Headers.TryAddWithoutValidation(FederationHeaders.SourceOperationId, sourceOperationId);
        }
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

    private async Task<bool> TryQueueFailedWriteAsync(
        HttpContext context,
        string operationId,
        string? sourceOperationId,
        string? proxyId,
        string? globalWorkspaceId,
        RequestBodyCapture bodyCapture,
        CancellationToken cancellationToken)
    {
        if (!bodyCapture.CanQueue ||
            _topologyService is null ||
            _options?.Value.Queue.Enabled != true ||
            !IsMutatingRequest(context.Request) ||
            string.IsNullOrWhiteSpace(proxyId))
        {
            return false;
        }

        var response = await _topologyService.QueueLocalOperationAsync(new FederationOperationRequest
        {
            OperationId = operationId,
            ProxyId = proxyId,
            SourceOperationId = sourceOperationId,
            GlobalWorkspaceId = globalWorkspaceId,
            Domain = InferDomain(context.Request.Path),
            ResourceId = InferResourceId(context.Request),
            HttpMethod = context.Request.Method,
            Path = BuildReplayPath(context.Request),
            HeadersJson = SerializeReplayHeaders(context.Request),
            BodyBase64 = bodyCapture.Body is { Length: > 0 } ? Convert.ToBase64String(bodyCapture.Body) : null,
            BaseVersion = context.Request.Headers["If-Match"].FirstOrDefault(),
        }, cancellationToken).ConfigureAwait(false);

        context.Response.StatusCode = StatusCodes.Status202Accepted;
        context.Response.ContentType = "application/json";
        context.Response.Headers[FederationHeaders.Queued] = "true";
        context.Response.Headers[FederationHeaders.OperationId] = response.OperationId;
        await context.Response.WriteAsync(
            JsonSerializer.Serialize(new
            {
                operationId = response.OperationId,
                status = response.Status,
                queued = true,
            }),
            cancellationToken)
            .ConfigureAwait(false);
        return true;
    }

    private async Task<RequestBodyCapture> CaptureBodyForQueueAsync(
        HttpRequest request,
        bool queueOnFailure,
        CancellationToken cancellationToken)
    {
        if (!queueOnFailure || !IsMutatingRequest(request) || _options?.Value.Queue.Enabled != true)
            return RequestBodyCapture.NotQueued;

        if (!HasRequestBody(request))
            return RequestBodyCapture.Empty;

        var maxBodyBytes = _options.Value.Queue.MaxBodyBytes;
        if (request.ContentLength is > 0 && request.ContentLength > maxBodyBytes)
            return RequestBodyCapture.NotQueued;

        request.EnableBuffering();
        await using var memory = new MemoryStream();
        await request.Body.CopyToAsync(memory, cancellationToken).ConfigureAwait(false);
        if (memory.Length > maxBodyBytes)
        {
            request.Body.Position = 0;
            return RequestBodyCapture.NotQueued;
        }

        request.Body.Position = 0;
        return new RequestBodyCapture(true, memory.ToArray());
    }

    private static bool HasRequestBody(HttpRequest request)
        => request.ContentLength > 0 || request.Headers.TransferEncoding.Count > 0;

    private static bool IsMutatingRequest(HttpRequest request)
        => HttpMethods.IsPost(request.Method) ||
           HttpMethods.IsPut(request.Method) ||
           HttpMethods.IsPatch(request.Method) ||
           HttpMethods.IsDelete(request.Method);

    private static string ResolveOperationId(HttpRequest request)
    {
        var operationId = request.Headers[FederationHeaders.OperationId].FirstOrDefault();
        return string.IsNullOrWhiteSpace(operationId) ? $"fedop-{Guid.NewGuid():N}" : operationId.Trim();
    }

    private static string BuildReplayPath(HttpRequest request)
        => $"{request.Path.Value ?? "/"}{request.QueryString.Value ?? string.Empty}";

    private static string InferDomain(PathString path)
    {
        var value = path.Value ?? string.Empty;
        if (value.StartsWith("/mcpserver/todo", StringComparison.OrdinalIgnoreCase))
            return "todo";
        if (value.StartsWith("/mcpserver/sessionlog", StringComparison.OrdinalIgnoreCase))
            return "session_log";
        if (value.StartsWith("/mcpserver/requirements", StringComparison.OrdinalIgnoreCase))
            return "requirements";
        if (value.StartsWith("/mcpserver/context", StringComparison.OrdinalIgnoreCase))
            return "context_metadata";
        if (value.StartsWith("/mcpserver/tools", StringComparison.OrdinalIgnoreCase))
            return "tools_buckets";
        if (value.StartsWith("/mcpserver/agents", StringComparison.OrdinalIgnoreCase))
            return "agents";
        if (value.StartsWith("/mcpserver/gh", StringComparison.OrdinalIgnoreCase))
            return "github_metadata";
        if (value.StartsWith("/mcpserver/repo", StringComparison.OrdinalIgnoreCase))
            return "repo_file_changes";
        if (value.StartsWith("/marker-file-timestamp", StringComparison.OrdinalIgnoreCase))
            return "marker_state";
        if (value.StartsWith("/mcpserver/workspace", StringComparison.OrdinalIgnoreCase))
            return "workspace";
        if (value.StartsWith("/mcp-transport", StringComparison.OrdinalIgnoreCase))
            return "mcp_transport";

        return "unknown";
    }

    private static string? InferResourceId(HttpRequest request)
    {
        if (request.RouteValues.TryGetValue("id", out var routeId) && routeId is not null)
            return Convert.ToString(routeId, System.Globalization.CultureInfo.InvariantCulture);

        if (request.Query.TryGetValue("id", out var queryId))
            return queryId.FirstOrDefault();

        return request.Path.Value;
    }

    private static string SerializeReplayHeaders(HttpRequest request)
    {
        var headers = request.Headers
            .Where(h => !IsSecretHeader(h.Key))
            .ToDictionary(h => h.Key, h => h.Value.ToArray(), StringComparer.OrdinalIgnoreCase);
        return JsonSerializer.Serialize(headers);
    }

    private static bool IsSecretHeader(string header)
        => string.Equals(header, "Authorization", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(header, "Cookie", StringComparison.OrdinalIgnoreCase) ||
           string.Equals(header, "X-Api-Key", StringComparison.OrdinalIgnoreCase);

    private sealed record RequestBodyCapture(bool CanQueue, byte[]? Body)
    {
        public static RequestBodyCapture NotQueued { get; } = new(false, null);

        public static RequestBodyCapture Empty { get; } = new(true, null);
    }
}
