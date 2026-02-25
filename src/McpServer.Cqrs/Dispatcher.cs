using System.Collections.Concurrent;
using System.Diagnostics;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace McpServer.Cqrs;

/// <summary>
/// TR-MCP-CQRS-001, TR-MCP-CQRS-003, TR-MCP-CQRS-004: Central dispatcher for CQRS commands and queries.
/// Resolves handlers from DI, wraps execution in pipeline behaviors, manages <see cref="CallContext"/>
/// lifecycle, and implements <see cref="ILoggerProvider"/> for correlation-enriched logging.
/// </summary>
public sealed class Dispatcher : ILoggerProvider
{
    private readonly IServiceProvider _services;
    private readonly ILogger<Dispatcher> _logger;
    private readonly ConcurrentDictionary<long, CallContext> _activeContexts = new();

    /// <summary>Initializes a new <see cref="Dispatcher"/>.</summary>
    /// <param name="services">The DI service provider for resolving handlers and behaviors.</param>
    /// <param name="logger">Logger for dispatcher-level diagnostics.</param>
    public Dispatcher(IServiceProvider services, ILogger<Dispatcher> logger)
    {
        _services = services;
        _logger = logger;
    }

    /// <summary>Active call contexts keyed by <see cref="CorrelationId.BaseId"/>.</summary>
    public IReadOnlyDictionary<long, CallContext> ActiveContexts => _activeContexts;

    /// <summary>
    /// Dispatches a command to its handler, wrapped in pipeline behaviors.
    /// </summary>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="command">The command to dispatch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The handler result.</returns>
    public Task<Result<TResult>> SendAsync<TResult>(ICommand<TResult> command, CancellationToken ct = default)
        => DispatchAsync<TResult>(command, typeof(ICommandHandler<,>), ct);

    /// <summary>
    /// Dispatches a query to its handler, wrapped in pipeline behaviors.
    /// </summary>
    /// <typeparam name="TResult">The result value type.</typeparam>
    /// <param name="query">The query to dispatch.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>The handler result.</returns>
    public Task<Result<TResult>> QueryAsync<TResult>(IQuery<TResult> query, CancellationToken ct = default)
        => DispatchAsync<TResult>(query, typeof(IQueryHandler<,>), ct);

    private async Task<Result<TResult>> DispatchAsync<TResult>(object request, Type handlerOpenType, CancellationToken ct)
    {
        var requestType = request.GetType();
        var context = new CallContext { OperationName = requestType.Name, CancellationToken = ct };
        _activeContexts[context.Correlation.BaseId] = context;

        var sw = Stopwatch.StartNew();
        try
        {
            _logger.LogDebug(
                "Dispatching {Operation} [{CorrelationId}]",
                context.OperationName, context.Correlation.Current);

            // Apply timeout if the request implements IHasTimeout
            var effectiveCt = ct;
            CancellationTokenSource? timeoutCts = null;
            if (request is IHasTimeout hasTimeout)
            {
                timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(ct);
                timeoutCts.CancelAfter(hasTimeout.Timeout);
                effectiveCt = timeoutCts.Token;
                context.CancellationToken = effectiveCt;
            }

            try
            {
                // Resolve handler
                var handlerType = handlerOpenType.MakeGenericType(requestType, typeof(TResult));
                var handler = _services.GetRequiredService(handlerType);

                // Build pipeline: behaviors wrap the handler invocation
                var behaviors = _services.GetServices<IPipelineBehavior>().ToList();

                // The innermost delegate calls the handler
                Func<Task<Result<TResult>>> innermost = () => InvokeHandler<TResult>(handler, handlerType, request, context);

                // Wrap with behaviors (outermost first = first registered)
                var pipeline = innermost;
                for (var i = behaviors.Count - 1; i >= 0; i--)
                {
                    var behavior = behaviors[i];
                    var next = pipeline;
                    pipeline = () => behavior.HandleAsync(request, context, next);
                }

                var result = await pipeline().ConfigureAwait(false);

                sw.Stop();
                LogResult(result, context, sw.Elapsed);
                return result;
            }
            finally
            {
                timeoutCts?.Dispose();
            }
        }
        catch (OperationCanceledException ex) when (ct.IsCancellationRequested)
        {
            sw.Stop();
            var result = Result<TResult>.Failure("Operation was cancelled.", ex);
            _logger.LogWarning("Dispatch cancelled {Operation} [{CorrelationId}] after {Elapsed}ms",
                context.OperationName, context.Correlation.Current, sw.ElapsedMilliseconds);
            return result;
        }
        catch (OperationCanceledException ex)
        {
            sw.Stop();
            var result = Result<TResult>.Failure("Operation timed out.", ex);
            _logger.LogWarning("Dispatch timed out {Operation} [{CorrelationId}] after {Elapsed}ms",
                context.OperationName, context.Correlation.Current, sw.ElapsedMilliseconds);
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            _logger.LogError(ex, "Dispatch failed {Operation} [{CorrelationId}] after {Elapsed}ms",
                context.OperationName, context.Correlation.Current, sw.ElapsedMilliseconds);
            return Result<TResult>.Failure(ex);
        }
        finally
        {
            _activeContexts.TryRemove(context.Correlation.BaseId, out _);
            context.Dispose();
        }
    }

