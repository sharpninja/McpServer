namespace McpServer.Support.Mcp.Services;

/// <summary>TR-HANDOFF-SURFACE-001: Shared handoff ingestion service used by every public surface.</summary>
public interface IHandoffIngestionService
{
    /// <summary>Resolve a source, extract a draft, persist the run, and optionally create a TODO.</summary>
    Task<HandoffIngestionResult> IngestAsync(HandoffIngestionRequest request, CancellationToken cancellationToken = default);

    /// <summary>Load a previously persisted run.</summary>
    Task<HandoffIngestionResult> GetRunAsync(string runId, CancellationToken cancellationToken = default);

    /// <summary>Revalidate a stored draft and create or reject the TODO.</summary>
    Task<HandoffIngestionResult> ApproveAsync(string runId, HandoffApprovalRequest request, CancellationToken cancellationToken = default);
}
