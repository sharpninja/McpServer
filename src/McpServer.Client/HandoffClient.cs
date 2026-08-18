using System.Net.Http;
using McpServer.Client.Models;

namespace McpServer.Client;

/// <summary>
/// TR-HANDOFF-SURFACE-001: Typed client for handoff ingestion, run inspection, and approval.
/// </summary>
public sealed class HandoffClient : McpClientBase
{
    /// <inheritdoc />
    public HandoffClient(HttpClient http, McpServerClientOptions options)
        : base(http, options)
    {
    }

    internal HandoffClient(HttpClient http, McpServerClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
    }

    /// <summary>FR-HANDOFF-007: POST /mcpserver/handoff/ingest</summary>
    public Task<HandoffIngestionResult> IngestHandoffAsync(
        HandoffIngestionRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<HandoffIngestionResult>("mcpserver/handoff/ingest", request, cancellationToken);

    /// <summary>FR-HANDOFF-007: GET /mcpserver/handoff/runs/{runId}</summary>
    public Task<HandoffIngestionResult> GetHandoffRunAsync(
        string runId,
        CancellationToken cancellationToken = default)
        => GetAsync<HandoffIngestionResult>($"mcpserver/handoff/runs/{Uri.EscapeDataString(runId)}", cancellationToken);

    /// <summary>FR-HANDOFF-007: POST /mcpserver/handoff/runs/{runId}/approve</summary>
    public Task<HandoffIngestionResult> ApproveHandoffAsync(
        string runId,
        HandoffApprovalRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<HandoffIngestionResult>(
            $"mcpserver/handoff/runs/{Uri.EscapeDataString(runId)}/approve",
            request,
            cancellationToken);
}
