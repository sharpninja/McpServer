using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using McpServer.Support.Mcp.Options;

namespace McpServer.Support.Mcp.Controllers;

/// <summary>
/// Exposes public OIDC configuration so CLI clients (Director) can auto-discover
/// Keycloak settings without prior knowledge of the auth infrastructure.
/// This endpoint is intentionally unauthenticated — it only returns public metadata.
/// </summary>
[ApiController]
[Route("auth")]
public sealed class AuthConfigController : ControllerBase
{
    /// <summary>
    /// Returns the public OIDC configuration for CLI clients.
    /// No secrets are exposed — only the authority URL, public client ID, and endpoint URLs.
    /// </summary>
    /// <param name="options">Bound from <c>Mcp:Auth</c> configuration section.</param>
    /// <returns>Public auth configuration or a disabled indicator.</returns>
    [HttpGet("config")]
    [ProducesResponseType(typeof(AuthConfigResponse), 200)]
    public IActionResult GetConfig([FromServices] IOptions<OidcAuthOptions> options)
    {
        var auth = options.Value;

        if (!auth.Enabled)
        {
            return Ok(new AuthConfigResponse
            {
                Enabled = false,
                Authority = "",
                ClientId = "",
                Scopes = "",
                DeviceAuthorizationEndpoint = "",
                TokenEndpoint = ""
            });
        }

        var authority = auth.Authority.TrimEnd('/');
        var proxyBaseUrl = $"{Request.Scheme}://{Request.Host}";

        return Ok(new AuthConfigResponse
        {
            Enabled = true,
            Authority = authority,
            ClientId = auth.DirectorClientId,
            Scopes = "openid profile email",
            DeviceAuthorizationEndpoint = $"{proxyBaseUrl}/auth/device",
            TokenEndpoint = $"{proxyBaseUrl}/auth/token"
        });
    }

