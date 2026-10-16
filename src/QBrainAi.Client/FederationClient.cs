using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using QBrainAi.Client.Models;

namespace QBrainAi.Client;

/// <summary>
/// Client for federation management endpoints (<c>/qbrainai/federation</c>).
/// Provides runtime control of federation state: enable/disable, add/remove targets,
/// configure workspace routing rules, auto-discover targets from tunnels, get connection
/// credentials, and push local data to remote federation targets.
/// FR-MCP-077, FR-MCP-085.
/// </summary>
/// <seealso cref="QBrainAiClient.Federation"/>
public sealed class FederationClient : McpClientBase
{
    /// <inheritdoc />
    public FederationClient(HttpClient http, QBrainAiClientOptions options)
        : base(http, options) { }

    internal FederationClient(HttpClient http, QBrainAiClientOptions options, WorkspacePathHolder holder)
        : base(http, options, holder) { }

    /// <summary>Get the current federation status including all targets and workspace routes.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Full federation status snapshot.</returns>
    public async Task<FederationStatusResponse> GetStatusAsync(CancellationToken cancellationToken = default)
        => await GetAsync<FederationStatusResponse>("qbrainai/federation/status", cancellationToken);

    /// <summary>List local proxies enrolled with the hub.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Proxy inventory.</returns>
    public async Task<List<FederationProxyInfo>> ListProxiesAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<FederationProxyInfo>>("qbrainai/federation/proxies", cancellationToken);

