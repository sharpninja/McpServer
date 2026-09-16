using System.Net.Http;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// FR-MCP-HOSTILEREVIEW-006: Typed client for hostile-review submit/status/get/query only.
/// </summary>
public sealed class HostileReviewClient : McpClientBase
{
    /// <inheritdoc />
    public HostileReviewClient(HttpClient http, McpServerClientOptions options)
        : base(http, options)
    {
    }

    internal HostileReviewClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
    }

    /// <summary>FR-MCP-HOSTILEREVIEW-001: POST /mcpserver/hostile-review/submit</summary>
    public Task<HostileReviewResult> SubmitAsync(
        HostileReviewSubmitRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<HostileReviewResult>("mcpserver/hostile-review/submit", request, cancellationToken);

    /// <summary>FR-MCP-HOSTILEREVIEW-006: GET /mcpserver/hostile-review/{requestId}/status</summary>
    public Task<HostileReviewResult> StatusAsync(
        string requestId,
        CancellationToken cancellationToken = default)
        => GetAsync<HostileReviewResult>(
            $"mcpserver/hostile-review/{Uri.EscapeDataString(requestId)}/status",
            cancellationToken);

    /// <summary>FR-MCP-HOSTILEREVIEW-004: GET /mcpserver/hostile-review/{requestId}</summary>
    public Task<HostileReviewResult> GetAsync(
        string requestId,
        CancellationToken cancellationToken = default)
        => GetAsync<HostileReviewResult>(
            $"mcpserver/hostile-review/{Uri.EscapeDataString(requestId)}",
            cancellationToken);

    /// <summary>FR-MCP-HOSTILEREVIEW-005: POST /mcpserver/hostile-review/query</summary>
    public async Task<IReadOnlyList<HostileReviewResult>> QueryAsync(
        HostileReviewQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var rows = await PostAsync<List<HostileReviewResult>>("mcpserver/hostile-review/query", request, cancellationToken)
            .ConfigureAwait(false);
        return rows;
    }
}
