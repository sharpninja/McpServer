namespace McpServer.Cqrs;

/// <summary>
/// TR-MCP-CQRS-001: Handler for a CQRS command. Receives the command and a <see cref="CallContext"/>
/// and returns a <see cref="Result{TResult}"/>. All handler methods are async.
/// </summary>
/// <typeparam name="TCommand">The command type.</typeparam>
/// <typeparam name="TResult">The result value type on success.</typeparam>
public interface ICommandHandler<in TCommand, TResult> where TCommand : ICommand<TResult>
{
    /// <summary>Handles the command asynchronously.</summary>
    /// <param name="command">The command to handle.</param>
    /// <param name="context">The call context providing correlation, logging, and auth context.</param>
    /// <returns>A <see cref="Result{TResult}"/> indicating success or failure.</returns>
    Task<Result<TResult>> HandleAsync(TCommand command, CallContext context);
}
