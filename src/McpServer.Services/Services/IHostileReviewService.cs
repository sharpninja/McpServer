using McpServer.Support.Mcp.Models;

namespace McpServer.Support.Mcp.Services;

/// <summary>FR-MCP-HOSTILEREVIEW-001: Queue-and-record hostile review. Submit is the only public mutation.</summary>
public interface IHostileReviewService
{
    /// <summary>Enqueue a bounded review request.</summary>
    Task<HostileReviewResult> SubmitAsync(HostileReviewSubmitRequest request, CancellationToken cancellationToken = default);

    /// <summary>Read-only status. Never mutates lease, attempt, or cancellation state.</summary>
    Task<HostileReviewResult> StatusAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>Read-only get of normalized results.</summary>
    Task<HostileReviewResult> GetAsync(string requestId, CancellationToken cancellationToken = default);

    /// <summary>AND-filtered query. Empty match is an empty list.</summary>
    Task<IReadOnlyList<HostileReviewResult>> QueryAsync(HostileReviewQueryRequest request, CancellationToken cancellationToken = default);
}

/// <summary>TR-MCP-HOSTILEREVIEW-003: Internal worker mutation. Not a public REST/MCP/REPL/Director operation.</summary>
public interface IHostileReviewWorker
{
    /// <summary>Accept one terminal reviewer output for a queued request.</summary>
    Task<HostileReviewResult> AcceptReviewerOutputAsync(string requestId, HostileReviewerOutput output, CancellationToken cancellationToken = default);
}