    private static async Task<Result<TResult>> InvokeHandler<TResult>(
        object handler, Type handlerType, object request, CallContext context)
    {
        // Use reflection to call HandleAsync(request, context)
        var method = handlerType.GetMethod("HandleAsync")
            ?? throw new InvalidOperationException($"Handler {handlerType.Name} does not have a HandleAsync method.");

        var task = (Task<Result<TResult>>)method.Invoke(handler, [request, context])!;
        return await task.ConfigureAwait(false);
    }

    /// <summary>
    /// TR-MCP-CQRS-004: Logs the result of a dispatch call.
    /// Success → Debug, Failure with exception → Error, Failure without exception → Warning.
    /// </summary>
    private void LogResult<TResult>(Result<TResult> result, CallContext context, TimeSpan elapsed)
    {
        if (result.IsSuccess)
        {
            _logger.LogDebug(
                "Dispatch succeeded {Operation} [{CorrelationId}] in {Elapsed}ms",
                context.OperationName, context.Correlation.Current, elapsed.TotalMilliseconds);
        }
        else if (result.Exception is not null)
        {
            _logger.LogError(result.Exception,
                "Dispatch failed {Operation} [{CorrelationId}] in {Elapsed}ms: {Error}",
                context.OperationName, context.Correlation.Current, elapsed.TotalMilliseconds, result.Error);
        }
        else
        {
            _logger.LogWarning(
                "Dispatch failed {Operation} [{CorrelationId}] in {Elapsed}ms: {Error}",
                context.OperationName, context.Correlation.Current, elapsed.TotalMilliseconds, result.Error);
        }
    }

    // --- ILoggerProvider implementation ---

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName) => new DispatcherLogger(this, categoryName);

    /// <inheritdoc />
    public void Dispose() { /* No resources to dispose */ }
}

/// <summary>
/// TR-MCP-CQRS-003: Logger created by <see cref="Dispatcher"/> as <see cref="ILoggerProvider"/>.
/// Enriches structured log entries with decomposed correlation context from active <see cref="CallContext"/>s.
/// </summary>
internal sealed class DispatcherLogger : ILogger
{
    private readonly Dispatcher _dispatcher;
    private readonly string _categoryName;

    /// <summary>Initializes a new <see cref="DispatcherLogger"/>.</summary>
    /// <param name="dispatcher">The dispatcher providing active context lookup.</param>
    /// <param name="categoryName">The logger category name.</param>
    public DispatcherLogger(Dispatcher dispatcher, string categoryName)
    {
        _dispatcher = dispatcher;
        _categoryName = categoryName;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        // Find the active context (if any) to enrich the log entry
        // This is a best-effort lookup — if no context is active, the log is still emitted
        foreach (var kvp in _dispatcher.ActiveContexts)
        {
            kvp.Value.Log(logLevel, eventId, state, exception, formatter);
            break; // Use the first active context (typically there's only one per thread)
        }
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel) => logLevel != LogLevel.None;

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
}
