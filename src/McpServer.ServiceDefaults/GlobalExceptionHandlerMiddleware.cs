using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Middleware that catches all unhandled exceptions flowing through the pipeline,
/// logs them, and returns a standardized detailed-but-sanitized 500 JSON response.
/// Register this as the first middleware in the pipeline so it wraps every other middleware and
/// controller action.
/// </summary>
internal sealed partial class GlobalExceptionHandlerMiddleware
{
    private static readonly Regex s_bearerTokenRegex = new(@"Bearer\s+[A-Za-z0-9\-\._~\+/]+=*", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    private static readonly Regex s_connectionStringRegex = new(@"(?i)(password|pwd|secret|token|apikey|api_key|clientsecret|client_secret)\s*=\s*[^;\s]+", RegexOptions.Compiled);
    private static readonly Regex s_jsonSecretRegex = new(@"(?i)\""(password|pwd|secret|token|access_token|refresh_token|apiKey|api_key|clientSecret|client_secret)\""\s*:\s*\""[^\""]+\""", RegexOptions.Compiled);
    private static readonly Regex s_stackTraceLineRegex = new(@"\s+at\s+.+", RegexOptions.Compiled);

    private readonly RequestDelegate _next;
    private readonly ILogger<GlobalExceptionHandlerMiddleware> _logger;

    public GlobalExceptionHandlerMiddleware(
        RequestDelegate next,
        ILogger<GlobalExceptionHandlerMiddleware> logger)
    {
        _next = next ?? throw new ArgumentNullException(nameof(next));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await _next(context).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (context.RequestAborted.IsCancellationRequested)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            Log.RequestCancelled(_logger, context.Request.Method, context.Request.Path);
        }

        catch (Exception ex)
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            var operation = GetOperation(context);
            var traceId = GetTraceId(context);
            Log.UnhandledException(_logger, ex, context.Request.Method, context.Request.Path, traceId);

            if (context.Response.HasStarted)
            {
                Log.ResponseAlreadyStarted(_logger, context.Request.Method, context.Request.Path, traceId);
                throw;
            }

            context.Response.Clear();
            context.Response.ContentType = "application/json";

            // TR-MCP-HEALTH-003: single typed mapping for backend-unavailability. When the
            // registered detector classifies the failure as a connection-class storage outage,
            // return HTTP 503 with the stable machine-readable body
            // {"error":"backend_unavailable", ...} instead of a generic 500 echoing raw
            // provider text (raw SqlClient messages, the EnableRetryOnFailure hint).
            HttpErrorResponse payload;
            var classified = Classify(context, ex);
            if (classified is not null)
            {
                context.Response.StatusCode = classified.StatusCode;
                payload = new HttpErrorResponse
                {
                    Status = classified.StatusCode,
                    Error = classified.Code,
                    Code = classified.Code,
                    Message = classified.Message,
                    Retryable = classified.Retryable,
                    Details = ToStringDetails(classified.Details),
                    Detail = classified.Code == "backend_unavailable"
                        ? $"Operation '{operation}' failed because the storage backend is unreachable."
                        : BuildSanitizedDetail(ex, operation),
                    Operation = operation,
                    TraceId = traceId,
                    TimestampUtc = DateTimeOffset.UtcNow,
                };
            }
            else if (IsBackendUnavailable(context, ex))
            {
                context.Response.StatusCode = StatusCodes.Status503ServiceUnavailable;
                payload = new HttpErrorResponse
                {
                    Status = StatusCodes.Status503ServiceUnavailable,
                    Error = "backend_unavailable",
                    Code = "backend_unavailable",
                    Message = "The storage backend is currently unreachable. Retry the request once connectivity is restored.",
                    Retryable = true,
                    Detail = $"Operation '{operation}' failed because the storage backend is unreachable.",
                    Operation = operation,
                    TraceId = traceId,
                    TimestampUtc = DateTimeOffset.UtcNow,
                };
            }
            else
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                payload = new HttpErrorResponse
                {
                    Status = StatusCodes.Status500InternalServerError,
                    Error = "internal_server_error",
                    Code = "internal_server_error",
                    Message = "The server encountered an unexpected error while processing the request.",
                    Retryable = false,
                    Detail = BuildSanitizedDetail(ex, operation),
                    Operation = operation,
                    TraceId = traceId,
                    TimestampUtc = DateTimeOffset.UtcNow,
                };
            }

            var result = JsonSerializer.Serialize(payload, ServiceDefaultsJsonContext.Default.HttpErrorResponse);
            await context.Response.WriteAsync(result).ConfigureAwait(false);
        }
    }

    /// <summary>
    /// TR-MCP-HEALTH-003: resolves the optional <see cref="IBackendUnavailabilityDetector"/> from
    /// request services and classifies the exception. Returns false when no detector is
    /// registered so services without storage keep the plain 500 mapping.
    /// </summary>
    private static bool IsBackendUnavailable(HttpContext context, Exception exception)
    {
        if (context.RequestServices is not { } requestServices)
            return false;

        var detector = requestServices.GetService<IBackendUnavailabilityDetector>();
        return detector?.IsBackendUnavailable(exception) == true;
    }

    private static McpErrorClassification? Classify(HttpContext context, Exception exception)
    {
        if (context.RequestServices is not { } requestServices)
            return null;

        var classifier = requestServices.GetService<IMcpErrorClassifier>();
        return classifier?.Classify(exception);
    }

    private static Dictionary<string, string>? ToStringDetails(IReadOnlyDictionary<string, object?>? details)
    {
        if (details is null || details.Count == 0)
            return null;

        var mapped = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var pair in details)
        {
            if (pair.Value is null)
                continue;
            mapped[pair.Key] = pair.Value.ToString() ?? string.Empty;
        }

        return mapped.Count == 0 ? null : mapped;
    }

    private static string GetOperation(HttpContext context)
    {
        var endpoint = context.GetEndpoint()?.DisplayName;
        if (!string.IsNullOrWhiteSpace(endpoint))
            return endpoint!;

        return $"{context.Request.Method} {context.Request.Path}";
    }

    private static string GetTraceId(HttpContext context)
        => Activity.Current?.Id ?? context.TraceIdentifier;

    private static string BuildSanitizedDetail(Exception ex, string operation)
    {
        var category = ex switch
        {
            TimeoutException => "timeout",
            UnauthorizedAccessException => "access_denied",
            IOException => "io_failure",
            InvalidOperationException => "invalid_operation",
            _ => "unhandled_exception"
        };

        var exceptionName = ex.GetType().Name;
        var message = string.IsNullOrWhiteSpace(ex.Message)
            ? "No exception message was provided."
            : Sanitize(ex.Message);

        return $"Operation '{operation}' failed with {category} ({exceptionName}): {message}";
    }

    private static string Sanitize(string value)
    {
        var sanitized = value.Replace("\r", " ").Replace("\n", " ").Trim();
        sanitized = s_bearerTokenRegex.Replace(sanitized, "Bearer [REDACTED]");
        sanitized = s_connectionStringRegex.Replace(sanitized, "$1=[REDACTED]");
        sanitized = s_jsonSecretRegex.Replace(sanitized, m =>
        {
            var key = m.Groups[1].Value;
            return $"\"{key}\":\"[REDACTED]\"";
        });
        sanitized = s_stackTraceLineRegex.Replace(sanitized, string.Empty);

        return sanitized.Length > 600 ? sanitized[..600] + "…" : sanitized;
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 9001,
            Level = LogLevel.Error,
            Message = "Unhandled exception in middleware pipeline: {Method} {Path} (TraceId: {TraceId})")]
        public static partial void UnhandledException(
            ILogger logger, Exception exception, string method, string path, string traceId);

        [LoggerMessage(
            EventId = 9002,
            Level = LogLevel.Debug,
            Message = "Request cancelled by client: {Method} {Path}")]
        public static partial void RequestCancelled(
            ILogger logger, string method, string path);

        [LoggerMessage(
            EventId = 9003,
            Level = LogLevel.Warning,
            Message = "Unhandled exception occurred after response started: {Method} {Path} (TraceId: {TraceId})")]
        public static partial void ResponseAlreadyStarted(
            ILogger logger, string method, string path, string traceId);
    }
}
