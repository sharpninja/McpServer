using McpServer.Client.Models;

namespace McpServer.Repl.Core;

/// <summary>TR-HANDOFF-SURFACE-001: REPL workflow for handoff ingest, get, and approve.</summary>
public interface IHandoffWorkflow
{
    /// <summary>Ingest a handoff document.</summary>
    Task<HandoffIngestionResult> IngestAsync(HandoffIngestionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Get a persisted run.</summary>
    Task<HandoffIngestionResult> GetAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>Approve or reject a persisted run.</summary>
    Task<HandoffIngestionResult> ApproveAsync(string runId, HandoffApprovalRequest request, CancellationToken cancellationToken = default);
}
