using System.Net.Http;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>
/// FR-MCP-HOSTILEREVIEW-006: Typed client for hostile-review submit/status/get/query only.
/// </summary>
public sealed class HostileReviewClient : McpClientBase
{
    /// <inheritdoc />
    public HostileReviewClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options)
    {
    }

    internal HostileReviewClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
    }

    /// <summary>FR-MCP-HOSTILEREVIEW-001: POST /qbrainai/hostile-review/submit</summary>
    public Task<HostileReviewResult> SubmitAsync(
        HostileReviewSubmitRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<HostileReviewResult>("qbrainai/hostile-review/submit", request, cancellationToken);

    /// <summary>FR-MCP-HOSTILEREVIEW-006: GET /qbrainai/hostile-review/{requestId}/status</summary>
    public Task<HostileReviewResult> StatusAsync(
        string requestId,
        CancellationToken cancellationToken = default)
        => GetAsync<HostileReviewResult>(
            $"qbrainai/hostile-review/{Uri.EscapeDataString(requestId)}/status",
            cancellationToken);

    /// <summary>FR-MCP-HOSTILEREVIEW-004: GET /qbrainai/hostile-review/{requestId}</summary>
    public Task<HostileReviewResult> GetAsync(
        string requestId,
        CancellationToken cancellationToken = default)
        => GetAsync<HostileReviewResult>(
            $"qbrainai/hostile-review/{Uri.EscapeDataString(requestId)}",
            cancellationToken);

    /// <summary>FR-MCP-HOSTILEREVIEW-005: POST /qbrainai/hostile-review/query</summary>
    public async Task<IReadOnlyList<HostileReviewResult>> QueryAsync(
        HostileReviewQueryRequest request,
        CancellationToken cancellationToken = default)
    {
        var rows = await PostAsync<List<HostileReviewResult>>("qbrainai/hostile-review/query", request, cancellationToken)
            .ConfigureAwait(false);
        return rows;
    }
}
