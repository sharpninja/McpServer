namespace McpServer.Repl.Core;

/// <summary>FR-MCP-HOSTILEREVIEW-006: REPL workflow.hostileReview submit/status/get/query.</summary>
public interface IHostileReviewWorkflow
{
    /// <summary>Submit a bounded review request.</summary>
    Task<object> SubmitAsync(IReadOnlyDictionary<string, object?> args, CancellationToken cancellationToken);

    /// <summary>Read-only status.</summary>
    Task<object> StatusAsync(string requestId, CancellationToken cancellationToken);

    /// <summary>Read-only get.</summary>
    Task<object> GetAsync(string requestId, CancellationToken cancellationToken);

    /// <summary>AND-filtered query.</summary>
    Task<object> QueryAsync(IReadOnlyDictionary<string, object?> args, CancellationToken cancellationToken);
}
