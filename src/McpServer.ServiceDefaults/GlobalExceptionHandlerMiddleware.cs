using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace Microsoft.Extensions.Hosting;

/// <summary>
/// Middleware that catches all unhandled exceptions flowing through the pipeline,
/// logs them, and returns a generic 500 JSON response. Register this as the
/// first middleware in the pipeline so it wraps every other middleware and
/// controller action.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1812:Avoid uninstantiated internal classes", Justification = "Instantiated by ASP.NET Core via UseMiddleware<T>()")]
internal sealed partial class GlobalExceptionHandlerMiddleware
{
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
            _logger.LogWarning("{ExceptionDetail}", ex.ToString());
            // Client disconnected — not an error worth logging at Error level.
            Log.RequestCancelled(_logger, context.Request.Method, context.Request.Path);
        }

#pragma warning disable CA1031 // Catch general exception — this is a global safety-net handler by design
        catch (Exception ex)
#pragma warning restore CA1031
        {
            _logger.LogError("{ExceptionDetail}", ex.ToString());
            Log.UnhandledException(_logger, ex, context.Request.Method, context.Request.Path);

            // Only write a response if one hasn't already started.
            if (!context.Response.HasStarted)
            {
                context.Response.StatusCode = (int)HttpStatusCode.InternalServerError;
                context.Response.ContentType = "application/json";
                await context.Response.WriteAsync(
                    """{"error":"An unexpected error occurred."}""").ConfigureAwait(false);
            }
        }
    }

    private static partial class Log
    {
        [LoggerMessage(
            EventId = 9001,
            Level = LogLevel.Error,
            Message = "Unhandled exception in middleware pipeline: {Method} {Path}")]
        public static partial void UnhandledException(
            ILogger logger, Exception exception, string method, string path);

        [LoggerMessage(
            EventId = 9002,
            Level = LogLevel.Debug,
            Message = "Request cancelled by client: {Method} {Path}")]
        public static partial void RequestCancelled(
            ILogger logger, string method, string path);
    }
}