    /// <summary>Enroll a local proxy with the hub.</summary>
    /// <param name="request">Enrollment payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Accepted enrollment response.</returns>
    public async Task<FederationEnrollmentResponse> EnrollProxyAsync(
        FederationEnrollmentRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<FederationEnrollmentResponse>("qbrainai/federation/proxies/enroll", request, cancellationToken);

    /// <summary>Record a heartbeat from an enrolled local proxy.</summary>
    /// <param name="proxyId">Proxy identifier.</param>
    /// <param name="request">Heartbeat payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Heartbeat response.</returns>
    public async Task<FederationHeartbeatResponse> HeartbeatAsync(
        string proxyId,
        FederationHeartbeatRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<FederationHeartbeatResponse>(
            $"qbrainai/federation/proxies/{Encode(proxyId)}/heartbeat",
            request,
            cancellationToken);

    /// <summary>Register or update one workspace hosted by a proxy.</summary>
    /// <param name="proxyId">Proxy identifier.</param>
    /// <param name="request">Workspace registration payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Registered workspace info.</returns>
    public async Task<FederationWorkspaceInfo> RegisterWorkspaceAsync(
        string proxyId,
        FederationWorkspaceRegistrationRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<FederationWorkspaceInfo>(
            $"qbrainai/federation/proxies/{Encode(proxyId)}/workspaces",
            request,
            cancellationToken);

    /// <summary>List proxy-hosted workspaces known by the hub.</summary>
    /// <param name="proxyId">Optional proxy filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Workspace inventory.</returns>
    public async Task<List<FederationWorkspaceInfo>> ListWorkspacesAsync(string? proxyId = null, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(proxyId)
            ? "qbrainai/federation/workspaces"
            : $"qbrainai/federation/workspaces?proxyId={Encode(proxyId)}";
        return await GetAsync<List<FederationWorkspaceInfo>>(path, cancellationToken);
    }

    /// <summary>Return queued operation and conflict counts.</summary>
    /// <param name="proxyId">Optional proxy filter.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Queue status.</returns>
    public async Task<FederationQueueStatusResponse> GetQueueStatusAsync(string? proxyId = null, CancellationToken cancellationToken = default)
    {
        var path = string.IsNullOrWhiteSpace(proxyId)
            ? "qbrainai/federation/queue"
            : $"qbrainai/federation/queue?proxyId={Encode(proxyId)}";
        return await GetAsync<FederationQueueStatusResponse>(path, cancellationToken);
    }

    /// <summary>List federation conflicts.</summary>
    /// <param name="proxyId">Optional proxy filter.</param>
    /// <param name="openOnly">Whether to return only open conflicts.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Conflict inventory.</returns>
    public async Task<List<FederationConflictInfo>> ListConflictsAsync(
        string? proxyId = null,
        bool openOnly = true,
        CancellationToken cancellationToken = default)
    {
        var query = string.IsNullOrWhiteSpace(proxyId)
            ? $"?openOnly={openOnly}"
            : $"?proxyId={Encode(proxyId)}&openOnly={openOnly}";
        return await GetAsync<List<FederationConflictInfo>>($"qbrainai/federation/conflicts{query}", cancellationToken);
    }

    /// <summary>Return mutable state adapter coverage diagnostics.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Adapter coverage rows.</returns>
    public async Task<List<FederationStateAdapterCoverage>> GetAdapterCoverageAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<FederationStateAdapterCoverage>>("qbrainai/federation/adapters", cancellationToken);

    /// <summary>Return hub fanout rows for a proxy after a sequence.</summary>
    /// <param name="proxyId">Proxy identifier.</param>
    /// <param name="afterSequence">Exclusive sequence cursor.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Sync rows waiting for proxy acknowledgement.</returns>
    public async Task<List<FederationSyncItem>> GetSyncItemsAsync(
        string proxyId,
        long afterSequence = 0,
        CancellationToken cancellationToken = default)
        => await GetAsync<List<FederationSyncItem>>(
            $"qbrainai/federation/sync?proxyId={Encode(proxyId)}&afterSequence={afterSequence}",
            cancellationToken);

    /// <summary>Acknowledge one recipient-specific sync row.</summary>
    /// <param name="sequence">Sync sequence number.</param>
    /// <param name="request">Acknowledgement payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated operation status.</returns>
    public async Task<FederationOperationResponse> AcknowledgeSyncAsync(
        long sequence,
        FederationSyncAckRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<FederationOperationResponse>($"qbrainai/federation/sync/{sequence}/ack", request, cancellationToken);

    /// <summary>Accept or idempotently replay one federation operation.</summary>
    /// <param name="request">Operation payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation status.</returns>
    public async Task<FederationOperationResponse> RecordOperationAsync(
        FederationOperationRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<FederationOperationResponse>("qbrainai/federation/operations", request, cancellationToken);

    /// <summary>Acknowledge one replayed or fanned-out operation.</summary>
    /// <param name="operationId">Operation identifier.</param>
    /// <param name="request">Acknowledgement payload.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated operation status.</returns>
    public async Task<FederationOperationResponse> AcknowledgeOperationAsync(
        string operationId,
        FederationOperationAckRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<FederationOperationResponse>(
            $"qbrainai/federation/operations/{Encode(operationId)}/ack",
            request,
            cancellationToken);

    /// <summary>Accept or idempotently replay one signed federation operation envelope.</summary>
    /// <param name="envelope">Signed operation envelope.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Operation status.</returns>
    public async Task<FederationOperationResponse> RecordEnvelopeAsync(
        FederationExecutionEnvelope envelope,
        CancellationToken cancellationToken = default)
        => await PostAsync<FederationOperationResponse>("qbrainai/federation/envelopes", envelope, cancellationToken);

    /// <summary>Resolve a federation conflict.</summary>
    /// <param name="conflictId">Conflict identifier.</param>
    /// <param name="request">Resolution request.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Resolved conflict info.</returns>
    public async Task<FederationConflictInfo> ResolveConflictAsync(
        string conflictId,
        FederationConflictResolutionRequest request,
        CancellationToken cancellationToken = default)
        => await PostAsync<FederationConflictInfo>(
            $"qbrainai/federation/conflicts/{Encode(conflictId)}/resolve",
            request,
            cancellationToken);

    /// <summary>Enable federation proxying globally.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated federation status.</returns>
    public async Task<FederationStatusResponse> EnableAsync(CancellationToken cancellationToken = default)
        => await PostAsync<FederationStatusResponse>("qbrainai/federation/enable", null, cancellationToken);

    /// <summary>Disable federation proxying globally.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated federation status.</returns>
    public async Task<FederationStatusResponse> DisableAsync(CancellationToken cancellationToken = default)
        => await PostAsync<FederationStatusResponse>("qbrainai/federation/disable", null, cancellationToken);

    /// <summary>List all registered federation targets.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Array of federation target info objects.</returns>
    public async Task<List<FederationTargetInfo>> ListTargetsAsync(CancellationToken cancellationToken = default)
        => await GetAsync<List<FederationTargetInfo>>("qbrainai/federation/targets", cancellationToken);

    /// <summary>Add a new named federation target.</summary>
    /// <param name="request">Target configuration.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The newly created target info.</returns>
    /// <exception cref="McpConflictException">A target with the same name already exists.</exception>
    public async Task<FederationTargetInfo> AddTargetAsync(FederationTargetAddRequest request, CancellationToken cancellationToken = default)
        => await PostAsync<FederationTargetInfo>("qbrainai/federation/targets", request, cancellationToken);

    /// <summary>Remove a federation target by name.</summary>
    /// <param name="name">Target name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP status code (204 on success).</returns>
    /// <exception cref="McpNotFoundException">No target with the given name exists.</exception>
    public async Task<HttpStatusCode> RemoveTargetAsync(string name, CancellationToken cancellationToken = default)
        => await SendForStatusAsync(HttpMethod.Delete, $"qbrainai/federation/targets/{Encode(name)}", null, cancellationToken);

    /// <summary>Set a target as the global default for requests with no workspace-specific route.</summary>
    /// <param name="name">Target name to set as default.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated federation status.</returns>
    /// <exception cref="McpNotFoundException">No target with the given name exists.</exception>
    public async Task<FederationStatusResponse> SetDefaultTargetAsync(string name, CancellationToken cancellationToken = default)
        => await PostAsync<FederationStatusResponse>($"qbrainai/federation/targets/{Encode(name)}/set-default", null, cancellationToken);

    /// <summary>Clear the global default target.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated federation status.</returns>
    public async Task<FederationStatusResponse> ClearDefaultTargetAsync(CancellationToken cancellationToken = default)
        => await DeleteAsync<FederationStatusResponse>("qbrainai/federation/targets/default", cancellationToken);

    /// <summary>Add or update a workspace-specific routing rule.</summary>
    /// <param name="request">Workspace path and target name.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Updated route list.</returns>
    /// <exception cref="McpNotFoundException">The specified target does not exist.</exception>
    public async Task<List<WorkspaceRouteInfo>> AddRouteAsync(WorkspaceRouteRequest request, CancellationToken cancellationToken = default)
        => await PostAsync<List<WorkspaceRouteInfo>>("qbrainai/federation/routes", request, cancellationToken);

    /// <summary>Remove a workspace-specific routing rule.</summary>
    /// <param name="request">Route specifying the workspace path to remove.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The HTTP status code (204 on success).</returns>
    /// <exception cref="McpNotFoundException">No route for the specified workspace path exists.</exception>
    public async Task<HttpStatusCode> RemoveRouteAsync(WorkspaceRouteRequest request, CancellationToken cancellationToken = default)
        => await SendForStatusAsync(HttpMethod.Delete, "qbrainai/federation/routes", request, cancellationToken);

    /// <summary>Get connection credentials so a federated peer can connect to this server.</summary>
    /// <param name="workspaceName">Display name of the workspace to look up.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Connection info including base URL, port, and API key.</returns>
    /// <exception cref="McpNotFoundException">No enabled workspace with the given name exists.</exception>
    public async Task<FederationConnectionInfo> GetConnectionAsync(string workspaceName, CancellationToken cancellationToken = default)
        => await GetAsync<FederationConnectionInfo>($"qbrainai/federation/connection?workspaceName={Encode(workspaceName)}", cancellationToken);

    /// <summary>Auto-discover federation targets from running tunnel providers.</summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Discovery result with count and details of newly registered targets.</returns>
    public async Task<TunnelDiscoveryResult> DiscoverFromTunnelsAsync(CancellationToken cancellationToken = default)
        => await PostAsync<TunnelDiscoveryResult>("qbrainai/federation/targets/discover-from-tunnels", null, cancellationToken);

    /// <summary>Push local data (TODOs, session logs) to the resolved federation target.</summary>
    /// <param name="types">
    /// Optional filter for which data types to push. Valid values: <c>"todos"</c>, <c>"sessionlogs"</c>.
    /// Pass <see langword="null"/> or an empty list to push all types.
    /// </param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Push result with success/failure counts.</returns>
    /// <exception cref="McpConflictException">Federation is disabled.</exception>
    /// <exception cref="McpNotFoundException">No federation target resolved.</exception>
    public async Task<FederationPushResult> PushAsync(IReadOnlyList<string>? types = null, CancellationToken cancellationToken = default)
        => await PostAsync<FederationPushResult>("qbrainai/federation/push", new FederationPushRequest { Types = types }, cancellationToken);

    private static string Encode(string value) => Uri.EscapeDataString(value);
}
