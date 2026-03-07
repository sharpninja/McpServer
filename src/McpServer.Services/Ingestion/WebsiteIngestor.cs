using System.Net;
using System.Net.Http.Headers;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using McpServer.Support.Mcp.Indexing;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Ingestion;

/// <summary>
/// FR-MCP-065, TR-MCP-INGEST-003: Direct website URL ingestion with SSRF guards and bounded crawling.
/// </summary>
public sealed partial class WebsiteIngestor : IWebsiteIngestor
{
    /// <summary>Named HTTP client used by the website ingestor.</summary>
    public const string HttpClientName = "WebsiteIngestor";

    private readonly Chunker _chunker;
    private readonly Func<HttpClient>? _httpClientFactory;
    private readonly IngestionOptions _options;
    private readonly WorkspaceContext _workspaceContext;
    private readonly ILogger<WebsiteIngestor> _logger;

    /// <summary>TR-MCP-INGEST-003: Constructor.</summary>
    public WebsiteIngestor(
        Chunker chunker,
        IOptions<IngestionOptions> options,
        WorkspaceContext workspaceContext,
        ILogger<WebsiteIngestor> logger,
        Func<HttpClient>? httpClientFactory = null)
    {
        _chunker = chunker;
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _workspaceContext = workspaceContext;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<WebsiteIngestPage>> IngestAsync(
        WebsiteIngestRequest request,
        Func<WebsiteIngestPage, Task>? onPageFetched = null,
        CancellationToken cancellationToken = default)
    {
        var pages = new List<WebsiteIngestPage>();
        var maxPages = Math.Clamp(request.MaxPages, 1, _options.MaxWebsitePages);
        var maxDepth = Math.Clamp(request.MaxDepth, 0, _options.MaxWebsiteDepth);
        var maxBytes = Math.Clamp(request.MaxBytesPerPage, 4096, _options.MaxWebsiteBytesPerPage);

        if (!TryNormalizeUrl(request.Url, out var startUri, out var normalizeError))
        {
            pages.Add(new WebsiteIngestPage
            {
                Url = request.Url,
                Outcome = new WebsiteIngestUrlResult
                {
                    Url = request.Url,
                    Status = "error",
                    Message = normalizeError
                }
            });
            return pages;
        }

        var start = startUri!;

        var queue = new Queue<(Uri Url, int Depth)>();
        var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        queue.Enqueue((start, 0));

        while (queue.Count > 0 && pages.Count < maxPages)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var (candidateUrl, depth) = queue.Dequeue();
            var normalized = NormalizeUrl(candidateUrl);
            if (!visited.Add(normalized))
            {
                continue;
            }

            var page = await FetchAndConvertAsync(candidateUrl, maxBytes, cancellationToken).ConfigureAwait(false);
            pages.Add(page);
            if (onPageFetched is not null)
            {
                await onPageFetched(page).ConfigureAwait(false);
            }

            if (!request.IncludeSubpages || depth >= maxDepth || !string.Equals(page.Outcome.Status, "ingested", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var links = page.DiscoveredLinks;
            foreach (var link in OrderCrawlLinks(links, start))
            {
                if (queue.Count + pages.Count >= maxPages)
                {
                    break;
                }

                if (!TryNormalizeUrl(link, out var normalizedLink, out _))
                {
                    continue;
                }

                if (!string.Equals(normalizedLink!.Host, start.Host, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (ShouldSkipCrawlLink(normalizedLink))
                {
                    continue;
                }

                var linkKey = NormalizeUrl(normalizedLink);
                if (!visited.Contains(linkKey))
                {
                    queue.Enqueue((normalizedLink, depth + 1));
                }
            }
        }

        return pages;
    }

    private async Task<WebsiteIngestPage> FetchAndConvertAsync(Uri inputUrl, int maxBytes, CancellationToken cancellationToken)
    {
        try
        {
            var fetched = await FetchWithRedirectsAsync(inputUrl, maxBytes, cancellationToken).ConfigureAwait(false);
            if (!fetched.Success || fetched.FinalUri is null)
            {
                return new WebsiteIngestPage
                {
                    Url = NormalizeUrl(inputUrl),
                    Outcome = new WebsiteIngestUrlResult
                    {
                        Url = NormalizeUrl(inputUrl),
                        Status = "error",
                        Message = fetched.Error ?? "Failed to fetch content."
                    }
                };
            }

            var extractedText = ExtractContentText(fetched.ContentType, fetched.Body, fetched.FinalUri);
            var discoveredLinks = IsHtmlContent(fetched.ContentType, fetched.FinalUri)
                ? ExtractLinks(fetched.FinalUri, fetched.Body)
                : [];
            if (string.IsNullOrWhiteSpace(extractedText))
            {
                return new WebsiteIngestPage
                {
                    Url = NormalizeUrl(fetched.FinalUri),
                    Outcome = new WebsiteIngestUrlResult
                    {
                        Url = NormalizeUrl(fetched.FinalUri),
                        Status = "skipped",
                        Message = "Page content was empty after extraction."
                    }
                };
            }

            var canonicalUrl = NormalizeUrl(fetched.FinalUri);
            var documentId = BuildWorkspaceScopedDocumentId("external-web", ResolveRepoRoot(), canonicalUrl);
            var contentHash = ComputeHash(extractedText);
            var doc = new ContextDocument
            {
                Id = documentId,
                SourceType = "external-web",
                SourceKey = canonicalUrl,
                IngestedAt = DateTime.UtcNow,
                ContentHash = contentHash
            };
            var chunks = _chunker.Chunk(documentId, extractedText);
            return new WebsiteIngestPage
            {
                Url = canonicalUrl,
                Document = doc,
                Chunks = chunks,
                DiscoveredLinks = discoveredLinks,
                Outcome = new WebsiteIngestUrlResult
                {
                    Url = canonicalUrl,
                    SourceKey = canonicalUrl,
                    Status = "ingested",
                    ChunksWritten = chunks.Count
                }
            };
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Website ingestion failed for {Url}", inputUrl);
            return new WebsiteIngestPage
            {
                Url = NormalizeUrl(inputUrl),
                Outcome = new WebsiteIngestUrlResult
                {
                    Url = NormalizeUrl(inputUrl),
                    Status = "error",
                    Message = ex.Message
                }
            };
        }
    }

    private async Task<(bool Success, Uri? FinalUri, string Body, string ContentType, string? Error)> FetchWithRedirectsAsync(Uri startUrl, int maxBytes, CancellationToken cancellationToken)
    {
        var current = startUrl;
        var maxRedirects = Math.Clamp(_options.MaxWebsiteRedirects, 0, 10);
        using var client = _httpClientFactory?.Invoke() ?? CreateDefaultHttpClient();

        for (var i = 0; i <= maxRedirects; i++)
        {
            var safetyError = await ValidateTargetAsync(current, cancellationToken).ConfigureAwait(false);
            if (!string.IsNullOrEmpty(safetyError))
            {
                return (false, current, string.Empty, string.Empty, safetyError);
            }

            var fetchResult = await FetchWithRetriesAsync(client, current, maxBytes, cancellationToken).ConfigureAwait(false);
            if (!fetchResult.Success)
            {
                return (false, current, string.Empty, string.Empty, fetchResult.Error ?? "Failed to fetch content.");
            }

            if (IsRedirect(fetchResult.StatusCode))
            {
                if (fetchResult.RedirectLocation is null)
                {
                    return (false, current, string.Empty, string.Empty, "Redirect response missing Location header.");
                }

                current = fetchResult.RedirectLocation.IsAbsoluteUri
                    ? fetchResult.RedirectLocation
                    : new Uri(current, fetchResult.RedirectLocation);
                continue;
            }

            return (true, current, fetchResult.Body, fetchResult.ContentType, null);
        }

        return (false, current, string.Empty, string.Empty, "Too many redirects.");
    }

    private async Task<(bool Success, HttpStatusCode StatusCode, Uri? RedirectLocation, string Body, string ContentType, string? Error)> FetchWithRetriesAsync(
        HttpClient client,
        Uri current,
        int maxBytes,
        CancellationToken cancellationToken)
    {
        var maxAttempts = Math.Clamp(_options.WebsiteRequestMaxAttempts, 1, 10);
        var baseDelayMs = Math.Clamp(_options.WebsiteRequestRetryDelayMilliseconds, 100, 30_000);

        for (var attempt = 1; attempt <= maxAttempts; attempt++)
        {
            try
            {
                using var request = new HttpRequestMessage(HttpMethod.Get, current);
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/html"));
                request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));

                using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken).ConfigureAwait(false);
                if (IsRedirect(response.StatusCode))
                {
                    return (true, response.StatusCode, response.Headers.Location, string.Empty, string.Empty, null);
                }

                if (!response.IsSuccessStatusCode)
                {
                    if (attempt < maxAttempts && IsTransientStatusCode(response.StatusCode))
                    {
                        await DelayForRetryAsync(baseDelayMs, attempt, cancellationToken).ConfigureAwait(false);
                        continue;
                    }

                    return (false, response.StatusCode, null, string.Empty, string.Empty, $"HTTP {(int)response.StatusCode} returned from target.");
                }

                await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
                var body = await ReadLimitedTextAsync(stream, maxBytes, cancellationToken).ConfigureAwait(false);
                var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
                return (true, response.StatusCode, null, body, contentType, null);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                if (attempt < maxAttempts)
                {
                    await DelayForRetryAsync(baseDelayMs, attempt, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return (false, HttpStatusCode.RequestTimeout, null, string.Empty, string.Empty, ex.Message);
            }
            catch (HttpRequestException ex)
            {
                if (attempt < maxAttempts)
                {
                    await DelayForRetryAsync(baseDelayMs, attempt, cancellationToken).ConfigureAwait(false);
                    continue;
                }

                return (false, HttpStatusCode.ServiceUnavailable, null, string.Empty, string.Empty, ex.Message);
            }
        }

        return (false, HttpStatusCode.RequestTimeout, null, string.Empty, string.Empty, "Request failed after retry attempts.");
    }

    private static async Task DelayForRetryAsync(int baseDelayMs, int attempt, CancellationToken cancellationToken)
    {
        var exponent = Math.Clamp(attempt - 1, 0, 6);
        var delayMs = (int)Math.Min(baseDelayMs * Math.Pow(2, exponent), 30_000);
        await Task.Delay(delayMs, cancellationToken).ConfigureAwait(false);
    }

    private static bool IsTransientStatusCode(HttpStatusCode statusCode)
    {
        return statusCode is HttpStatusCode.RequestTimeout
            or HttpStatusCode.TooManyRequests
            or HttpStatusCode.InternalServerError
            or HttpStatusCode.BadGateway
            or HttpStatusCode.ServiceUnavailable
            or HttpStatusCode.GatewayTimeout;
    }

    private HttpClient CreateDefaultHttpClient()
    {
        var timeoutSeconds = Math.Clamp(_options.WebsiteRequestTimeoutSeconds, 5, 600);
        var client = new HttpClient(new HttpClientHandler
        {
            AllowAutoRedirect = false
        })
        {
            Timeout = TimeSpan.FromSeconds(timeoutSeconds)
        };
        client.DefaultRequestHeaders.UserAgent.ParseAdd("McpServer-WebsiteIngestor/1.0");
        return client;
    }

    private async Task<string?> ValidateTargetAsync(Uri uri, CancellationToken cancellationToken)
    {
        if (!_options.WebsiteAllowedSchemes.Contains(uri.Scheme, StringComparer.OrdinalIgnoreCase))
        {
            return "Only configured URL schemes are allowed.";
        }

        if (_options.WebsiteBlockedHosts.Contains(uri.Host, StringComparer.OrdinalIgnoreCase) || uri.Host.EndsWith(".localhost", StringComparison.OrdinalIgnoreCase))
        {
            return "Target host is blocked by SSRF policy.";
        }

        if (IPAddress.TryParse(uri.Host, out var ipAddress))
        {
            return IsBlockedAddress(ipAddress) ? "Target IP is blocked by SSRF policy." : null;
        }

        try
        {
            var addresses = await Dns.GetHostAddressesAsync(uri.DnsSafeHost, cancellationToken).ConfigureAwait(false);
            if (addresses.Any(IsBlockedAddress))
            {
                return "Resolved target IP is blocked by SSRF policy.";
            }
        }
        catch (SocketException ex)
        {
            return $"DNS lookup failed: {ex.Message}";
        }

        return null;
    }

    private static bool IsBlockedAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address))
        {
            return true;
        }

        if (address.AddressFamily == AddressFamily.InterNetwork)
        {
            var bytes = address.GetAddressBytes();
            if (bytes[0] == 10 || bytes[0] == 127)
            {
                return true;
            }

            if (bytes[0] == 169 && bytes[1] == 254)
            {
                return true;
            }

            if (bytes[0] == 192 && bytes[1] == 168)
            {
                return true;
            }

            if (bytes[0] == 172 && bytes[1] >= 16 && bytes[1] <= 31)
            {
                return true;
            }
        }

        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            if (address.IsIPv6LinkLocal || address.Equals(IPAddress.IPv6Loopback))
            {
                return true;
            }

            var bytes = address.GetAddressBytes();
            return (bytes[0] & 0xFE) == 0xFC;
        }

