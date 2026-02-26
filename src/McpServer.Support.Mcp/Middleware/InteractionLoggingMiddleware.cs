using System.Diagnostics;
using System.Text;
using McpServer.Support.Mcp.Models;
using McpServer.Support.Mcp.Options;
using McpServer.Support.Mcp.Services;
using Microsoft.Extensions.Options;

namespace McpServer.Support.Mcp.Middleware;

/// <summary>
/// TR-PLANNED-013: Middleware that logs every request/response with structured data
/// (including input/output bodies) and optionally enqueues entries for async submission
/// to a logging service.
/// </summary>
public sealed class InteractionLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<InteractionLoggingMiddleware> _logger;
    private readonly McpInteractionLoggingOptions _options;
    private readonly IInteractionLogSubmissionChannel? _channel;

    /// <summary>TR-PLANNED-013: Constructor.</summary>
    /// <param name="next">The next middleware in the pipeline.</param>
    /// <param name="logger">Logger instance.</param>
    /// <param name="options">Interaction logging options.</param>
    /// <param name="channel">Optional submission channel for async log forwarding.</param>
    public InteractionLoggingMiddleware(
        RequestDelegate next,
        ILogger<InteractionLoggingMiddleware> logger,
        IOptions<McpInteractionLoggingOptions> options,
        IInteractionLogSubmissionChannel? channel = null)
    {
        _next = next;
        _logger = logger;
        _options = options?.Value ?? new McpInteractionLoggingOptions();
        _channel = channel;
    }

    /// <summary>Logs the request/response interaction and optionally enqueues it for async submission.</summary>
    /// <param name="context">The HTTP context for the current request.</param>
    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // --- Log request entry with full headers ---
        _logger.LogInformation(
            "MCP request ENTRY {Method} {Path} (RequestId: {RequestId}, Headers: {Headers})",
            context.Request.Method,
            context.Request.Path.Value ?? string.Empty,
            context.TraceIdentifier,
            FormatHeaders(context.Request.Headers));

        // --- Capture request body ---
        string? requestBody = null;
        if (_options.IncludeRequestBody && context.Request.ContentLength is > 0)
        {
            context.Request.EnableBuffering();
            requestBody = await ReadAndTruncateAsync(context.Request.Body, _options.MaxBodyCaptureSize).ConfigureAwait(false);
            context.Request.Body.Position = 0;
        }

        // --- Wrap response body stream to capture output ---
        // Skip response buffering for SSE endpoints — buffering defeats real-time streaming.
        var isSse = string.Equals(context.Request.Headers.Accept.ToString(), "text/event-stream", StringComparison.OrdinalIgnoreCase)
                    || context.Request.Path.Value?.Contains("/prompt/", StringComparison.OrdinalIgnoreCase) == true;
        Stream? originalResponseBody = null;
        MemoryStream? responseBuffer = null;
        if (_options.IncludeResponseBody && !isSse)
        {
            originalResponseBody = context.Response.Body;
            responseBuffer = new MemoryStream();
            context.Response.Body = responseBuffer;
        }

        var stopwatch = Stopwatch.StartNew();
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        finally
        {
            stopwatch.Stop();

            // --- Read captured response body ---
            string? responseBody = null;
            if (responseBuffer != null && originalResponseBody != null)
            {
                responseBody = await ReadResponseBufferAsync(responseBuffer, _options.MaxBodyCaptureSize).ConfigureAwait(false);

                // Copy the buffered response back to the original stream so the client receives it.
                responseBuffer.Position = 0;
                await responseBuffer.CopyToAsync(originalResponseBody).ConfigureAwait(false);
                context.Response.Body = originalResponseBody;
                await responseBuffer.DisposeAsync().ConfigureAwait(false);
            }

            var path = context.Request.Path.Value ?? string.Empty;
            var queryString = _options.IncludeQueryString && !string.IsNullOrEmpty(context.Request.QueryString.Value)
                ? context.Request.QueryString.Value
                : null;

            var entry = new InteractionLogEntry
            {
                TimestampUtc = DateTime.UtcNow,
                Method = context.Request.Method,
                Path = path,
                QueryString = queryString,
                StatusCode = context.Response.StatusCode,
                DurationMs = stopwatch.Elapsed.TotalMilliseconds,
                RequestId = context.TraceIdentifier,
                RequestBody = requestBody,
                ResponseBody = responseBody
            };

            _logger.LogInformation(
                "MCP interaction {Method} {Path} completed with {StatusCode} in {DurationMs:F2}ms (RequestId: {RequestId}, RequestHeaders: {RequestHeaders}, ResponseHeaders: {ResponseHeaders}, Input: {RequestBody}, Output: {ResponseBody})",
                entry.Method,
                entry.Path,
                entry.StatusCode,
                entry.DurationMs,
                entry.RequestId,
                FormatHeaders(context.Request.Headers),
                FormatHeaders(context.Response.Headers),
                entry.RequestBody ?? "(none)",
                entry.ResponseBody ?? "(none)");

            if (!string.IsNullOrWhiteSpace(_options.LoggingServiceUrl) && _channel != null)
                _channel.TryEnqueue(entry);
        }
    }

    /// <summary>Reads a stream into a string, truncating at <paramref name="maxChars"/>.</summary>
    private static async Task<string?> ReadAndTruncateAsync(Stream stream, int maxChars)
    {
        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
        var buffer = new char[maxChars + 1];
        var charsRead = await reader.ReadBlockAsync(buffer, 0, buffer.Length).ConfigureAwait(false);
        if (charsRead == 0) return null;
        var truncated = charsRead > maxChars;
        return truncated
            ? new string(buffer, 0, maxChars) + "...(truncated)"
            : new string(buffer, 0, charsRead);
    }

    /// <summary>Reads a <see cref="MemoryStream"/> response buffer into a string, truncating at <paramref name="maxChars"/>.</summary>
    private static async Task<string?> ReadResponseBufferAsync(MemoryStream buffer, int maxChars)
    {
        if (buffer.Length == 0) return null;
        buffer.Position = 0;
        return await ReadAndTruncateAsync(buffer, maxChars).ConfigureAwait(false);
    }

    /// <summary>Formats HTTP headers as a semicolon-delimited string for structured logging.</summary>
    private static string FormatHeaders(IHeaderDictionary headers)
    {
        var sb = new StringBuilder();
        foreach (var kvp in headers)
        {
            if (sb.Length > 0) sb.Append("; ");
            sb.Append(kvp.Key).Append('=').Append(kvp.Value);
        }
        return sb.Length > 0 ? sb.ToString() : "(none)";
    }
}
