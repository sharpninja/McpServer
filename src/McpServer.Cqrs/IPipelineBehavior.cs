namespace McpServer.Cqrs;

/// <summary>
/// TR-MCP-CQRS-005: Pipeline behavior that wraps handler execution with pre/post processing.
/// Behaviors receive the request, <see cref="CallContext"/>, and a <c>next</c> delegate.
/// Behaviors can short-circuit by returning <see cref="Result{T}.Failure(string)"/> without calling <c>next</c>.
/// Registration order determines execution order (outermost first).
/// </summary>
public interface IPipelineBehavior
{
    /// <summary>
    /// Handles the pipeline step. Call <paramref name="next"/> to continue to the next behavior or handler.
    /// Return a <see cref="Result{T}"/> directly to short-circuit the pipeline.
    /// </summary>
    /// <typeparam name="T">The result value type.</typeparam>
    /// <param name="request">The command or query being dispatched.</param>
    /// <param name="context">The call context for this dispatch.</param>
    /// <param name="next">Delegate to invoke the next behavior or the handler.</param>
    /// <returns>The result from the handler or a short-circuit result.</returns>
    Task<Result<T>> HandleAsync<T>(object request, CallContext context, Func<Task<Result<T>>> next);
}
