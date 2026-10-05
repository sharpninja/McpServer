using System.Net.Http;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>
/// TR-HANDOFF-SURFACE-001: Typed client for handoff ingestion, run inspection, and approval.
/// </summary>
public sealed class HandoffClient : McpClientBase
{
    /// <inheritdoc />
    public HandoffClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options)
    {
    }

    internal HandoffClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder)
    {
    }

    /// <summary>FR-HANDOFF-007: POST /qbrainai/handoff/ingest</summary>
    public Task<HandoffIngestionResult> IngestHandoffAsync(
        HandoffIngestionRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<HandoffIngestionResult>("qbrainai/handoff/ingest", request, cancellationToken);

    /// <summary>FR-HANDOFF-007: GET /qbrainai/handoff/runs/{runId}</summary>
    public Task<HandoffIngestionResult> GetHandoffRunAsync(
        string runId,
        CancellationToken cancellationToken = default)
        => GetAsync<HandoffIngestionResult>($"qbrainai/handoff/runs/{Uri.EscapeDataString(runId)}", cancellationToken);

    /// <summary>FR-HANDOFF-007: POST /qbrainai/handoff/runs/{runId}/approve</summary>
    public Task<HandoffIngestionResult> ApproveHandoffAsync(
        string runId,
        HandoffApprovalRequest request,
        CancellationToken cancellationToken = default)
        => PostAsync<HandoffIngestionResult>(
            $"qbrainai/handoff/runs/{Uri.EscapeDataString(runId)}/approve",
            request,
            cancellationToken);
}
