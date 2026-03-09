using McpServer.Client.Models;

namespace McpServer.AgentFramework.Todo;

/// <summary>
/// FR-MCP-066/TR-MCP-AGENT-007: Built-in TODO workflow service that exposes MCP Server TODO
/// retrieval, mutation, requirements-analysis, and prompt-stream operations through a cohesive
/// host-facing workflow surface.
/// <para>
/// Implementations must reuse <see cref="McpServer.Client.TodoClient"/> and the existing
/// <c>McpServer.Client.Models</c> DTOs so host applications can work with TODO workflows without
/// reimplementing HTTP or SSE plumbing while remaining compatible with TODO identifiers already
/// stored by the server, including legacy non-canonical IDs.
/// </para>
/// </summary>
public interface ITodoWorkflow
{
    /// <summary>
    /// Queries TODO items using the same optional filters exposed by
    /// <see cref="McpServer.Client.TodoClient.QueryAsync"/>.
    /// </summary>
    /// <param name="keyword">Optional keyword filter.</param>
    /// <param name="priority">Optional priority filter.</param>
    /// <param name="section">Optional section filter.</param>
    /// <param name="id">Optional TODO identifier filter.</param>
    /// <param name="done">Optional completion filter.</param>
    /// <param name="cancellationToken">Cancellation token forwarded directly to the transport client.</param>
    /// <returns>The matching TODO items and total count.</returns>
    Task<TodoQueryResult> QueryAsync(
        string? keyword = null,
        string? priority = null,
        string? section = null,
        string? id = null,
        bool? done = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets a single TODO item by its identifier as stored on the server.
    /// </summary>
    /// <param name="id">TODO item identifier.</param>
    /// <param name="cancellationToken">Cancellation token forwarded directly to the transport client.</param>
    /// <returns>The requested TODO item.</returns>
    Task<TodoFlatItem> GetAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Updates an existing TODO item by its identifier.
    /// </summary>
    /// <param name="id">TODO item identifier.</param>
    /// <param name="request">The mutation payload to send to the MCP Server.</param>
    /// <param name="cancellationToken">Cancellation token forwarded directly to the transport client.</param>
    /// <returns>The mutation result returned by the MCP Server.</returns>
    Task<TodoMutationResult> UpdateAsync(
        string id,
        TodoUpdateRequest request,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Runs server-side requirements analysis for a TODO item.
    /// </summary>
    /// <param name="id">TODO item identifier.</param>
    /// <param name="cancellationToken">Cancellation token forwarded directly to the transport client.</param>
    /// <returns>The requirements-analysis result returned by the MCP Server.</returns>
    Task<RequirementsAnalysisResult> AnalyzeRequirementsAsync(
        string id,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the server-generated implementation plan for a TODO item line-by-line.
    /// </summary>
    /// <param name="id">TODO item identifier.</param>
    /// <param name="cancellationToken">Cancellation token forwarded directly to the transport client.</param>
    /// <returns>An async sequence of streamed plan lines.</returns>
    IAsyncEnumerable<string> StreamPlanAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the server-generated status report for a TODO item line-by-line.
    /// </summary>
    /// <param name="id">TODO item identifier.</param>
    /// <param name="cancellationToken">Cancellation token forwarded directly to the transport client.</param>
    /// <returns>An async sequence of streamed status-report lines.</returns>
    IAsyncEnumerable<string> StreamStatusAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Streams the server-generated implementation guide for a TODO item line-by-line.
    /// </summary>
    /// <param name="id">TODO item identifier.</param>
    /// <param name="cancellationToken">Cancellation token forwarded directly to the transport client.</param>
    /// <returns>An async sequence of streamed implementation-guide lines.</returns>
    IAsyncEnumerable<string> StreamImplementAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Buffers the streamed plan output for a TODO item into a single newline-delimited string.
    /// </summary>
    /// <param name="id">TODO item identifier.</param>
    /// <param name="cancellationToken">Cancellation token forwarded directly to the transport client and stream enumeration.</param>
    /// <returns>The buffered plan text.</returns>
    Task<string> GetPlanAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Buffers the streamed status-report output for a TODO item into a single newline-delimited string.
    /// </summary>
    /// <param name="id">TODO item identifier.</param>
    /// <param name="cancellationToken">Cancellation token forwarded directly to the transport client and stream enumeration.</param>
    /// <returns>The buffered status-report text.</returns>
    Task<string> GetStatusReportAsync(string id, CancellationToken cancellationToken = default);

    /// <summary>
    /// Buffers the streamed implementation-guide output for a TODO item into a single newline-delimited string.
    /// </summary>
    /// <param name="id">TODO item identifier.</param>
    /// <param name="cancellationToken">Cancellation token forwarded directly to the transport client and stream enumeration.</param>
    /// <returns>The buffered implementation-guide text.</returns>
    Task<string> GetImplementationGuideAsync(string id, CancellationToken cancellationToken = default);
}
