namespace McpServer.Support.Mcp.Services;

/// <summary>
/// FR-MCP-103: Hub/proxy topology service for enrollment, heartbeat,
/// workspace inventory, queue status, replay intake, fanout, and conflicts.
/// </summary>
public interface IFederationTopologyService
{
    /// <summary>Returns a best-effort cached status snapshot for synchronous status endpoints.</summary>
    FederationTopologySnapshot GetSnapshot();

    /// <summary>Enrolls or updates a local proxy on the hub.</summary>
    Task<FederationEnrollmentResponse> EnrollAsync(FederationEnrollmentRequest request, CancellationToken cancellationToken);

    /// <summary>Records a heartbeat from an enrolled local proxy.</summary>
    Task<FederationHeartbeatResponse> HeartbeatAsync(string proxyId, FederationHeartbeatRequest request, CancellationToken cancellationToken);

    /// <summary>Registers or updates one workspace hosted by a proxy.</summary>
    Task<FederationWorkspaceInfo> RegisterWorkspaceAsync(string proxyId, FederationWorkspaceRegistrationRequest request, CancellationToken cancellationToken);

    /// <summary>Lists enrolled proxies.</summary>
    Task<IReadOnlyList<FederationProxyInfo>> ListProxiesAsync(CancellationToken cancellationToken);

    /// <summary>Lists workspaces globally or for a single proxy.</summary>
    Task<IReadOnlyList<FederationWorkspaceInfo>> ListWorkspacesAsync(string? proxyId, CancellationToken cancellationToken);

    /// <summary>Records an operation submitted by a proxy or hub fanout source.</summary>
    Task<FederationOperationResponse> RecordOperationAsync(FederationOperationRequest request, CancellationToken cancellationToken);

    /// <summary>Queues a local proxy write for later replay to the hub.</summary>
    Task<FederationOperationResponse> QueueLocalOperationAsync(FederationOperationRequest request, CancellationToken cancellationToken);

    /// <summary>Lists local proxy operations waiting for replay.</summary>
    Task<IReadOnlyList<FederationOperationReplayItem>> ListPendingOperationsAsync(
        string proxyId,
        int limit,
        int maxAttempts,
        CancellationToken cancellationToken);

    /// <summary>Records a failed replay attempt for a queued local operation.</summary>
    Task<FederationOperationResponse> MarkReplayFailureAsync(
        string operationId,
        string error,
        int maxAttempts,
        CancellationToken cancellationToken);

    /// <summary>Acknowledges an operation after replay or fanout delivery.</summary>
    Task<FederationOperationResponse> AcknowledgeOperationAsync(string operationId, FederationOperationAckRequest request, CancellationToken cancellationToken);

    /// <summary>Returns queue and conflict counts.</summary>
    Task<FederationQueueStatusResponse> GetQueueStatusAsync(string? proxyId, CancellationToken cancellationToken);

    /// <summary>Lists open or historical conflicts.</summary>
    Task<IReadOnlyList<FederationConflictInfo>> ListConflictsAsync(string? proxyId, bool openOnly, CancellationToken cancellationToken);

    /// <summary>Resolves a conflict with an operator-selected status.</summary>
    Task<FederationConflictInfo?> ResolveConflictAsync(string conflictId, FederationConflictResolutionRequest request, CancellationToken cancellationToken);

    /// <summary>Returns hub fanout entries after a sequence for a proxy.</summary>
    Task<IReadOnlyList<FederationSyncItem>> GetSyncItemsAsync(string proxyId, long afterSequence, CancellationToken cancellationToken);
}

/// <summary>FR-MCP-103: Synchronous topology counters surfaced by federation status.</summary>
public sealed class FederationTopologySnapshot
{
    /// <summary>Number of enrolled proxies in the cached snapshot.</summary>
    public int ProxyCount { get; set; }

    /// <summary>Number of globally registered proxy-hosted workspaces in the cached snapshot.</summary>
    public int WorkspaceCount { get; set; }

    /// <summary>Total queued operation count in the cached snapshot.</summary>
    public int QueueDepth { get; set; }

    /// <summary>Total open conflict count in the cached snapshot.</summary>
    public int ConflictCount { get; set; }
}
