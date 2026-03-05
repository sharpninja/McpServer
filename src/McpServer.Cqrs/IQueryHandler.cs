namespace McpServer.Cqrs;

/// <summary>
/// TR-MCP-CQRS-001: Handler for a CQRS query. Receives the query and a <see cref="CallContext"/>
/// and returns a <see cref="Result{TResult}"/>. All handler methods are async.
/// </summary>
/// <typeparam name="TQuery">The query type.</typeparam>
/// <typeparam name="TResult">The result value type on success.</typeparam>
public interface IQueryHandler<in TQuery, TResult> where TQuery : IQuery<TResult>
{
    /// <summary>Handles the query asynchronously.</summary>
    /// <param name="query">The query to handle.</param>
    /// <param name="context">The call context providing correlation, logging, and auth context.</param>
    /// <returns>A <see cref="Result{TResult}"/> indicating success or failure.</returns>
    Task<Result<TResult>> HandleAsync(TQuery query, CallContext context);
}