    /// <summary>
    /// Proxies the OAuth 2.0 Device Authorization request to Keycloak so clients
    /// can stay on the MCP host/port (e.g. Android can call :7147 instead of :7080).
    /// </summary>
    [HttpPost("device")]
    [Consumes("application/x-www-form-urlencoded")]
    public Task<IActionResult> ProxyDeviceAuthorization(
        [FromServices] IOptions<OidcAuthOptions> options,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] ILogger<AuthConfigController> logger,
        CancellationToken cancellationToken)
        => ProxyOidcFormPostAsync(
            options.Value,
            GetDeviceAuthorizationEndpoint(options.Value),
            httpClientFactory,
            logger,
            cancellationToken,
            rewriteDeviceVerificationUris: true);

    /// <summary>
    /// Proxies the OAuth 2.0 Token request to Keycloak so clients can stay on the
    /// MCP host/port (e.g. Android can call :7147 instead of :7080).
    /// </summary>
    [HttpPost("token")]
    [Consumes("application/x-www-form-urlencoded")]
    public Task<IActionResult> ProxyToken(
        [FromServices] IOptions<OidcAuthOptions> options,
        [FromServices] IHttpClientFactory httpClientFactory,
        [FromServices] ILogger<AuthConfigController> logger,
        CancellationToken cancellationToken)
        => ProxyOidcFormPostAsync(
            options.Value,
            GetTokenEndpoint(options.Value),
            httpClientFactory,
            logger,
            cancellationToken);

    /// <summary>
    /// Browser-facing Keycloak UI proxy for device-flow verification pages and supporting assets.
    /// Keeps browser traffic on the MCP host/port instead of requiring direct Keycloak access.
    /// </summary>
    [HttpGet("ui/{**path}")]
    [HttpPost("ui/{**path}")]
    public Task<IActionResult> ProxyBrowserUi(
        string? path,
        [FromServices] IOptions<OidcAuthOptions> options,
        [FromServices] ILogger<AuthConfigController> logger,
        CancellationToken cancellationToken)
        => ProxyOidcBrowserUiAsync(path, options.Value, logger, cancellationToken);

    private async Task<IActionResult> ProxyOidcFormPostAsync(
        OidcAuthOptions authOptions,
        string? endpoint,
        IHttpClientFactory httpClientFactory,
        ILogger<AuthConfigController> logger,
        CancellationToken cancellationToken,
        bool rewriteDeviceVerificationUris = false)
    {
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            return Problem(
                title: "OIDC authentication is not enabled.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        string body;
        using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true))
        {
            body = await reader.ReadToEndAsync(cancellationToken);
        }

        using var outbound = new HttpRequestMessage(HttpMethod.Post, endpoint);
        outbound.Content = new StringContent(body, Encoding.UTF8);

        var inboundContentType = Request.ContentType;
        if (!string.IsNullOrWhiteSpace(inboundContentType))
        {
            outbound.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(inboundContentType);
        }
        else
        {
            outbound.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");
        }

        foreach (var header in Request.Headers)
        {
            if (header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var value in header.Value)
                {
                    if (MediaTypeWithQualityHeaderValue.TryParse(value, out var accept))
                    {
                        outbound.Headers.Accept.Add(accept);
                    }
                }
            }
        }

        try
        {
            var client = httpClientFactory.CreateClient();
            using var response = await client.SendAsync(outbound, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var contentType = response.Content.Headers.ContentType?.ToString() ?? "application/json";
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (rewriteDeviceVerificationUris)
            {
                content = RewriteDeviceAuthorizationResponse(content, contentType, authOptions);
            }

            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                ContentType = contentType,
                Content = content
            };
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OIDC proxy request failed for endpoint {Endpoint}", endpoint);
            return Problem(
                title: "OIDC upstream request failed.",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private async Task<IActionResult> ProxyOidcBrowserUiAsync(
        string? path,
        OidcAuthOptions authOptions,
        ILogger<AuthConfigController> logger,
        CancellationToken cancellationToken)
    {
        if (!TryGetKeycloakAuthorityUris(authOptions, out var keycloakAuthorityUri, out var keycloakHostBaseUri))
        {
            return Problem(
                title: "OIDC authentication is not enabled.",
                statusCode: StatusCodes.Status503ServiceUnavailable);
        }

        var proxyPath = NormalizeUiProxyPath(path);
        if (string.IsNullOrWhiteSpace(proxyPath) || !IsAllowedUiProxyPath(proxyPath))
        {
            return NotFound();
        }

        var targetUri = new Uri(keycloakHostBaseUri, "/" + proxyPath + Request.QueryString);

        using var handler = new HttpClientHandler
        {
            AllowAutoRedirect = false,
            UseCookies = false
        };
        using var client = new HttpClient(handler, disposeHandler: true);
        using var outbound = new HttpRequestMessage(new HttpMethod(Request.Method), targetUri);

        if (Request.ContentLength is > 0)
        {
            var body = await ReadRequestBodyAsync(cancellationToken);
            outbound.Content = new StringContent(body, Encoding.UTF8);
            if (!string.IsNullOrWhiteSpace(Request.ContentType))
            {
                outbound.Content.Headers.ContentType = MediaTypeHeaderValue.Parse(Request.ContentType);
            }
        }

        CopyInboundBrowserProxyRequestHeaders(outbound);

        try
        {
            using var response = await client.SendAsync(outbound, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            var proxyUiPrefix = "/auth/ui";
            var proxyUiBaseUrl = GetProxyUiBaseUrl();

            Response.StatusCode = (int)response.StatusCode;
            CopyOutboundBrowserProxyResponseHeaders(response, keycloakHostBaseUri, proxyUiBaseUrl, proxyUiPrefix);

            var contentType = response.Content.Headers.ContentType?.ToString();
            if (IsHtmlResponse(contentType))
            {
                var html = await response.Content.ReadAsStringAsync(cancellationToken);
                html = RewriteBrowserHtmlForUiProxy(html, keycloakHostBaseUri, proxyUiBaseUrl, proxyUiPrefix);
                return new ContentResult
                {
                    StatusCode = (int)response.StatusCode,
                    ContentType = contentType ?? "text/html; charset=utf-8",
                    Content = html
                };
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            return new FileContentResult(bytes, contentType ?? "application/octet-stream");
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested || HttpContext.RequestAborted.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "OIDC browser UI proxy request failed for path {Path}", proxyPath);
            return Problem(
                title: "OIDC browser proxy request failed.",
                detail: ex.Message,
                statusCode: StatusCodes.Status502BadGateway);
        }
    }

    private string RewriteDeviceAuthorizationResponse(string content, string contentType, OidcAuthOptions authOptions)
    {
        if (!IsJsonResponse(contentType))
        {
            return content;
        }

        if (!TryGetKeycloakAuthorityUris(authOptions, out var keycloakAuthorityUri, out var keycloakHostBaseUri))
        {
            return content;
        }

        JsonNode? root;
        try
        {
            root = JsonNode.Parse(content);
        }
        catch
        {
            return content;
        }

        if (root is not JsonObject obj)
        {
            return content;
        }

        var proxyUiBaseUrl = GetProxyUiBaseUrl();
        var devicePagePath = $"{keycloakAuthorityUri.AbsolutePath.TrimEnd('/')}/device";
        var defaultVerificationUri = $"{proxyUiBaseUrl}{devicePagePath}";

        if (obj["verification_uri"] is JsonValue verificationUriValue &&
            verificationUriValue.TryGetValue<string>(out var verificationUri) &&
            !string.IsNullOrWhiteSpace(verificationUri))
        {
            obj["verification_uri"] = RewriteKeycloakUrlForUiProxy(verificationUri, keycloakHostBaseUri, proxyUiBaseUrl);
        }
        else
        {
            obj["verification_uri"] = defaultVerificationUri;
        }

        if (obj["verification_uri_complete"] is JsonValue verificationUriCompleteValue &&
            verificationUriCompleteValue.TryGetValue<string>(out var verificationUriComplete) &&
            !string.IsNullOrWhiteSpace(verificationUriComplete))
        {
            obj["verification_uri_complete"] = RewriteKeycloakUrlForUiProxy(verificationUriComplete, keycloakHostBaseUri, proxyUiBaseUrl);
        }

        return obj.ToJsonString(new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }

    private async Task<string> ReadRequestBodyAsync(CancellationToken cancellationToken)
    {
        using var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }

    private void CopyInboundBrowserProxyRequestHeaders(HttpRequestMessage outbound)
    {
        foreach (var header in Request.Headers)
        {
            if (!ShouldForwardBrowserRequestHeader(header.Key))
            {
                continue;
            }

            if (header.Key.Equals("Accept", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var value in header.Value)
                {
                    if (MediaTypeWithQualityHeaderValue.TryParse(value, out var accept))
                    {
                        outbound.Headers.Accept.Add(accept);
                    }
                }

                continue;
            }

            if (!outbound.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) &&
                outbound.Content is not null)
            {
                outbound.Content.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }
    }

    private void CopyOutboundBrowserProxyResponseHeaders(
        HttpResponseMessage upstream,
        Uri keycloakHostBaseUri,
        string proxyUiBaseUrl,
        string proxyUiPrefix)
    {
        CopyHeaderCollection(upstream.Headers, keycloakHostBaseUri, proxyUiBaseUrl, proxyUiPrefix);
        CopyHeaderCollection(upstream.Content.Headers, keycloakHostBaseUri, proxyUiBaseUrl, proxyUiPrefix);
    }

    private void CopyHeaderCollection(
        IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers,
        Uri keycloakHostBaseUri,
        string proxyUiBaseUrl,
        string proxyUiPrefix)
    {
        foreach (var header in headers)
        {
            if (header.Key.Equals("Content-Length", StringComparison.OrdinalIgnoreCase) ||
                header.Key.Equals("Transfer-Encoding", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (header.Key.Equals("Location", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var value in header.Value)
                {
                    Response.Headers.Append(header.Key, RewriteKeycloakUrlForUiProxy(value, keycloakHostBaseUri, proxyUiBaseUrl));
                }

                continue;
            }

            if (header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase))
            {
                foreach (var value in header.Value)
                {
                    Response.Headers.Append(header.Key, RewriteSetCookieForUiProxy(value, proxyUiPrefix));
                }

                continue;
            }

            foreach (var value in header.Value)
            {
                Response.Headers.Append(header.Key, value);
            }
        }
    }

    private static bool ShouldForwardBrowserRequestHeader(string headerName)
        => headerName.Equals("Accept", StringComparison.OrdinalIgnoreCase) ||
           headerName.Equals("Accept-Language", StringComparison.OrdinalIgnoreCase) ||
           headerName.Equals("User-Agent", StringComparison.OrdinalIgnoreCase) ||
           headerName.Equals("Cookie", StringComparison.OrdinalIgnoreCase) ||
           headerName.Equals("Referer", StringComparison.OrdinalIgnoreCase) ||
           headerName.Equals("Origin", StringComparison.OrdinalIgnoreCase) ||
           headerName.Equals("Cache-Control", StringComparison.OrdinalIgnoreCase) ||
           headerName.Equals("Pragma", StringComparison.OrdinalIgnoreCase) ||
           headerName.StartsWith("Sec-", StringComparison.OrdinalIgnoreCase);

    private static bool IsAllowedUiProxyPath(string path)
        => path.StartsWith("realms/", StringComparison.OrdinalIgnoreCase) ||
           path.StartsWith("resources/", StringComparison.OrdinalIgnoreCase);

    private static string NormalizeUiProxyPath(string? path)
        => (path ?? "").Trim().TrimStart('/');

    private static bool IsHtmlResponse(string? contentType)
        => !string.IsNullOrWhiteSpace(contentType) &&
           contentType.StartsWith("text/html", StringComparison.OrdinalIgnoreCase);

    private static bool IsJsonResponse(string? contentType)
        => !string.IsNullOrWhiteSpace(contentType) &&
           contentType.Contains("json", StringComparison.OrdinalIgnoreCase);

    private string GetProxyUiBaseUrl()
        => $"{Request.Scheme}://{Request.Host}/auth/ui";

    private static bool TryGetKeycloakAuthorityUris(OidcAuthOptions authOptions, out Uri authorityUri, out Uri keycloakHostBaseUri)
    {
        authorityUri = null!;
        keycloakHostBaseUri = null!;

        if (!authOptions.Enabled || !Uri.TryCreate(authOptions.Authority, UriKind.Absolute, out var parsedAuthorityUri))
        {
            return false;
        }

        authorityUri = parsedAuthorityUri;
        keycloakHostBaseUri = new Uri(authorityUri.GetLeftPart(UriPartial.Authority));
        return true;
    }

    private static string RewriteBrowserHtmlForUiProxy(string html, Uri keycloakHostBaseUri, string proxyUiBaseUrl, string proxyUiPrefix)
    {
        if (string.IsNullOrEmpty(html))
        {
            return html;
        }

        var keycloakOrigin = keycloakHostBaseUri.GetLeftPart(UriPartial.Authority);
        html = html.Replace(keycloakOrigin, proxyUiBaseUrl, StringComparison.OrdinalIgnoreCase);

        // Keycloak emits root-relative links in both HTML attributes and JS string literals.
        html = html.Replace("\"/realms/", $"\"{proxyUiPrefix}/realms/", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("\"/resources/", $"\"{proxyUiPrefix}/resources/", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("'/realms/", $"'{proxyUiPrefix}/realms/", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("'/resources/", $"'{proxyUiPrefix}/resources/", StringComparison.OrdinalIgnoreCase);
        html = html.Replace("url(/", $"url({proxyUiPrefix}/", StringComparison.OrdinalIgnoreCase);

        return html;
    }

    private static string RewriteKeycloakUrlForUiProxy(string value, Uri keycloakHostBaseUri, string proxyUiBaseUrl)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return value;
        }

        if (Uri.TryCreate(value, UriKind.Absolute, out var absolute))
        {
            if (Uri.Compare(
                    new Uri(absolute.GetLeftPart(UriPartial.Authority)),
                    keycloakHostBaseUri,
                    UriComponents.SchemeAndServer,
                    UriFormat.Unescaped,
                    StringComparison.OrdinalIgnoreCase) == 0)
            {
                return $"{proxyUiBaseUrl}{absolute.PathAndQuery}{absolute.Fragment}";
            }

            return value;
        }

        if (value.StartsWith("/") && !value.StartsWith("/auth/ui/", StringComparison.OrdinalIgnoreCase))
        {
            return $"{proxyUiBaseUrl}{value}";
        }

        return value;
    }

    private static string RewriteSetCookieForUiProxy(string setCookie, string proxyUiPrefix)
    {
        if (string.IsNullOrWhiteSpace(setCookie))
        {
            return setCookie;
        }

        var parts = setCookie.Split(';', StringSplitOptions.None);
        var rewritten = new List<string>(parts.Length);
        var pathRewritten = false;

        foreach (var rawPart in parts)
        {
            var part = rawPart.Trim();

            if (part.StartsWith("Domain=", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (part.StartsWith("Path=", StringComparison.OrdinalIgnoreCase))
            {
                var cookiePath = part.Substring("Path=".Length).Trim();
                if (!cookiePath.StartsWith(proxyUiPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    cookiePath = cookiePath.StartsWith("/", StringComparison.Ordinal)
                        ? $"{proxyUiPrefix}{cookiePath}"
                        : $"{proxyUiPrefix}/{cookiePath}";
                }

                rewritten.Add($"Path={cookiePath}");
                pathRewritten = true;
                continue;
            }

            rewritten.Add(part);
        }

        if (!pathRewritten)
        {
            rewritten.Add($"Path={proxyUiPrefix}");
        }

        return string.Join("; ", rewritten);
    }

    private static string? GetDeviceAuthorizationEndpoint(OidcAuthOptions options)
        => BuildRealmEndpoint(options, "protocol/openid-connect/auth/device");

    private static string? GetTokenEndpoint(OidcAuthOptions options)
        => BuildRealmEndpoint(options, "protocol/openid-connect/token");

    private static string? BuildRealmEndpoint(OidcAuthOptions options, string relativePath)
    {
        if (!options.Enabled)
        {
            return null;
        }

        var authority = options.Authority.TrimEnd('/');
        return $"{authority}/{relativePath}";
    }
}

/// <summary>
/// Public OIDC configuration response for CLI clients.
/// Contains only public metadata — no secrets.
/// </summary>
public sealed class AuthConfigResponse
{
    /// <summary>Whether OIDC authentication is enabled on this server.</summary>
    public bool Enabled { get; set; }

    /// <summary>Keycloak realm authority URL.</summary>
    public string Authority { get; set; } = "";

    /// <summary>Public client ID for the Director CLI (Device Authorization Flow).</summary>
    public string ClientId { get; set; } = "";

    /// <summary>OAuth scopes to request.</summary>
    public string Scopes { get; set; } = "";

    /// <summary>OAuth 2.0 Device Authorization endpoint.</summary>
    public string DeviceAuthorizationEndpoint { get; set; } = "";

    /// <summary>OAuth 2.0 Token endpoint.</summary>
    public string TokenEndpoint { get; set; } = "";
}