        return false;
    }

    private static bool IsRedirect(HttpStatusCode statusCode)
    {
        var code = (int)statusCode;
        return code is 301 or 302 or 303 or 307 or 308;
    }

    private static async Task<string> ReadLimitedTextAsync(Stream stream, int maxBytes, CancellationToken cancellationToken)
    {
        using var ms = new MemoryStream();
        var buffer = new byte[8192];
        while (true)
        {
            var read = await stream.ReadAsync(buffer.AsMemory(0, buffer.Length), cancellationToken).ConfigureAwait(false);
            if (read == 0)
            {
                break;
            }

            if (ms.Length + read > maxBytes)
            {
                throw new InvalidOperationException($"Response exceeded max bytes per page ({maxBytes}).");
            }

            await ms.WriteAsync(buffer.AsMemory(0, read), cancellationToken).ConfigureAwait(false);
        }

        return Encoding.UTF8.GetString(ms.ToArray());
    }

    private static string ExtractContentText(string contentType, string body, Uri pageUri)
    {
        if (IsHtmlContent(contentType, pageUri))
        {
            return ExtractHtmlText(body);
        }

        if (contentType.StartsWith("text/", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("json", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("xml", StringComparison.OrdinalIgnoreCase)
            || contentType.Contains("javascript", StringComparison.OrdinalIgnoreCase))
        {
            return body.Trim();
        }

        return string.Empty;
    }

    private static bool IsHtmlContent(string contentType, Uri pageUri)
    {
        return contentType.Contains("html", StringComparison.OrdinalIgnoreCase)
            || pageUri.AbsolutePath.EndsWith(".html", StringComparison.OrdinalIgnoreCase);
    }

    private static string ExtractHtmlText(string html)
    {
        var titleMatch = TitleRegex().Match(html);
        var title = titleMatch.Success ? titleMatch.Groups[1].Value.Trim() : string.Empty;

        var headingMatches = HeadingRegex().Matches(html);
        var headings = headingMatches.Select(m => m.Groups[2].Value.Trim()).Where(h => !string.IsNullOrWhiteSpace(h));

        var cleaned = ScriptStyleRegex().Replace(html, " ");
        cleaned = BoilerplateRegex().Replace(cleaned, " ");
        cleaned = TagRegex().Replace(cleaned, " ");
        cleaned = WhitespaceRegex().Replace(cleaned, " ").Trim();

        var sb = new StringBuilder();
        if (!string.IsNullOrWhiteSpace(title))
        {
            sb.AppendLine(title);
        }

        foreach (var heading in headings)
        {
            sb.AppendLine(heading);
        }

        if (!string.IsNullOrWhiteSpace(cleaned))
        {
            sb.Append(cleaned);
        }

        return sb.ToString().Trim();
    }

    private static IReadOnlyList<string> ExtractLinks(Uri pageUri, string html)
    {
        var links = new List<string>();
        foreach (Match match in LinkRegex().Matches(html))
        {
            var href = match.Groups[1].Value;
            if (href.StartsWith("#", StringComparison.Ordinal) || href.StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (Uri.TryCreate(pageUri, href, out var resolved))
            {
                links.Add(resolved.ToString());
            }
        }

        return links;
    }

    private static IEnumerable<string> OrderCrawlLinks(IReadOnlyList<string> links, Uri startUri)
    {
        return links
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(link => GetCrawlPriority(link, startUri))
            .ThenBy(link => link, StringComparer.OrdinalIgnoreCase);
    }

    private static int GetCrawlPriority(string rawUrl, Uri startUri)
    {
        if (!Uri.TryCreate(startUri, rawUrl, out var uri))
        {
            return int.MaxValue;
        }

        if (ShouldSkipCrawlLink(uri))
        {
            return int.MaxValue - 1;
        }

        var path = uri.AbsolutePath;
        if (path.StartsWith("/wiki/", StringComparison.OrdinalIgnoreCase))
        {
            var articleSlug = path["/wiki/".Length..];
            if (articleSlug.Contains(':', StringComparison.Ordinal))
            {
                return 2;
            }

            return 0;
        }

        if (path.EndsWith("/index.php", StringComparison.OrdinalIgnoreCase)
            || path.Equals("/index.php", StringComparison.OrdinalIgnoreCase))
        {
            return uri.Query.Contains("title=", StringComparison.OrdinalIgnoreCase) ? 1 : 3;
        }

        return 4;
    }

    private static bool ShouldSkipCrawlLink(Uri uri)
    {
        if (!string.Equals(uri.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var path = uri.AbsolutePath;
        if (path.EndsWith("/load.php", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/rest.php", StringComparison.OrdinalIgnoreCase)
            || path.EndsWith("/api.php", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (uri.Query.Contains("modules=", StringComparison.OrdinalIgnoreCase)
            || uri.Query.Contains("only=styles", StringComparison.OrdinalIgnoreCase)
            || uri.Query.Contains("only=scripts", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        var extension = Path.GetExtension(path);
        return extension.Length > 0
            && _nonContentExtensions.Contains(extension);
    }

    private static bool TryNormalizeUrl(string rawUrl, out Uri? normalizedUri, out string? error)
    {
        normalizedUri = null;
        error = null;
        if (!Uri.TryCreate(rawUrl?.Trim(), UriKind.Absolute, out var parsed))
        {
            error = "The provided URL is invalid.";
            return false;
        }

        if (!string.Equals(parsed.Scheme, Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase)
            && !string.Equals(parsed.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase))
        {
            error = "Only http/https URLs are supported.";
            return false;
        }

        normalizedUri = new Uri(NormalizeUrl(parsed));
        return true;
    }

    private static string NormalizeUrl(Uri uri)
    {
        var builder = new UriBuilder(uri)
        {
            Scheme = uri.Scheme.ToLowerInvariant(),
            Host = uri.Host.ToLowerInvariant(),
            Fragment = string.Empty,
        };

        if ((builder.Scheme == Uri.UriSchemeHttp && builder.Port == 80)
            || (builder.Scheme == Uri.UriSchemeHttps && builder.Port == 443))
        {
            builder.Port = -1;
        }

        var path = string.IsNullOrEmpty(builder.Path) ? "/" : builder.Path;
        if (path.Length > 1)
        {
            path = path.TrimEnd('/');
        }

        builder.Path = path;
        return builder.Uri.AbsoluteUri;
    }

    private static string BuildWorkspaceScopedDocumentId(string sourcePrefix, string workspaceRoot, string canonicalUrl)
    {
        var scope = ComputeHash(workspaceRoot).Substring(0, 16).ToLowerInvariant();
        var keyHash = ComputeHash(canonicalUrl).ToLowerInvariant();
        return $"{sourcePrefix}:{scope}:{keyHash}";
    }

    private string ResolveRepoRoot()
    {
        var candidate = _workspaceContext.WorkspacePath;
        if (string.IsNullOrWhiteSpace(candidate))
        {
            candidate = _options.RepoRoot;
        }

        return Path.GetFullPath(candidate);
    }

    private static string ComputeHash(string content)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(content));
        return Convert.ToHexString(bytes).ToUpperInvariant();
    }

    [GeneratedRegex("<title[^>]*>(.*?)</title>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex TitleRegex();

    [GeneratedRegex("<(h[1-6])[^>]*>(.*?)</\\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex HeadingRegex();

    [GeneratedRegex("<(script|style|noscript)[^>]*>.*?</\\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex ScriptStyleRegex();

    [GeneratedRegex("<(nav|header|footer|aside)[^>]*>.*?</\\1>", RegexOptions.IgnoreCase | RegexOptions.Singleline)]
    private static partial Regex BoilerplateRegex();

    [GeneratedRegex("<[^>]+>", RegexOptions.Singleline)]
    private static partial Regex TagRegex();

    [GeneratedRegex("\\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex("href\\s*=\\s*\"([^\"]+)\"", RegexOptions.IgnoreCase)]
    private static partial Regex LinkRegex();

    private static readonly HashSet<string> _nonContentExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".css",
        ".js",
        ".mjs",
        ".json",
        ".xml",
        ".map",
        ".png",
        ".jpg",
        ".jpeg",
        ".gif",
        ".svg",
        ".ico",
        ".webp",
        ".avif",
        ".woff",
        ".woff2",
        ".ttf",
        ".eot",
        ".otf",
        ".pdf",
        ".zip",
    };
}
