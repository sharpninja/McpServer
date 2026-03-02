namespace McpServer.Cqrs;

/// <summary>
/// TR-MCP-CQRS-001: Optional interface for commands/queries that specify a timeout.
/// When implemented, the <see cref="Dispatcher"/> wraps handler execution in a
/// <see cref="CancellationTokenSource"/> with the specified timeout.
/// </summary>
public interface IHasTimeout
{
    /// <summary>Maximum time allowed for handler execution.</summary>
    TimeSpan Timeout { get; }
}
