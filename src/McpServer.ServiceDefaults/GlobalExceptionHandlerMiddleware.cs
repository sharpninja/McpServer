using System.Diagnostics;
using System.Net;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Middleware that catches all unhandled exceptions flowing through the pipeline,
/// logs them, and returns a standardized detailed-but-sanitized 500 JSON response.
/// Register this as the first middleware in the pipeline so it wraps every other middleware and
/// controller action.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by ASP.NET Core via UseMiddleware<T>()")]
internal sealed partial class GlobalExceptionHandlerMiddleware
{
    private static readonly JsonSerializerOptions s_jsonOptions = new(JsonSerializerDefaults.Web);
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

#pragma warning disable CA1031
        catch (Exception ex)
#pragma warning restore CA1031
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
            context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
            context.Response.ContentType = "application/json";

            var payload = new HttpErrorResponse
            {
                Status = StatusCodes.Status500InternalServerError,
                Error = "internal_server_error",
                Message = "The server encountered an unexpected error while processing the request.",
                Detail = BuildSanitizedDetail(ex, operation),
                Operation = operation,
                TraceId = traceId,
                TimestampUtc = DateTimeOffset.UtcNow,
            };

            await context.Response.WriteAsync(JsonSerializer.Serialize(payload, s_jsonOptions)).ConfigureAwait(false);
        }
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
